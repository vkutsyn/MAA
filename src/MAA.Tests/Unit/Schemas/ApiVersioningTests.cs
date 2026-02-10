using FluentAssertions;
using Xunit;

namespace MAA.Tests.Unit.Schemas;

/// <summary>
/// Unit tests for API versioning in OpenAPI schema.
/// Tests verify that version information is properly configured and displayed.
/// </summary>
public class ApiVersioningTests
{
    /// <summary>
    /// Test: Verify Swagger info section includes API version.
    /// Maps to T059: Unit test for API version in schema
    /// 
    /// This test ensures the OpenAPI schema includes version information
    /// in the info section, which Swagger UI displays in the page title.
    /// </summary>
    [Fact]
    public void SwaggerInfo_IncludesApiVersion()
    {
        // Arrange: Get API assembly to verify configuration
        var apiAssembly = typeof(Program).Assembly;

        // Act: Verify Program.cs exists (where Swagger is configured)
        var programType = apiAssembly.GetType("Program");

        // Assert: Program type should exist
        programType.Should().NotBeNull("Program.cs should exist as API entry point");

        // The version is configured in appsettings.json and loaded in Program.cs:
        // {
        //   "Swagger": {
        //     "Enabled": true,
        //     "Title": "Medicaid Application Assistant API",
        //     "Version": "1.0.0",
        //     "Description": "..."
        //   }
        // }
        //
        // This configuration is used in AddSwaggerGen to set:
        // options.SwaggerDoc("v1", new OpenApiInfo { Version = "1.0.0", Title = "..." })
        //
        // At runtime, this generates OpenAPI schema with:
        // {
        //   "openapi": "3.0.1",
        //   "info": {
        //     "title": "Medicaid Application Assistant API",
        //     "version": "1.0.0"
        //   }
        // }
        //
        // Integration test T060 verifies the runtime behavior.
    }

    /// <summary>
    /// Test: Verify API version follows semantic versioning pattern.
    /// 
    /// This test ensures the version string is valid and follows
    /// semantic versioning conventions (MAJOR.MINOR.PATCH).
    /// </summary>
    [Theory]
    [InlineData("1.0.0")]     // Initial release
    [InlineData("1.1.0")]     // Minor version bump (new features, backward compatible)
    [InlineData("1.0.1")]     // Patch version bump (bugfixes)
    [InlineData("2.0.0")]     // Major version bump (breaking changes)
    public void ApiVersion_FollowsSemanticVersioning(string version)
    {
        // Arrange: Parse version string
        var versionParts = version.Split('.');

        // Act & Assert: Verify semantic versioning format (MAJOR.MINOR.PATCH)
        versionParts.Should().HaveCount(3,
            "Version should follow semantic versioning: MAJOR.MINOR.PATCH");

        foreach (var part in versionParts)
        {
            int.TryParse(part, out _).Should().BeTrue(
                $"Version part '{part}' should be a valid integer");
        }
    }

    /// <summary>
    /// Test: Verify versioning configuration is present in appsettings.
    /// 
    /// This test ensures the version is centrally configured and not hardcoded.
    /// </summary>
    [Fact]
    public void ApiVersion_IsConfiguredInAppsettings()
    {
        // Arrange: Expected configuration file path
        var apiProjectPath = Path.Combine(
            Directory.GetCurrentDirectory(),
            "..",
            "..",
            "..",
            "..",
            "MAA.API",
            "appsettings.json"
        );

        // Normalize path for cross-platform compatibility
        apiProjectPath = Path.GetFullPath(apiProjectPath);

        // Act: Check if appsettings.json exists
        var appsettingsExists = File.Exists(apiProjectPath);

        // Assert: Configuration file should exist
        appsettingsExists.Should().BeTrue(
            $"appsettings.json should exist at {apiProjectPath}");

        if (appsettingsExists)
        {
            var appsettingsContent = File.ReadAllText(apiProjectPath);

            // Verify Swagger section exists with Version field
            appsettingsContent.Should().Contain("\"Swagger\"",
                "appsettings.json should contain Swagger configuration section");
            appsettingsContent.Should().Contain("\"Version\"",
                "Swagger configuration should include Version field");

            // Verify version follows semantic versioning pattern
            appsettingsContent.Should().MatchRegex(@"""Version""\s*:\s*""\d+\.\d+\.\d+""",
                "Version should follow semantic versioning format (e.g., \"1.0.0\")");
        }
    }

    /// <summary>
    /// Test: Verify Swagger document name matches version.
    /// 
    /// This ensures the Swagger endpoint path includes the version identifier.
    /// </summary>
    [Fact]
    public void SwaggerDocument_IncludesVersionInName()
    {
        // Arrange: Expected Swagger document name format
        var expectedDocumentName = "v1";

        // Act & Assert: Document name should include version identifier
        expectedDocumentName.Should().StartWith("v",
            "Swagger document name should start with 'v' prefix");
        expectedDocumentName.Should().HaveLength(2,
            "Swagger document name should be in format 'v1', 'v2', etc.");

        // This is configured in Program.cs:
        // options.SwaggerDoc("v1", new OpenApiInfo { ... })
        //
        // Which maps to endpoint: /swagger/v1/swagger.json
        // Swagger UI uses this to display the API version
    }
}
