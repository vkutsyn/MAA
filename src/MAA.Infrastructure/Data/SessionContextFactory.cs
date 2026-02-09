using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace MAA.Infrastructure.Data;

/// <summary>
/// Design-time factory for SessionContext to support EF Core migrations.
/// This is only used during migration generation, not at runtime.
/// </summary>
public class SessionContextFactory : IDesignTimeDbContextFactory<SessionContext>
{
    public SessionContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<SessionContext>();
        
        // Use a dummy connection string for migrations generation
        // Actual connection string comes from appsettings at runtime
        optionsBuilder.UseNpgsql("Host=localhost;Port=5432;Database=maa_dev;Username=maa;Password=devpass");

        return new SessionContext(optionsBuilder.Options);
    }
}
