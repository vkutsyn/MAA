using MAA.API.Middleware;
using MAA.Application.Services;
using MAA.Application.Sessions;
using MAA.Application.Eligibility.Handlers;
using MAA.Application.Eligibility.Repositories;
using MAA.Application.Eligibility.Caching;
using MAA.Application.Eligibility.Validators;
using MAA.Domain.Repositories;
using MAA.Domain.Rules;
using MAA.Infrastructure.Data;
using MAA.Infrastructure.DataAccess;
using MAA.Infrastructure.Caching;
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

    // Register repositories
    builder.Services.AddScoped<ISessionRepository, SessionRepository>();
    builder.Services.AddScoped<ISessionAnswerRepository, SessionAnswerRepository>();

    // Add AutoMapper
    builder.Services.AddAutoMapper(typeof(MappingProfile));

    // Add memory cache for key caching (US4: Encryption Service)
    builder.Services.AddMemoryCache();

    // Register infrastructure services (US4: Azure Key Vault integration)
    builder.Services.AddScoped<IKeyVaultClient, MAA.Infrastructure.Security.KeyVaultClient>();

    // Register domain services
    builder.Services.AddScoped<ISessionService, SessionService>();
    builder.Services.AddScoped<IEncryptionService, MAA.Infrastructure.Security.EncryptionService>(); // US4: Production implementation with Azure Key Vault

    // Register command/query handlers (US2: Session Data Persistence)
    builder.Services.AddScoped<MAA.Application.Sessions.Commands.SaveAnswerCommandHandler>();
    builder.Services.AddScoped<MAA.Application.Sessions.Queries.GetAnswersQueryHandler>();
    
    // Register validators
    builder.Services.AddScoped<MAA.Application.Sessions.Validators.SaveAnswerCommandValidator>();
    
    // Register Rules domain services (Phase 3-5: E2 Feature)
    builder.Services.AddTransient<RuleEngine>();
    builder.Services.AddTransient<FPLCalculator>();
    
    // Register Rules application layer services
    builder.Services.AddScoped<IEvaluateEligibilityHandler, EvaluateEligibilityHandler>();
    builder.Services.AddScoped<EligibilityInputValidator>();
    
    // Register Rules infrastructure services
    builder.Services.AddScoped<IRuleRepository, RuleRepository>();
    builder.Services.AddScoped<IFplRepository, FplRepository>();
    builder.Services.AddScoped<IRuleCacheService, RuleCacheService>();

    // Add controllers
    builder.Services.AddControllers();

    var app = builder.Build();

    // Configure the HTTP request pipeline
    if (app.Environment.IsDevelopment())
    {
        app.MapOpenApi();
        app.UseDeveloperExceptionPage();
    }

    // Global exception handler middleware
    app.UseMiddleware<GlobalExceptionHandlerMiddleware>();

    // Session validation middleware - validates session cookies and timeouts
    // Must be before routing to validate all requests except bypass paths
    app.UseMiddleware<SessionMiddleware>();

    // US3: Admin role-based access control middleware
    // Enforces Admin/Reviewer/Analyst roles for /api/admin/* endpoints
    app.UseMiddleware<AdminRoleMiddleware>();

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
