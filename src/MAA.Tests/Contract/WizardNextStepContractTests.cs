using FluentAssertions;
using MAA.Tests.Integration;
using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace MAA.Tests.Contract;

/// <summary>
/// Contract tests validating wizard next-step endpoint.
/// </summary>
public class WizardNextStepContractTests : IAsyncLifetime
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
    public async Task PostNextStep_InvalidPayload_ReturnsBadRequest()
    {
        var request = new { };

        var response = await _httpClient!.PostAsJsonAsync("/api/wizard-session/next-step", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest,
            "POST /api/wizard-session/next-step should validate required fields");
    }
}
