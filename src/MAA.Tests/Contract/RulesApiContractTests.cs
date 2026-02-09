using FluentAssertions;
using MAA.Tests.Integration;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace MAA.Tests.Contract;

/// <summary>
/// Contract tests validating Rules API endpoints match OpenAPI schema.
/// Validates response shapes, status codes, and DTO structure.
/// </summary>
public class RulesApiContractTests : IAsyncLifetime
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
    public async Task PostEvaluateEligibility_ReturnsOkWithEligibilityResultSchema()
    {
        var payload = CreateEvaluatePayload(stateCode: "IL");

        var response = await _httpClient!.PostAsJsonAsync("/api/rules/evaluate", payload);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var json = await response.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;

        root.TryGetProperty("evaluation_date", out _).Should().BeTrue();
        root.TryGetProperty("status", out _).Should().BeTrue();
        root.TryGetProperty("matched_programs", out var matches).Should().BeTrue();
        root.TryGetProperty("explanation", out _).Should().BeTrue();
        root.TryGetProperty("rule_version_used", out _).Should().BeTrue();
        root.TryGetProperty("confidence_score", out _).Should().BeTrue();

        if (matches.ValueKind == JsonValueKind.Array && matches.GetArrayLength() > 0)
        {
            var first = matches[0];
            first.TryGetProperty("program_id", out _).Should().BeTrue();
            first.TryGetProperty("program_name", out _).Should().BeTrue();
            first.TryGetProperty("confidence_score", out _).Should().BeTrue();
            first.TryGetProperty("explanation", out _).Should().BeTrue();
        }
    }

    [Fact]
    public async Task PostEvaluateEligibility_InvalidInput_ReturnsBadRequest()
    {
        var payload = CreateEvaluatePayload(stateCode: "IL");
        payload["household_size"] = 0;

        var response = await _httpClient!.PostAsJsonAsync("/api/rules/evaluate", payload);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task PostEvaluateEligibility_StateNotFound_ReturnsNotFound()
    {
        var payload = CreateEvaluatePayload(stateCode: "ZZ");

        var response = await _httpClient!.PostAsJsonAsync("/api/rules/evaluate", payload);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task PostEvaluateEligibility_MissingRequiredFields_ReturnsBadRequest()
    {
        var payload = new Dictionary<string, object?>
        {
            ["state_code"] = "IL",
            ["household_size"] = 2
        };

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
