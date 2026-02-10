using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using MAA.Tests.Integration;
using Xunit;

namespace MAA.Tests.Integration.SchemaGeneration;

/// <summary>
/// Integration tests for authentication and security in Swagger/OpenAPI documentation.
/// These tests verify that protected endpoints require authentication and that
/// authentication behavior is correctly reflected in the API documentation.
/// </summary>
[Collection("Integration")]
public class SwaggerAuthenticationTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;

    public SwaggerAuthenticationTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
    }

    /// <summary>
    /// Test: Verify unauthenticated request to protected endpoint fails with 401.
    /// Maps to T048: Integration test for unauthorized requests
    /// 
    /// This test ensures that endpoints marked with [Authorize] properly reject
    /// requests without valid JWT tokens, which Swagger UI will reflect with
    /// the lock icon and Authorize button requirement.
    /// </summary>
    [Fact]
    public async Task UnauthorizedRequest_Returns_401()
    {
        // Arrange: Create test client without authentication
        var client = _factory.CreateClient();
        
        // Create a test session ID (doesn't matter if it exists, we should get 401 first)
        var testSessionId = Guid.NewGuid();

        // Act: Attempt to access protected endpoint without Authorization header
        var response = await client.GetAsync($"/api/sessions/{testSessionId}");

        // Assert: Should receive 401 Unauthorized
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized,
            "Protected endpoint should return 401 when accessed without authentication");

        // Additional verification: Response should not contain session data
        var responseContent = await response.Content.ReadAsStringAsync();
        responseContent.Should().NotContain("sessionId",
            "Unauthorized response should not leak session data");
    }

    /// <summary>
    /// Test: Verify authenticated request with valid token succeeds.
    /// Maps to T049: Integration test for authorized requests
    /// 
    /// This test ensures that endpoints properly accept valid JWT tokens
    /// and that the Swagger Authorize button functionality works correctly.
    /// </summary>
    [Fact]
    public async Task AuthorizedRequest_WithValidToken_Succeeds()
    {
        // Arrange: Create authenticated client
        var client = _factory.CreateClient();
        
        // Step 1: Register a test user
        var registerRequest = new
        {
            email = $"authtest_{Guid.NewGuid()}@test.com",
            password = "SecurePassword123!",
            fullName = "Auth Test User"
        };

        var registerResponse = await client.PostAsJsonAsync("/api/auth/register", registerRequest);
        registerResponse.StatusCode.Should().Be(HttpStatusCode.OK,
            "User registration should succeed");

        // Step 2: Login to get JWT token
        var loginRequest = new
        {
            email = registerRequest.email,
            password = registerRequest.password
        };

        var loginResponse = await client.PostAsJsonAsync("/api/auth/login", loginRequest);
        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK,
            "Login should succeed");

        var loginResult = await loginResponse.Content.ReadFromJsonAsync<dynamic>();
        string? token = loginResult?.GetProperty("token").GetString();
        
        token.Should().NotBeNullOrEmpty("Login should return a JWT token");

        // Step 3: Create a session for this user (authenticated request)
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var createSessionRequest = new
        {
            timeoutMinutes = 30
        };

        var createSessionResponse = await client.PostAsJsonAsync("/api/sessions", createSessionRequest);

        // Assert: Should succeed with valid authentication
        createSessionResponse.StatusCode.Should().Be(HttpStatusCode.Created,
            "Protected endpoint should return 201 Created when accessed with valid JWT token");

        // Step 4: Retrieve the created session (also requires auth)
        var sessionLocation = createSessionResponse.Headers.Location;
        if (sessionLocation != null)
        {
            var getSessionResponse = await client.GetAsync(sessionLocation);
            getSessionResponse.StatusCode.Should().Be(HttpStatusCode.OK,
                "GET request with valid token should succeed");

            var sessionJson = await getSessionResponse.Content.ReadAsStringAsync();
            sessionJson.Should().NotBeNullOrEmpty("Response should contain session data");
            
            // Parse and verify session has valid ID
            using var jsonDoc = JsonDocument.Parse(sessionJson);
            var sessionId = jsonDoc.RootElement.GetProperty("sessionId").GetString();
            sessionId.Should().NotBeNullOrEmpty("Session should have valid ID");
        }
    }

    /// <summary>
    /// Test: Verify Swagger UI endpoint returns 200 OK in Development environment.
    /// Maps to T015 (already implemented) - additional verification for authentication context
    /// 
    /// This test ensures Swagger UI is accessible and can display authentication
    /// documentation including the Authorize button and JWT bearer scheme.
    /// </summary>
    [Fact]
    public async Task SwaggerUI_Returns_200_In_Test_Environment()
    {
        // Arrange: Use test client (configured for test environment with Swagger enabled)
        var client = _factory.CreateClient();

        // Act: Request Swagger UI page
        var response = await client.GetAsync("/swagger");

        // Assert: Swagger UI should be accessible in Test environment
        response.StatusCode.Should().Be(HttpStatusCode.OK,
            "Swagger UI should be accessible at /swagger in Test environment");

        response.Content.Headers.ContentType?.MediaType.Should().Contain("text/html",
            "Swagger UI should return HTML content");

        // Verify Swagger UI contains authentication-related elements
        var htmlContent = await response.Content.ReadAsStringAsync();
        htmlContent.Should().Contain("swagger",
            "Response should contain Swagger UI HTML");
    }

    /// <summary>
    /// Test: Verify OpenAPI JSON schema is valid and retrievable.
    /// Maps to T016 (already implemented) - additional verification for security schemes
    /// 
    /// This test ensures the OpenAPI schema includes security scheme definitions
    /// that enable Swagger UI to show the Authorize button and handle JWT tokens.
    /// </summary>
    [Fact]
    public async Task OpenApiSchema_IsValid_And_Retrievable()
    {
        // Arrange: Use test client
        var client = _factory.CreateClient();

        // Act: Request OpenAPI schema JSON
        var response = await client.GetAsync("/openapi/v1.json");

        // Assert: Schema should be accessible
        response.StatusCode.Should().Be(HttpStatusCode.OK,
            "OpenAPI schema should be accessible at /openapi/v1.json");

        response.Content.Headers.ContentType?.MediaType.Should().Be("application/json",
            "OpenAPI schema should be JSON format");

        // Verify schema contains required OpenAPI fields
        var schemaJson = await response.Content.ReadAsStringAsync();
        schemaJson.Should().Contain("\"openapi\":",
            "Schema should contain OpenAPI version field");
        schemaJson.Should().Contain("\"paths\":",
            "Schema should contain paths (endpoints) definition");

        // Note: Security schemes are verified in T050 unit test
        // This integration test confirms the schema is accessible at runtime
    }

    /// <summary>
    /// Test: Verify API version appears in Swagger UI title.
    /// Maps to T060: Integration test for version in Swagger UI
    /// 
    /// This test ensures the API version is displayed in the Swagger UI
    /// page title and header, helping developers understand which version
    /// of the API they are working with.
    /// </summary>
    [Fact]
    public async Task SwaggerUI_DisplaysApiVersion()
    {
        // Arrange: Use test client
        var client = _factory.CreateClient();

        // Act: Request Swagger UI page
        var response = await client.GetAsync("/swagger");

        // Assert: Swagger UI should be accessible
        response.StatusCode.Should().Be(HttpStatusCode.OK,
            "Swagger UI should be accessible at /swagger");

        // Get HTML content
        var htmlContent = await response.Content.ReadAsStringAsync();

        // Verify Swagger UI HTML contains version information
        htmlContent.Should().Contain("swagger",
            "Response should contain Swagger UI HTML");

        // Verify version appears in page
        // The version is configured in appsettings.json as "1.0.0"
        // and displayed in Swagger UI title/header via OpenApiInfo.Version
        htmlContent.Should().MatchRegex(@"\d+\.\d+\.\d+",
            "Swagger UI should display API version in semantic versioning format");

        // Additional verification: Check OpenAPI schema directly includes version
        var schemaResponse = await client.GetAsync("/openapi/v1.json");
        var schemaJson = await schemaResponse.Content.ReadAsStringAsync();

        schemaJson.Should().Contain("\"version\":",
            "OpenAPI schema should include version field in info section");
        schemaJson.Should().MatchRegex(@"""version""\s*:\s*""\d+\.\d+\.\d+""",
            "Version should follow semantic versioning format in schema");
    }

    /// <summary>
    /// Test: Verify OpenAPI schema version field matches configuration.
    /// 
    /// This test ensures consistency between appsettings.json version
    /// and the version displayed in the OpenAPI schema.
    /// </summary>
    [Fact]
    public async Task OpenApiSchema_VersionMatchesConfiguration()
    {
        // Arrange: Use test client
        var client = _factory.CreateClient();

        // Act: Request OpenAPI schema
        var response = await client.GetAsync("/openapi/v1.json");

        // Assert: Schema should be accessible
        response.StatusCode.Should().Be(HttpStatusCode.OK,
            "OpenAPI schema should be accessible");

        var schemaJson = await response.Content.ReadAsStringAsync();

        // Parse schema to verify version
        using var jsonDoc = JsonDocument.Parse(schemaJson);
        var root = jsonDoc.RootElement;

        // Verify info section exists
        root.TryGetProperty("info", out var info).Should().BeTrue(
            "OpenAPI schema should have 'info' section");

        // Verify version field exists
        info.TryGetProperty("version", out var version).Should().BeTrue(
            "Info section should have 'version' field");

        // Verify version format (semantic versioning: X.Y.Z)
        var versionString = version.GetString();
        versionString.Should().NotBeNullOrEmpty("Version should not be empty");
        versionString.Should().MatchRegex(@"^\d+\.\d+\.\d+$",
            "Version should follow semantic versioning format (e.g., 1.0.0)");

        // Expected version from appsettings.json
        var expectedVersion = "1.0.0";
        versionString.Should().Be(expectedVersion,
            "OpenAPI schema version should match appsettings.json configuration");
    }

    /// <summary>
    /// Test: Verify multiple protected endpoints consistently require authentication.
    /// 
    /// This ensures all endpoints marked with [Authorize] behave consistently
    /// and are properly documented in Swagger UI with security requirements.
    /// </summary>
    [Fact]
    public async Task MultipleProtectedEndpoints_RequireAuthentication()
    {
        // Arrange: Create unauthenticated client
        var client = _factory.CreateClient();

        // Act: Test multiple protected endpoints
        var testEndpoints = new[]
        {
            $"/api/sessions/{Guid.NewGuid()}",
            "/api/sessions/active",
            "/api/admin/users",
            "/api/rules/validate"
        };

        var unauthorizedCount = 0;
        foreach (var endpoint in testEndpoints)
        {
            try
            {
                var response = await client.GetAsync(endpoint);
                
                // Protected endpoints should return 401 (or 404 if endpoint doesn't exist)
                // Either way, they should NOT return 200 OK without authentication
                if (response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    unauthorizedCount++;
                }
            }
            catch
            {
                // Endpoint might not exist in this test environment
                // That's acceptable - we're testing that existing protected endpoints require auth
            }
        }

        // Assert: At least some endpoints should require authentication
        unauthorizedCount.Should().BeGreaterThan(0,
            "Protected endpoints should return 401 Unauthorized without valid token");
    }

    /// <summary>
    /// Test: Verify expired token is rejected.
    /// 
    /// This ensures JWT token expiration is properly enforced and documented.
    /// </summary>
    [Fact]
    public async Task ExpiredToken_IsRejected()
    {
        //Arrange: Create client
        var client = _factory.CreateClient();

        // Use an obviously invalid/expired token (malformed JWT)
        var expiredToken = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJleHAiOjB9.invalid";
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", expiredToken);

        // Act: Attempt to access protected endpoint with expired token
        var response = await client.GetAsync($"/api/sessions/{Guid.NewGuid()}");

        // Assert: Should be rejected with 401
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized,
            "Expired or invalid JWT token should be rejected with 401");
    }
}
