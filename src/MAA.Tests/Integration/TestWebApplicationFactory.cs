using MAA.Application.Services;
using EligibilityDomain = MAA.Domain.Eligibility;
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
    private bool _isSeeded = false;
    private readonly object _seedLock = new object();

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
        builder.UseEnvironment("Test");

        builder.ConfigureAppConfiguration((context, config) =>
        {
            // Add test configuration that provides required settings
            var testConfig = new Dictionary<string, string>
            {
                ["Azure:KeyVault:Uri"] = "https://test-keyvault.vault.azure.net/",
                ["ConnectionStrings:DefaultConnection"] = _databaseFixture?.GetConnectionString()
                    ?? "Host=127.0.0.1;Port=5432;Database=maa_test;Username=postgres;Password=postgres"
            };
            config.AddInMemoryCollection(testConfig!);
        });

        builder.ConfigureServices(services =>
        {
            // No need to remove DbContext - Program.cs skips it in Test environment
            // Just add our test-specific configuration

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
                {
                    options.UseNpgsql(
                        connectionString,
                        npgsqlOptions => npgsqlOptions.MigrationsAssembly("MAA.Infrastructure")
                    );
                    options.EnableServiceProviderCaching(false);
                });
            }
            else
            {
                // Contract tests: Use in-memory database for HTTP testing
                // This allows contract tests to run without Docker/PostgreSQL
                services.AddDbContext<SessionContext>(options =>
                {
                    options.UseInMemoryDatabase("TestDatabase_" + Guid.NewGuid().ToString());
                    options.EnableServiceProviderCaching(false);
                });
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

            // DO NOT build service provider here - it will cause multiple provider error
            // Seeding will happen after the host is built
        });
    }

    /// <summary>
    /// Ensures database is seeded for contract tests after server is created.
    /// Call this from tests that need seeded data.
    /// </summary>
    public void EnsureSeeded()
    {
        if (_databaseFixture == null && !_isSeeded)
        {
            lock (_seedLock)
            {
                if (!_isSeeded)
                {
                    // Access Server property to ensure host is built
                    _ = Server;

                    using var scope = Services.CreateScope();
                    var dbContext = scope.ServiceProvider.GetRequiredService<SessionContext>();

                    dbContext.Database.EnsureCreated();

                    if (!dbContext.EligibilityRules.Any())
                    {
                        SeedContractTestRules(dbContext);
                    }

                    if (!dbContext.EligibilityRuleSetVersions.Any())
                    {
                        SeedEligibilityV2ContractTestRules(dbContext);
                    }

                    _isSeeded = true;
                }
            }
        }
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

    private static void SeedEligibilityV2ContractTestRules(SessionContext dbContext)
    {
        var now = DateTime.UtcNow;
        var ruleSet = new EligibilityDomain.RuleSetVersion
        {
            RuleSetVersionId = Guid.NewGuid(),
            StateCode = "IL",
            Version = "v1",
            EffectiveDate = now.Date.AddDays(-1),
            EndDate = null,
            Status = EligibilityDomain.RuleSetStatus.Active,
            CreatedAt = now
        };

        var program = new EligibilityDomain.ProgramDefinition
        {
            ProgramCode = "IL_BASIC",
            StateCode = "IL",
            ProgramName = "IL Basic Program",
            Description = "Contract test program",
            Category = EligibilityDomain.ProgramCategory.Magi,
            IsActive = true
        };

        var rule = new EligibilityDomain.EligibilityRule
        {
            EligibilityRuleId = Guid.NewGuid(),
            RuleSetVersionId = ruleSet.RuleSetVersionId,
            RuleSetVersion = ruleSet,
            ProgramCode = program.ProgramCode,
            Program = program,
            RuleLogic = "{ \"==\": [ { \"var\": \"isCitizen\" }, true ] }",
            Priority = 0,
            CreatedAt = now
        };

        dbContext.EligibilityRuleSetVersions.Add(ruleSet);
        dbContext.ProgramDefinitions.Add(program);
        dbContext.EligibilityRulesV2.Add(rule);
        dbContext.SaveChanges();
    }
}
