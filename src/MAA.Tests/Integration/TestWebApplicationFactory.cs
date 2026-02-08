using MAA.Infrastructure.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace MAA.Tests.Integration;

/// <summary>
/// WebApplicationFactory for testing MAA.API with in-process HTTP client.
/// Configures test database and dependency injection for integration tests.
/// Supports both real PostgreSQL (via DatabaseFixture) and in-memory (for contract tests).
/// </summary>
public class TestWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly DatabaseFixture? _databaseFixture;

    /// <summary>
    /// Constructor for contract tests (no persistent database).
    /// Uses in-memory or test-specific configuration.
    /// </summary>
    public TestWebApplicationFactory()
    {
        _databaseFixture = null;
    }

    /// <summary>
    /// Constructor for integration tests with persistent database.
    /// Uses real PostgreSQL via Testcontainers through DatabaseFixture.
    /// </summary>
    /// <param name="databaseFixture">DatabaseFixture providing PostgreSQL connection</param>
    public TestWebApplicationFactory(DatabaseFixture databaseFixture)
    {
        _databaseFixture = databaseFixture ?? throw new ArgumentNullException(nameof(databaseFixture));
    }

    /// <summary>
    /// Configures host services for testing.
    /// Replaces production DbContext with test-specific configuration.
    /// Ensures dependency injection is properly set up for testing scenarios.
    /// </summary>
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Remove the production DbContext
            var dbContextDescriptor = services.FirstOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<SessionContext>));

            if (dbContextDescriptor != null)
            {
                services.Remove(dbContextDescriptor);
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
                // Contract tests: Use in-memory HTTP testing without database
                // The API will use dependency injection configured by Program.cs
                // Services are available, but no database is accessed in contract tests.
            }

            // Ensure all services are properly configured
            services.BuildServiceProvider();
        });
    }
}
