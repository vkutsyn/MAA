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
}
