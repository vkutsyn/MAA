using FluentAssertions;
using MAA.Application.Sessions.DTOs;
using MAA.Tests.Integration;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace MAA.Tests.Integration.Wizard;

/// <summary>
/// Integration tests for step review and revalidation flow.
/// </summary>
[Collection("Database collection")]
public class WizardStepReviewTests : IAsyncLifetime
{
    private readonly DatabaseFixture _databaseFixture;
    private TestWebApplicationFactory? _factory;
    private HttpClient? _httpClient;

    public WizardStepReviewTests(DatabaseFixture databaseFixture)
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
    public async Task UpdateEarlierStep_InvalidatesDownstreamSteps()
    {
        var createSession = new CreateSessionDto
        {
            IpAddress = "192.168.1.30",
            UserAgent = "WizardStepReviewTests/1.0"
        };

        var sessionResponse = await _httpClient!.PostAsJsonAsync("/api/sessions", createSession);
        sessionResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var sessionDto = await sessionResponse.Content.ReadAsAsync<SessionDto>();
        var sessionId = sessionDto!.Id;

        var saveFirstStep = new
        {
            sessionId,
            stepId = "household-size",
            schemaVersion = "v1",
            status = "submitted",
            answerData = new { householdSize = 2 }
        };

        var saveSecondStep = new
        {
            sessionId,
            stepId = "household-members",
            schemaVersion = "v1",
            status = "submitted",
            answerData = new { memberCount = 2 }
        };

        var firstResponse = await _httpClient.PostAsJsonAsync("/api/wizard-session/answers", saveFirstStep);
        firstResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var secondResponse = await _httpClient.PostAsJsonAsync("/api/wizard-session/answers", saveSecondStep);
        secondResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var updateFirstStep = new
        {
            sessionId,
            stepId = "household-size",
            schemaVersion = "v1",
            status = "submitted",
            answerData = new { householdSize = 1 }
        };

        var updateResponse = await _httpClient.PostAsJsonAsync("/api/wizard-session/answers", updateFirstStep);
        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var updatePayload = await updateResponse.Content.ReadFromJsonAsync<JsonElement>();
        var invalidatedSteps = updatePayload.GetProperty("invalidatedSteps")
            .EnumerateArray()
            .Select(element => element.GetString())
            .Where(value => value != null)
            .ToList();

        invalidatedSteps.Should().Contain("household-members");

        var stateResponse = await _httpClient.GetAsync($"/api/wizard-session/{sessionId}");
        stateResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var statePayload = await stateResponse.Content.ReadFromJsonAsync<JsonElement>();
        var progressStatus = statePayload.GetProperty("stepProgress")
            .EnumerateArray()
            .First(progress => progress.GetProperty("stepId").GetString() == "household-members")
            .GetProperty("status")
            .GetString();

        progressStatus.Should().Be("requires_revalidation");
    }
}
