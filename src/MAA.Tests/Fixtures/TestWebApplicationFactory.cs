using MAA.Infrastructure.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace MAA.Tests.Fixtures;

/// <summary>
/// WebApplicationFactory for integration tests.
/// Configures test services and in-memory database for API testing.
/// </summary>
public class TestWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string? _connectionString;

    public TestWebApplicationFactory()
    {
        // Use in-memory database by default for fast unit tests
        _connectionString = null;
    }

    public TestWebApplicationFactory(string connectionString)
    {
        // Use provided connection string for integration tests with PostgreSQL
        _connectionString = connectionString;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Remove the existing DbContext registration
            services.RemoveAll(typeof(DbContextOptions<SessionContext>));
            services.RemoveAll(typeof(SessionContext));

            if (_connectionString != null)
            {
                // Use provided connection string (PostgreSQL test container)
                services.AddDbContext<SessionContext>(options =>
                    options.UseNpgsql(_connectionString)
                );
            }
            else
            {
                // Use in-memory database for fast tests
                services.AddDbContext<SessionContext>(options =>
                    options.UseInMemoryDatabase("TestDatabase")
                );
            }

            // Override services for testing as needed
            // Example: Replace IKeyVaultClient with mock
            // services.RemoveAll(typeof(IKeyVaultClient));
            // services.AddScoped<IKeyVaultClient, MockKeyVaultClient>();

            // Ensure database is created
            using var scope = services.BuildServiceProvider().CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<SessionContext>();

            if (_connectionString == null)
            {
                // For in-memory database, ensure created
                dbContext.Database.EnsureCreated();
            }
            // For PostgreSQL, migrations are already applied in DatabaseFixture
        });

        builder.UseEnvironment("Test");
    }
}
