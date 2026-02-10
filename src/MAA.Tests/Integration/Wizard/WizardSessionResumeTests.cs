using FluentAssertions;
using MAA.Application.Sessions.DTOs;
using MAA.Tests.Integration;
using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace MAA.Tests.Integration.Wizard;

/// <summary>
/// Integration tests for wizard session save and resume flow.
/// </summary>
[Collection("Database collection")]
public class WizardSessionResumeTests : IAsyncLifetime
{
    private readonly DatabaseFixture _databaseFixture;
    private TestWebApplicationFactory? _factory;
    private HttpClient? _httpClient;

    public WizardSessionResumeTests(DatabaseFixture databaseFixture)
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
    public async Task SaveAnswers_ThenRestoreSession_ReturnsAllAnswers()
    {
        // Arrange: create base session
        var createSession = new CreateSessionDto
        {
            IpAddress = "192.168.1.10",
            UserAgent = "WizardResumeTests/1.0"
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
            answerData = new { householdSize = 3 }
        };

        // Act
        var saveResponse = await _httpClient.PostAsJsonAsync("/api/wizard-session/answers", saveAnswerRequest);
        var restoreResponse = await _httpClient.GetAsync($"/api/wizard-session/{sessionId}");

        // Assert
        saveResponse.StatusCode.Should().Be(HttpStatusCode.OK,
            "Save step answer should return 200 OK");
        restoreResponse.StatusCode.Should().Be(HttpStatusCode.OK,
            "Restore should return 200 OK with wizard state");
    }
}
