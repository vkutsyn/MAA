using FluentAssertions;
using MAA.Tests.Integration;
using System.Net;
using Xunit;

namespace MAA.Tests.Contract;

/// <summary>
/// Contract tests validating wizard step detail endpoint.
/// </summary>
public class WizardStepDetailContractTests : IAsyncLifetime
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
    public async Task GetStepDetail_Nonexistent_ReturnsNotFound()
    {
        var sessionId = Guid.NewGuid();

        var response = await _httpClient!.GetAsync($"/api/wizard-session/{sessionId}/steps/household-size");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound,
            "GET /api/wizard-session/{sessionId}/steps/{stepId} should return 404 when wizard session is missing");
    }
}
