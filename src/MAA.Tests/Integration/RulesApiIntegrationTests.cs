using FluentAssertions;
using MAA.Tests.Fixtures;
using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Xunit;

namespace MAA.Tests.Integration;

/// <summary>
/// Integration tests for Rules API using WebApplicationFactory + Testcontainers.PostgreSQL.
/// Validates end-to-end eligibility evaluation via HTTP with real database.
/// </summary>
[Collection("Database collection")]
[Trait("Category", "Integration")]
public class RulesApiIntegrationTests : IAsyncLifetime
{
    private readonly DatabaseFixture _databaseFixture;
    private TestWebApplicationFactory? _factory;
    private HttpClient? _httpClient;

    public RulesApiIntegrationTests(DatabaseFixture databaseFixture)
    {
        _databaseFixture = databaseFixture;
    }

    public async Task InitializeAsync()
    {
        _factory = new TestWebApplicationFactory(_databaseFixture);
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
    public async Task EvaluateEligibility_IL_ReturnsOkAndResult()
    {
        var payload = CreateEvaluatePayload(stateCode: "IL");

        var response = await _httpClient!.PostAsJsonAsync("/api/rules/evaluate", payload);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var json = await response.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;

        root.TryGetProperty("evaluation_date", out _).Should().BeTrue();
        root.TryGetProperty("status", out _).Should().BeTrue();
        root.TryGetProperty("matched_programs", out _).Should().BeTrue();
        root.TryGetProperty("explanation", out _).Should().BeTrue();
        root.TryGetProperty("rule_version_used", out _).Should().BeTrue();
        root.TryGetProperty("confidence_score", out _).Should().BeTrue();
    }

    [Fact]
    public async Task EvaluateEligibility_CA_ReturnsOk()
    {
        var payload = CreateEvaluatePayload(stateCode: "CA");

        var response = await _httpClient!.PostAsJsonAsync("/api/rules/evaluate", payload);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task EvaluateEligibility_TX_DiffersFromIL()
    {
        var ilPayload = CreateEvaluatePayload(stateCode: "IL");
        var txPayload = CreateEvaluatePayload(stateCode: "TX");

        var ilResponse = await _httpClient!.PostAsJsonAsync("/api/rules/evaluate", ilPayload);
        var txResponse = await _httpClient!.PostAsJsonAsync("/api/rules/evaluate", txPayload);

        ilResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        txResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var ilJson = await ilResponse.Content.ReadAsStringAsync();
        var txJson = await txResponse.Content.ReadAsStringAsync();

        using var ilDoc = JsonDocument.Parse(ilJson);
        using var txDoc = JsonDocument.Parse(txJson);

        var ilStatus = ilDoc.RootElement.GetProperty("status").GetString();
        var txStatus = txDoc.RootElement.GetProperty("status").GetString();

        ilStatus.Should().NotBeNull();
        txStatus.Should().NotBeNull();
        txStatus.Should().NotBe(ilStatus, "TX and IL thresholds should differ");
    }

    [Fact]
    public async Task EvaluateEligibility_MissingRuleForState_ReturnsNotFound()
    {
        var payload = CreateEvaluatePayload(stateCode: "ZZ");

        var response = await _httpClient!.PostAsJsonAsync("/api/rules/evaluate", payload);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task EvaluateEligibility_InvalidJson_ReturnsBadRequest()
    {
        var invalidJson = "{ \"state_code\": \"IL\", \"household_size\": 2 ";
        var content = new StringContent(invalidJson, Encoding.UTF8, "application/json");

        var response = await _httpClient!.PostAsync("/api/rules/evaluate", content);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task EvaluateEligibility_Performance_UnderTwoSeconds()
    {
        var payload = CreateEvaluatePayload(stateCode: "IL");
        var stopwatch = Stopwatch.StartNew();

        var response = await _httpClient!.PostAsJsonAsync("/api/rules/evaluate", payload);

        stopwatch.Stop();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        stopwatch.Elapsed.Should().BeLessThan(TimeSpan.FromSeconds(2));
    }

    [Fact]
    public async Task EvaluateEligibility_IsDeterministicAcrossRequests()
    {
        var payload = CreateEvaluatePayload(stateCode: "IL");

        var firstResponse = await _httpClient!.PostAsJsonAsync("/api/rules/evaluate", payload);
        var secondResponse = await _httpClient!.PostAsJsonAsync("/api/rules/evaluate", payload);

        firstResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        secondResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var firstJson = await firstResponse.Content.ReadAsStringAsync();
        var secondJson = await secondResponse.Content.ReadAsStringAsync();

        using var firstDoc = JsonDocument.Parse(firstJson);
        using var secondDoc = JsonDocument.Parse(secondJson);

        var firstStatus = firstDoc.RootElement.GetProperty("status").GetString();
        var secondStatus = secondDoc.RootElement.GetProperty("status").GetString();
        var firstScore = firstDoc.RootElement.GetProperty("confidence_score").GetInt32();
        var secondScore = secondDoc.RootElement.GetProperty("confidence_score").GetInt32();

        firstStatus.Should().Be(secondStatus);
        firstScore.Should().Be(secondScore);
    }

    [Fact]
    public async Task EvaluateEligibility_InvalidHouseholdSize_ReturnsBadRequest()
    {
        var payload = CreateEvaluatePayload(stateCode: "IL");
        payload["household_size"] = 0;

        var response = await _httpClient!.PostAsJsonAsync("/api/rules/evaluate", payload);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    private static Dictionary<string, object?> CreateEvaluatePayload(string stateCode)
    {
        return new Dictionary<string, object?>
        {
            ["state_code"] = stateCode,
            ["household_size"] = 2,
            ["monthly_income_cents"] = 210_000,
            ["age"] = 35,
            ["has_disability"] = false,
            ["is_pregnant"] = false,
            ["receives_ssi"] = false,
            ["is_citizen"] = true,
            ["assets_cents"] = null
        };
    }
}
