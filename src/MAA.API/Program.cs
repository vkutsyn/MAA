using MAA.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Serilog;

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("logs/maa-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

try
{
    Log.Information("Starting MAA API application");

    var builder = WebApplication.CreateBuilder(args);

    // Add Serilog
    builder.Host.UseSerilog();

    // Add services to the container
    builder.Services.AddOpenApi();

    // Configure Entity Framework Core with PostgreSQL
    builder.Services.AddDbContext<SessionContext>(options =>
        options.UseNpgsql(
            builder.Configuration.GetConnectionString("DefaultConnection"),
            npgsqlOptions => npgsqlOptions.MigrationsAssembly("MAA.Infrastructure")
        )
    );

    // Register domain services (placeholder - will be implemented in later tasks)
    // builder.Services.AddScoped<ISessionService, SessionService>();
    // builder.Services.AddScoped<IEncryptionService, EncryptionService>();

    // Register infrastructure services (placeholder)
    // builder.Services.AddScoped<ISessionRepository, SessionRepository>();
    // builder.Services.AddScoped<IKeyVaultClient, KeyVaultClient>();

    // Add AutoMapper (placeholder)
    // builder.Services.AddAutoMapper(typeof(Program));

    // Add controllers
    builder.Services.AddControllers();

    var app = builder.Build();

    // Configure the HTTP request pipeline
    if (app.Environment.IsDevelopment())
    {
        app.MapOpenApi();
        app.UseDeveloperExceptionPage();
    }

    // Middleware will be added in later tasks:
    // - app.UseMiddleware<SessionMiddleware>();
    // - app.UseMiddleware<AdminRoleMiddleware>();
    // - app.UseMiddleware<GlobalExceptionHandlerMiddleware>();

    app.UseHttpsRedirection();

    app.MapControllers();

    // Health check endpoint
    app.MapGet("/health/ready", async (SessionContext dbContext) =>
    {
        try
        {
            await dbContext.Database.CanConnectAsync();
            return Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Health check failed");
            return Results.Problem("Database unavailable", statusCode: 503);
        }
    })
    .WithName("HealthCheck");

    app.MapGet("/health/live", () => Results.Ok(new { status = "alive", timestamp = DateTime.UtcNow }))
        .WithName("LivenessCheck");

    app.Run();

    return 0;
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
    return 1;
}
finally
{
    Log.CloseAndFlush();
}

// Make Program accessible for WebApplicationFactory<Program> in tests
public partial class Program { }
