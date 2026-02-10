using System.Diagnostics;
using System.IO;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.Swagger;
using Xunit;
using Xunit.Abstractions;

namespace MAA.Tests.Integration;

/// <summary>
/// Performance checks for OpenAPI generation and Swagger services.
/// </summary>
public class OpenApiPerformanceTests
{
    private readonly ITestOutputHelper _output;

    public OpenApiPerformanceTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void OpenApiDocument_Generation_Under_30ms()
    {
        using var serviceProvider = BuildSwaggerServiceProvider();
        using var scope = serviceProvider.CreateScope();
        var swaggerProvider = scope.ServiceProvider.GetRequiredService<ISwaggerProvider>();

        // Warm up to avoid first-call overhead.
        swaggerProvider.GetSwagger("v1");

        var stopwatch = Stopwatch.StartNew();
        swaggerProvider.GetSwagger("v1");
        stopwatch.Stop();

        _output.WriteLine("OpenAPI generation time: {0} ms", stopwatch.ElapsedMilliseconds);
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(30,
            "OpenAPI document generation should remain under the 30 ms target");
    }

    [Fact]
    public void SwaggerService_Resolution_Under_100ms()
    {
        var stopwatch = Stopwatch.StartNew();
        using var serviceProvider = BuildSwaggerServiceProvider();
        using var scope = serviceProvider.CreateScope();
        var swaggerProvider = scope.ServiceProvider.GetRequiredService<ISwaggerProvider>();
        stopwatch.Stop();

        swaggerProvider.Should().NotBeNull();
        _output.WriteLine("Swagger service resolution time: {0} ms", stopwatch.ElapsedMilliseconds);
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(100,
            "Swagger registration should not add noticeable startup latency");
    }

    private static ServiceProvider BuildSwaggerServiceProvider()
    {
        var services = new ServiceCollection();

        services.AddLogging();
        services.AddSingleton<IWebHostEnvironment>(new TestWebHostEnvironment());
        services.AddSingleton<IHostEnvironment>(sp => sp.GetRequiredService<IWebHostEnvironment>());
        services.AddControllers()
            .AddApplicationPart(typeof(Program).Assembly);
        services.AddEndpointsApiExplorer();

        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "MAA API",
                Version = "1.0.0",
                Description = "Swagger performance test document"
            });

            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                In = ParameterLocation.Header,
                Description = "JWT Authorization header using the Bearer scheme"
            });

            options.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "Bearer"
                        }
                    },
                    Array.Empty<string>()
                }
            });
        });

        return services.BuildServiceProvider();
    }

    private sealed class TestWebHostEnvironment : IWebHostEnvironment
    {
        public string ApplicationName { get; set; } = "MAA.API";
        public IFileProvider WebRootFileProvider { get; set; } = new PhysicalFileProvider(Directory.GetCurrentDirectory());
        public string WebRootPath { get; set; } = Directory.GetCurrentDirectory();
        public string EnvironmentName { get; set; } = "Test";
        public string ContentRootPath { get; set; } = Directory.GetCurrentDirectory();
        public IFileProvider ContentRootFileProvider { get; set; } = new PhysicalFileProvider(Directory.GetCurrentDirectory());
    }
}
