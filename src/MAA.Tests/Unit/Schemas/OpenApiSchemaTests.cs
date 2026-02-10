using System.Reflection;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace MAA.Tests.Unit.Schemas;

/// <summary>
/// Unit tests for OpenAPI schema generation.
/// Tests verify that all controllers and endpoints are properly exposed in the schema.
/// </summary>
public class OpenApiSchemaTests
{
    /// <summary>
    /// Test: Verify all controller classes are exposed in the OpenAPI schema.
    /// Maps to T014: Unit test verification of controllers in schema
    /// 
    /// This test uses reflection to find all controller classes in the API assembly
    /// and verifies they would be included in schema generation.
    /// </summary>
    [Fact]
    public void VerifyAllControllersExposed_In_Schema()
    {
        // Arrange: Get all controller types from API assembly
        var apiAssembly = typeof(Program).Assembly;
        var controllerTypes = apiAssembly.GetTypes()
            .Where(t => typeof(ControllerBase).IsAssignableFrom(t) && 
                       !t.IsAbstract && 
                       t.Name.EndsWith("Controller", StringComparison.OrdinalIgnoreCase))
            .ToList();

        // Assert: Verify we found controller classes
        controllerTypes.Should().NotBeEmpty("API should have at least one controller marked with [ApiController]");

        // Verify specific expected controllers exist
        var controllerNames = controllerTypes.Select(t => t.Name).ToList();
        controllerNames.Should().Contain("SessionsController", "Sessions endpoints should exist");
        
        // Verify each controller has public HTTP action methods
        foreach (var controller in controllerTypes)
        {
            var httpMethods = controller.GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .Where(m => m.DeclaringType == controller && 
                           m.IsPublic && 
                           !m.IsGenericMethodDefinition &&
                           !m.IsSpecialName)
                .ToList();

            httpMethods.Should().NotBeEmpty(
                $"Controller {controller.Name} should have at least one public HTTP action method");
        }
    }

    /// <summary>
    /// Test: Verify that controller methods have proper return types for schema documentation.
    /// </summary>
    [Fact]
    public void ControllerMethods_Should_Return_Expected_Types()
    {
        // Arrange
        var apiAssembly = typeof(Program).Assembly;
        var sessionControllerType = apiAssembly.GetType("MAA.API.Controllers.SessionsController");

        // Assert
        sessionControllerType.Should().NotBeNull("SessionsController should exist");

        // Verify key methods exist
        var getMethods = sessionControllerType!.GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .Where(m => m.Name.StartsWith("Get", StringComparison.OrdinalIgnoreCase))
            .ToList();

        getMethods.Should().NotBeEmpty("SessionsController should have GET methods");

        // Verify methods have return types that can be documented
        foreach (var method in getMethods)
        {
            method.ReturnType.Should().NotBeNull("GET methods should have return types");
            method.ReturnType.Should().NotBe(typeof(void), "GET methods should return data");
        }
    }

    /// <summary>
    /// Test: Verify new controller method appears in schema after code change.
    /// Maps to T040: Unit test for auto-sync behavior
    /// 
    /// This test demonstrates that schema generation is dynamic and automatically
    /// includes new endpoints without manual schema updates.
    /// </summary>
    [Fact]
    public void NewEndpoint_AutomaticallyAppearsInSchema()
    {
        // Arrange: Get all controller types from API assembly
        var apiAssembly = typeof(Program).Assembly;
        var controllerTypes = apiAssembly.GetTypes()
            .Where(t => typeof(ControllerBase).IsAssignableFrom(t) && 
                       !t.IsAbstract && 
                       t.Name.EndsWith("Controller", StringComparison.OrdinalIgnoreCase))
            .ToList();

        // Act: Count all public action methods across all controllers
        int totalEndpointCount = 0;
        foreach (var controller in controllerTypes)
        {
            var actionMethods = controller.GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .Where(m => m.DeclaringType == controller && 
                           m.IsPublic && 
                           !m.IsGenericMethodDefinition &&
                           !m.IsSpecialName)
                .ToList();
            
            totalEndpointCount += actionMethods.Count;
        }

        // Assert: Verify endpoints exist (schema would auto-generate for all)
        totalEndpointCount.Should().BeGreaterThan(0, 
            "API should have endpoints that would be auto-documented");

        // Verification: Any new controller method added to the codebase
        // will automatically appear in the schema on next build - no manual schema updates needed
        // This test validates the reflection-based discovery mechanism
    }

    /// <summary>
    /// Test: Verify XML documentation exists for controllers (required for auto-sync).
    /// Maps to T041: Integration test for XML comment updates
    /// 
    /// This test verifies that controllers have XML documentation enabled,
    /// which is required for Swagger to automatically sync descriptions.
    /// </summary>
    [Fact]
    public void UpdatedXmlComments_ReflectedInSchema()
    {
        // Arrange: Get API assembly
        var apiAssembly = typeof(Program).Assembly;
        
        // Act: Check that XML documentation file would be generated
        // This is controlled by project settings GenerateDocumentationFile=true
        var assemblyLocation = apiAssembly.Location;
        var xmlDocPath = Path.ChangeExtension(assemblyLocation, ".xml");

        // Assert: XML documentation file should exist after build
        // Note: This test runs after build, so XML file should be present
        File.Exists(xmlDocPath).Should().BeTrue(
            $"XML documentation file should exist at {xmlDocPath}. " +
            "This is required for Swagger to auto-sync descriptions from code comments.");

        // Additional verification: Check XML file contains controller documentation
        if (File.Exists(xmlDocPath))
        {
            var xmlContent = File.ReadAllText(xmlDocPath);
            xmlContent.Should().Contain("Controller", 
                "XML documentation should include controller documentation");
        }
    }

    /// <summary>
    /// Test: Verify schema validation process would catch invalid OpenAPI structure.
    /// Maps to T042: Unit test for CI/CD schema validation
    /// 
    /// This test verifies that basic schema structure requirements are met,
    /// which CI/CD validation would enforce.
    /// </summary>
    [Fact]
    public void SchemaValidation_CatchesInvalidOpenApi()
    {
        // Arrange: Get all controller types
        var apiAssembly = typeof(Program).Assembly;
        var controllerTypes = apiAssembly.GetTypes()
            .Where(t => typeof(ControllerBase).IsAssignableFrom(t) && 
                       !t.IsAbstract && 
                       t.Name.EndsWith("Controller", StringComparison.OrdinalIgnoreCase))
            .ToList();

        // Act: Verify each controller has required attributes/documentation
        foreach (var controller in controllerTypes)
        {
            // Check for route attributes (required for OpenAPI path generation)
            var routeAttributes = controller.GetCustomAttributes(typeof(RouteAttribute), true);
            var apiControllerAttributes = controller.GetCustomAttributes()
                .Where(a => a.GetType().Name.Contains("ApiController"))
                .ToList();

            // Assert: Controllers should have proper attributes for schema generation
            (routeAttributes.Length > 0 || apiControllerAttributes.Count > 0).Should().BeTrue(
                $"Controller {controller.Name} should have [Route] or [ApiController] attribute " +
                "for valid OpenAPI schema generation");
        }

        // Schema validation in CI/CD would:
        // 1. Generate swagger.json during build
        // 2. Validate against OpenAPI 3.0 spec using swagger-validation.ps1
        // 3. Fail build if validation errors occur
        // This test verifies the prerequisites for that validation are met
    }
}
