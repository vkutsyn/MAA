using FluentAssertions;
using MAA.Tests.Integration;
using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace MAA.Tests.Contract;

/// <summary>
/// Contract tests for Admin API endpoints.
/// Tests US3: Role-Based Access Control enforcement at API level.
/// Validates that admin endpoints properly reject unauthorized requests.
/// </summary>
public class AdminApiContractTests : IAsyncLifetime
{
    private TestWebApplicationFactory? _factory;
    private HttpClient? _httpClient;

    public async Task InitializeAsync()
    {
        _factory = new TestWebApplicationFactory();
        _httpClient = _factory.CreateClient();
        await Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        _httpClient?.Dispose();
        if (_factory != null)
            await _factory.DisposeAsync();
    }

    #region US3: Authorization Tests

    /// <summary>
    /// US3 Acceptance Scenario 1: No authorization header → 403 Forbidden.
    /// </summary>
    [Fact]
    public async Task GetRules_NoAuthorization_Returns403Forbidden()
    {
        // Arrange
        // No authorization header set

        // Act
        var response = await _httpClient!.GetAsync("/api/admin/rules");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden,
            "admin endpoints should reject requests without authorization");

        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Forbidden", "error message should indicate forbidden access");
    }

    /// <summary>
    /// US3 Acceptance Scenario 2: role="User" → 403 Forbidden.
    /// </summary>
    [Fact]
    public async Task GetRules_UserRole_Returns403Forbidden()
    {
        // Arrange
        _httpClient!.DefaultRequestHeaders.Add("X-User-Role", "User");

        // Act
        var response = await _httpClient.GetAsync("/api/admin/rules");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden,
            "regular users should not access admin endpoints");

        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Insufficient permissions",
            "error should explain insufficient permissions");
    }

    /// <summary>
    /// US3 Acceptance Scenario 3: role="Admin" → 200 OK.
    /// </summary>
    [Fact]
    public async Task GetRules_AdminRole_Returns200OK()
    {
        // Arrange
        _httpClient!.DefaultRequestHeaders.Add("X-User-Role", "Admin");

        // Act
        var response = await _httpClient.GetAsync("/api/admin/rules");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK,
            "Admin role should have access to admin endpoints");
    }

    /// <summary>
    /// US3 Acceptance Scenario 4: role="Reviewer" → 200 OK for approval endpoints.
    /// </summary>
    [Fact]
    public async Task GetApprovalQueue_ReviewerRole_Returns200OK()
    {
        // Arrange
        _httpClient!.DefaultRequestHeaders.Add("X-User-Role", "Reviewer");

        // Act
        var response = await _httpClient.GetAsync("/api/admin/approval-queue");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK,
            "Reviewer role should access approval queue");
    }

    /// <summary>
    /// US3: role="Analyst" → 200 OK for read-only endpoints.
    /// </summary>
    [Fact]
    public async Task GetRules_AnalystRole_Returns200OK()
    {
        // Arrange
        _httpClient!.DefaultRequestHeaders.Add("X-User-Role", "Analyst");

        // Act
        var response = await _httpClient.GetAsync("/api/admin/rules");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK,
            "Analyst role should access admin endpoints");
    }

    /// <summary>
    /// POST request without authorization → 403 Forbidden.
    /// </summary>
    [Fact]
    public async Task CreateRule_NoAuthorization_Returns403Forbidden()
    {
        // Arrange
        var payload = new { name = "Test Rule", conditions = "{}" };

        // Act
        var response = await _httpClient!.PostAsJsonAsync("/api/admin/rules", payload);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden,
            "POST to admin endpoints should require authorization");
    }

    /// <summary>
    /// Anonymous role → 403 Forbidden for admin endpoints.
    /// </summary>
    [Fact]
    public async Task GetRules_AnonymousRole_Returns403Forbidden()
    {
        // Arrange
        _httpClient!.DefaultRequestHeaders.Add("X-User-Role", "Anonymous");

        // Act
        var response = await _httpClient.GetAsync("/api/admin/rules");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden,
            "anonymous users should not access admin endpoints");
    }

    #endregion
}
