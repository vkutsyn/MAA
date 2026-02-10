using FluentAssertions;
using MAA.Application.Sessions.DTOs;
using MAA.Tests.Integration;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace MAA.Tests.Integration.Wizard;

/// <summary>
/// Integration tests for dynamic wizard navigation.
/// </summary>
[Collection("Database collection")]
public class WizardNextStepTests : IAsyncLifetime
{
    private readonly DatabaseFixture _databaseFixture;
    private TestWebApplicationFactory? _factory;
    private HttpClient? _httpClient;

    public WizardNextStepTests(DatabaseFixture databaseFixture)
    {
        _databaseFixture = databaseFixture;
    }

    public async Task InitializeAsync()
    {
        _factory = TestWebApplicationFactory.CreateWithDatabase(_databaseFixture);
        _httpClient = _factory.CreateClient();

        await _databaseFixture.ClearAllDataAsync();
        await Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        _httpClient?.Dispose();
        if (_factory != null)
            await _factory.DisposeAsync();

        await Task.CompletedTask;
    }

    [Fact]
    public async Task GetNextStep_WithHouseholdSizeGreaterThanOne_ReturnsMembersStep()
    {
        var createSession = new CreateSessionDto
        {
            IpAddress = "192.168.1.20",
            UserAgent = "WizardNextStepTests/1.0"
        };

        var sessionResponse = await _httpClient!.PostAsJsonAsync("/api/sessions", createSession);
        sessionResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var sessionDto = await sessionResponse.Content.ReadAsAsync<SessionDto>();
        var sessionId = sessionDto!.Id;

        var saveAnswerRequest = new
        {
            sessionId,
            stepId = "household-size",
            schemaVersion = "v1",
            status = "submitted",
            answerData = new { householdSize = 2 }
        };

        var saveResponse = await _httpClient.PostAsJsonAsync("/api/wizard-session/answers", saveAnswerRequest);
        saveResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var nextStepRequest = new
        {
            sessionId,
            currentStepId = "household-size"
        };

        var nextStepResponse = await _httpClient.PostAsJsonAsync("/api/wizard-session/next-step", nextStepRequest);

        nextStepResponse.StatusCode.Should().Be(HttpStatusCode.OK,
            "Next-step should return 200 OK when definition exists");

        var payload = await nextStepResponse.Content.ReadFromJsonAsync<JsonElement>();
        var stepId = payload.GetProperty("stepDefinition").GetProperty("stepId").GetString();

        stepId.Should().Be("household-members");
    }
}
