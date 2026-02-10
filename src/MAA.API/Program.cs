using MAA.API.Middleware;
using MAA.Application.Services;
using MAA.Application.Sessions;
using MAA.Application.Eligibility.Handlers;
using MAA.Application.Eligibility.Repositories;
using MAA.Application.Eligibility.Caching;
using MAA.Application.Eligibility.Services;
using MAA.Application.Eligibility.Validators;
using MAA.Domain.Repositories;
using MAA.Domain.Rules;
using MAA.Infrastructure.Data;
using MAA.Infrastructure.DataAccess;
using MAA.Infrastructure.Caching;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Swashbuckle.AspNetCore.Filters;

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("logs/maa-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

try
{
    Log.Information("Starting MAA API application");

    var builder = WebApplication.CreateBuilder(args);

    builder.Configuration.AddJsonFile("appsettings.Swagger.json", optional: true, reloadOnChange: true);

    // Add Serilog
    builder.Host.UseSerilog();

    // Add services to the container
    // Note: We're using Swashbuckle/Swagger instead of the built-in OpenApi
    
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
    // In test environment, skip Azure Key Vault - tests provide mock
    if (builder.Environment.IsProduction() || builder.Configuration["Azure:KeyVault:Uri"] == null)
    {
        // Production: Use real Key Vault
        // Test: Use mock (provided by TestWebApplicationFactory or use default)
        if (builder.Environment.IsProduction())
        {
            builder.Services.AddScoped<IKeyVaultClient, MAA.Infrastructure.Security.KeyVaultClient>();
        }
        else
        {
            // For non-production environments without Key Vault config, skip registration
            // Test factory will provide implementation
        }
    }
    else
    {
        // Configuration available, register normally
        builder.Services.AddScoped<IKeyVaultClient, MAA.Infrastructure.Security.KeyVaultClient>();
    }

    // Register domain services
    builder.Services.AddScoped<ISessionService, SessionService>();
    builder.Services.AddScoped<IEncryptionService, MAA.Infrastructure.Security.EncryptionService>(); // US4: Production implementation with Azure Key Vault

    // Register JWT token provider and settings (Phase 5: Auth feature)
    var jwtSettings = new MAA.Infrastructure.Security.JwtSettings();
    builder.Configuration.GetSection("Jwt").Bind(jwtSettings);
    builder.Services.AddSingleton(jwtSettings);
    builder.Services.AddScoped<ITokenProvider>(sp => 
        new MAA.Infrastructure.Security.JwtTokenProvider(
            jwtSettings,
            sp.GetRequiredService<ILogger<MAA.Infrastructure.Security.JwtTokenProvider>>()
        )
    );

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
    builder.Services.AddScoped<IFPLThresholdCalculator, FPLThresholdCalculator>();
    builder.Services.AddScoped<IFPLCacheService, FPLCacheService>();

    // Add controllers
    builder.Services.AddControllers();
    builder.Services.AddOpenApi();

    // Configure Swagger/OpenAPI documentation if enabled
    var swaggerSettings = builder.Configuration.GetSection("Swagger");
    if (swaggerSettings.GetValue<bool>("Enabled", false))
    {
        var swaggerTitle = swaggerSettings.GetValue<string>("Title", "API");
        var swaggerVersion = swaggerSettings.GetValue<string>("Version", "1.0.0");
        var swaggerDescription = swaggerSettings.GetValue<string>("Description", "");

        // Add Swagger/OpenAPI
        builder.Services.AddSwaggerGen(options =>
        {
            // API metadata
            options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
            {
                Title = swaggerTitle,
                Version = swaggerVersion,
                Description = swaggerDescription,
                Contact = new Microsoft.OpenApi.Models.OpenApiContact
                {
                    Name = "MAA Development Team"
                }
            });

            // Enable XML documentation comments
            var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
            var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
            if (File.Exists(xmlPath))
            {
                options.IncludeXmlComments(xmlPath);

                // Include Application layer XML comments for DTOs
                var applicationXmlFile = "MAA.Application.xml";
                var applicationXmlPath = Path.Combine(AppContext.BaseDirectory, applicationXmlFile);
                if (File.Exists(applicationXmlPath))
                {
                    options.IncludeXmlComments(applicationXmlPath);
                }
            }

            // Add JWT Bearer token authentication support
            // This enables the "Authorize" button in Swagger UI
            options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                In = Microsoft.OpenApi.Models.ParameterLocation.Header,
                Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\""
            });

            options.AddFluentValidationRulesProvider();

            // Require Bearer token for all endpoints with [Authorize] attribute
            options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
            {
                {
                    new Microsoft.OpenApi.Models.OpenApiSecurityScheme
                    {
                        Reference = new Microsoft.OpenApi.Models.OpenApiReference
                        {
                            Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                            Id = "Bearer"
                        }
                    },
                    Array.Empty<string>()
                }
            });
        });
    }

    var app = builder.Build();

    // Configure the HTTP request pipeline
    if (app.Environment.IsDevelopment() || app.Environment.IsEnvironment("Test"))
    {
        app.UseDeveloperExceptionPage();

        // Enable Swagger UI in development and test environments
        if (swaggerSettings.GetValue<bool>("Enabled", false))
        {
            app.UseSwagger(options =>
            {
                options.RouteTemplate = "openapi/{documentName}.json";
            });
            app.UseSwaggerUI(options =>
            {
                options.SwaggerEndpoint("/openapi/v1.json", "MAA API v1");
                options.RoutePrefix = "swagger";
                options.DefaultModelsExpandDepth(2);
            });
        }
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
