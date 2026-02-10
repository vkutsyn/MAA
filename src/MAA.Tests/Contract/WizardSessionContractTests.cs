using FluentAssertions;
using MAA.Application.Sessions.DTOs;
using MAA.Tests.Integration;
using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace MAA.Tests.Contract;

/// <summary>
/// Contract tests validating wizard session endpoints match OpenAPI schema.
/// </summary>
public class WizardSessionContractTests : IAsyncLifetime
{
    private HttpClient? _httpClient;
    private TestWebApplicationFactory? _factory;

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

    [Fact]
    public async Task PostSaveStepAnswer_InvalidPayload_ReturnsBadRequest()
    {
        // Arrange
        var request = new { };

        // Act
        var response = await _httpClient!.PostAsJsonAsync("/api/wizard-session/answers", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest,
            "POST /api/wizard-session/answers should validate required fields");
    }

    [Fact]
    public async Task GetWizardSessionState_Nonexistent_ReturnsNotFound()
    {
        // Arrange
        var sessionId = Guid.NewGuid();

        // Act
        var response = await _httpClient!.GetAsync($"/api/wizard-session/{sessionId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound,
            "GET /api/wizard-session/{id} should return 404 when wizard session is missing");
    }
}
