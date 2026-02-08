using DotNet.Testcontainers.Configurations;
using MAA.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;
using Xunit;

namespace MAA.Tests.Integration;

/// <summary>
/// Database fixture using Testcontainers for PostgreSQL integration testing.
/// Manages PostgreSQL container lifecycle and provides DbContext for tests.
/// Implements IAsyncLifetime for proper async initialization and cleanup.
/// </summary>
public class DatabaseFixture : IAsyncLifetime
{
    private string? _connectionString;
    private PostgreSqlContainer? _container;

    /// <summary>
    /// Gets the database context for testing.
    /// Creates on-demand with the established connection string.
    /// </summary>
    public SessionContext CreateContext()
    {
        if (string.IsNullOrEmpty(_connectionString))
            throw new InvalidOperationException("Database fixture not initialized. Did you forget to implement IAsyncLifetime?");

        var options = new DbContextOptionsBuilder<SessionContext>()
            .UseNpgsql(_connectionString, npgsqlOptions =>
                npgsqlOptions.MigrationsAssembly("MAA.Infrastructure")
            )
            .Options;

        return new SessionContext(options);
    }

    /// <summary>
    /// Initializes the PostgreSQL container and applies migrations.
    /// Called automatically by xUnit before test execution.
    /// </summary>
    public async Task InitializeAsync()
    {
        _container = new PostgreSqlBuilder()
            .WithImage("postgres:17-alpine")
            .WithDatabase("maapp")
            .WithUsername("maauser")
            .WithPassword("maasecurepass123")
            .Build();

        await _container.StartAsync();

        _connectionString = _container.GetConnectionString();

        // Apply migrations to ensure database schema is ready
        await ApplyMigrationsAsync();
    }

    /// <summary>
    /// Stops the PostgreSQL container and cleans up resources.
    /// Called automatically by xUnit after test execution.
    /// </summary>
    public async Task DisposeAsync()
    {
        if (_container != null)
        {
            await _container.StopAsync();
            await _container.DisposeAsync();
        }
    }

    /// <summary>
    /// Applies Entity Framework migrations to the test database.
    /// Ensures migrations are executed in the proper order.
    /// </summary>
    private async Task ApplyMigrationsAsync()
    {
        using var context = CreateContext();
        try
        {
            await context.Database.MigrateAsync();
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Failed to apply migrations to test database.", ex);
        }
    }

    /// <summary>
    /// Clears all data from tables for test isolation.
    /// Called between tests if needed via helper method.
    /// </summary>
    public async Task ClearAllDataAsync()
    {
        using var context = CreateContext();

        // Delete in order of dependencies
        await context.SessionAnswers.ExecuteDeleteAsync();
        await context.Sessions.ExecuteDeleteAsync();
        await context.Users.ExecuteDeleteAsync();
        await context.EncryptionKeys.ExecuteDeleteAsync();

        await context.SaveChangesAsync();
    }

    /// <summary>
    /// Gets the raw connection string for direct database access if needed.
    /// </summary>
    public string GetConnectionString()
    {
        if (string.IsNullOrEmpty(_connectionString))
            throw new InvalidOperationException("Database fixture not initialized.");

        return _connectionString;
    }
}

/// <summary>
/// xUnit collection fixture for database initialization across multiple tests.
/// Use [Collection("Database collection")] on test classes to share this fixture.
/// </summary>
[CollectionDefinition("Database collection")]
public class DatabaseCollection : ICollectionFixture<DatabaseFixture>
{
    // This class has no code, and is never created. Its purpose is to define
    // the collection and allow tests to opt-in by using
    // [Collection("Database collection")]
}
