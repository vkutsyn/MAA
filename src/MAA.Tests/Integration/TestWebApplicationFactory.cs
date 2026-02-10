using MAA.Application.Services;
using MAA.Domain.Rules;
using MAA.Infrastructure.Data;
using MAA.Infrastructure.Security;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;

namespace MAA.Tests.Integration;

/// <summary>
/// WebApplicationFactory for testing MAA.API with in-process HTTP client.
/// Configures test database and dependency injection for integration tests.
/// Supports both real PostgreSQL (via DatabaseFixture) and in-memory (for contract tests).
/// </summary>
public class TestWebApplicationFactory : WebApplicationFactory<Program>
{
    private DatabaseFixture? _databaseFixture;

    /// <summary>
    /// Constructor for contract tests (no persistent database).
    /// Uses in-memory or test-specific configuration.
    /// </summary>
    public TestWebApplicationFactory()
    {
        _databaseFixture = null;
    }

    /// <summary>
    /// Factory method for integration tests with persistent database.
    /// Uses real PostgreSQL via Testcontainers through DatabaseFixture.
    /// </summary>
    /// <param name="databaseFixture">DatabaseFixture providing PostgreSQL connection</param>
    public static TestWebApplicationFactory CreateWithDatabase(DatabaseFixture databaseFixture)
    {
        return new TestWebApplicationFactory 
        { 
            _databaseFixture = databaseFixture ?? throw new ArgumentNullException(nameof(databaseFixture))
        };
    }

    /// <summary>
    /// Configures host services for testing.
    /// Replaces production DbContext with test-specific configuration.
    /// Ensures dependency injection is properly set up for testing scenarios.
    /// </summary>
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Set test environment so Program.cs skips certain registrations
        builder.UseEnvironment("test");

        builder.ConfigureAppConfiguration((context, config) =>
        {
            // Add test configuration that provides required settings
            var testConfig = new Dictionary<string, string>
            {
                ["Azure:KeyVault:Uri"] = "https://test-keyvault.vault.azure.net/",
                ["ConnectionStrings:DefaultConnection"] = _databaseFixture?.GetConnectionString() 
                    ?? "Host=127.0.0.1;Port=5432;Database=maa_test;Username=postgres;Password=postgres"
            };
            config.AddInMemoryCollection(testConfig);
        });

        builder.ConfigureServices(services =>
        {
            // Find and remove the old DbContext registration to prevent DI validation errors
            var dbContextDescriptor = services.FirstOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<SessionContext>));

            if (dbContextDescriptor != null)
            {
                services.Remove(dbContextDescriptor);
            }

            // Find and remove any DbContextOptions registrations to get a clean slate
            var allDbContextOptions = services.Where(
                d => d.ServiceType.IsGenericType && 
                     d.ServiceType.GetGenericTypeDefinition() == typeof(DbContextOptions<>) &&
                     d.ServiceType.GetGenericArguments()[0] == typeof(SessionContext)
            ).ToList();

            foreach (var descriptor in allDbContextOptions)
            {
                services.Remove(descriptor);
            }

            // Remove production KeyVaultClient to replace with mock
            var keyVaultDescriptor = services.FirstOrDefault(
                d => d.ServiceType == typeof(IKeyVaultClient));

            if (keyVaultDescriptor != null)
            {
                services.Remove(keyVaultDescriptor);
            }

            // Add test-specific DbContext configuration
            if (_databaseFixture != null)
            {
                // Integration tests: Use real PostgreSQL via DatabaseFixture
                var connectionString = _databaseFixture.GetConnectionString();

                services.AddDbContext<SessionContext>(options =>
                    options.UseNpgsql(
                        connectionString,
                        npgsqlOptions => npgsqlOptions.MigrationsAssembly("MAA.Infrastructure")
                    )
                );
            }
            else
            {
                // Contract tests: Use in-memory database for HTTP testing
                // This allows contract tests to run without Docker/PostgreSQL
                services.AddDbContext<SessionContext>(options =>
                    options.UseInMemoryDatabase("TestDatabase_" + Guid.NewGuid().ToString())
                );
            }

            // Register mock KeyVaultClient for testing (no Azure Key Vault access needed)
            var mockKeyVaultClient = new Mock<IKeyVaultClient>();
            mockKeyVaultClient
                .Setup(x => x.GetKeyAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync("test-encryption-key-material-32-bytes-long-1234567890");
            mockKeyVaultClient
                .Setup(x => x.GetCurrentKeyVersionAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(1); // Return version 1 (seeded by migration)
            
            services.AddScoped(_ => mockKeyVaultClient.Object);

            if (_databaseFixture == null)
            {
                using var serviceProvider = services.BuildServiceProvider();
                using var scope = serviceProvider.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<SessionContext>();

                dbContext.Database.EnsureCreated();

                if (!dbContext.EligibilityRules.Any())
                {
                    SeedContractTestRules(dbContext);
                }
            }

            // DO NOT validate the service provider here - let the host do it
            // This allows the host to properly initialize all services
        });
    }

    private static void SeedContractTestRules(SessionContext dbContext)
    {
        var now = DateTime.UtcNow;
        var effectiveDate = now.Date.AddDays(-1);
        const string alwaysEligibleRuleLogic = "{ \"<=\": [ { \"var\": \"monthly_income_cents\" }, 9999999999 ] }";

        var programs = new List<MedicaidProgram>
        {
            CreateProgram("IL", "IL Contract Program", "IL_CONTRACT", EligibilityPathway.MAGI, now),
            CreateProgram("CA", "CA Contract Program", "CA_CONTRACT", EligibilityPathway.MAGI, now),
            CreateProgram("NY", "NY Contract Program", "NY_CONTRACT", EligibilityPathway.MAGI, now),
            CreateProgram("TX", "TX Contract Program", "TX_CONTRACT", EligibilityPathway.MAGI, now),
            CreateProgram("FL", "FL Contract Program", "FL_CONTRACT", EligibilityPathway.MAGI, now)
        };

        var rules = programs.Select(program => new EligibilityRule
        {
            RuleId = Guid.NewGuid(),
            ProgramId = program.ProgramId,
            Program = program,
            StateCode = program.StateCode,
            RuleName = $"{program.StateCode} Contract Rule",
            Version = 1.0m,
            RuleLogic = alwaysEligibleRuleLogic,
            EffectiveDate = effectiveDate,
            EndDate = null,
            CreatedAt = now,
            UpdatedAt = now,
            Description = "Contract test rule: always eligible"
        }).ToList();

        dbContext.MedicaidPrograms.AddRange(programs);
        dbContext.EligibilityRules.AddRange(rules);
        dbContext.SaveChanges();
    }

    private static MedicaidProgram CreateProgram(
        string stateCode,
        string programName,
        string programCode,
        EligibilityPathway pathway,
        DateTime now)
    {
        return new MedicaidProgram
        {
            ProgramId = Guid.NewGuid(),
            StateCode = stateCode,
            ProgramName = programName,
            ProgramCode = programCode,
            EligibilityPathway = pathway,
            Description = "Contract test program",
            CreatedAt = now,
            UpdatedAt = now
        };
    }
}
