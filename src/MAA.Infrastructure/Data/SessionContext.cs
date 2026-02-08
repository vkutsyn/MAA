using Microsoft.EntityFrameworkCore;

namespace MAA.Infrastructure.Data;

/// <summary>
/// Entity Framework Core DbContext for session management.
/// Manages sessions, users, and encryption-related entities.
/// </summary>
public class SessionContext : DbContext
{
    public SessionContext(DbContextOptions<SessionContext> options) : base(options)
    {
    }

    // DbSets will be added as entities are created in Phase 1
    // public DbSet<Session> Sessions => Set<Session>();
    // public DbSet<SessionAnswer> SessionAnswers => Set<SessionAnswer>();
    // public DbSet<EncryptionKey> EncryptionKeys => Set<EncryptionKey>();
    // public DbSet<User> Users => Set<User>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Entity configurations will be added in Task T12
        // modelBuilder.ApplyConfigurationsFromAssembly(typeof(SessionContext).Assembly);
    }
}
