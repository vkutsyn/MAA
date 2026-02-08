using MAA.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;

namespace MAA.Tests.Fixtures;

/// <summary>
/// Database fixture for integration tests using PostgreSQL test containers.
/// Implements IAsyncLifetime to manage container lifecycle across test classes.
/// </summary>
public class DatabaseFixture : IAsyncLifetime
{
    private PostgreSqlContainer? _postgreSqlContainer;
    private string? _connectionString;

    /// <summary>
    /// Gets the connection string for the test database.
    /// </summary>
    public string ConnectionString => _connectionString 
        ?? throw new InvalidOperationException("Database fixture not initialized. Call InitializeAsync first.");

    /// <summary>
    /// Initializes the PostgreSQL container and runs migrations.
    /// Called once before any tests in the test class.
    /// </summary>
    public async Task InitializeAsync()
    {
        // Create and start PostgreSQL container
        _postgreSqlContainer = new PostgreSqlBuilder()
            .WithImage("postgres:16-alpine")
            .WithDatabase("maa_test")
            .WithUsername("postgres")
            .WithPassword("postgres")
            .WithCleanUp(true)
            .Build();

        await _postgreSqlContainer.StartAsync();

        // Get connection string
        _connectionString = _postgreSqlContainer.GetConnectionString();

        // Create DbContext and run migrations
        var options = new DbContextOptionsBuilder<SessionContext>()
            .UseNpgsql(_connectionString)
            .Options;

        await using var context = new SessionContext(options);
        await context.Database.MigrateAsync();
    }

    /// <summary>
    /// Stops and disposes the PostgreSQL container.
    /// Called once after all tests in the test class complete.
    /// </summary>
    public async Task DisposeAsync()
    {
        if (_postgreSqlContainer != null)
        {
            await _postgreSqlContainer.StopAsync();
            await _postgreSqlContainer.DisposeAsync();
        }
    }

    /// <summary>
    /// Creates a new SessionContext for a test with the test database connection.
    /// Each test should create its own context for isolation.
    /// </summary>
    public SessionContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<SessionContext>()
            .UseNpgsql(ConnectionString)
            .Options;

        return new SessionContext(options);
    }
}
