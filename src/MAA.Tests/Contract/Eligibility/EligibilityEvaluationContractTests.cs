using FluentAssertions;
using MAA.Tests.Integration;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace MAA.Tests.Contract.Eligibility;

/// <summary>
/// Contract tests validating Eligibility v2 API endpoints match OpenAPI schema.
/// </summary>
public class EligibilityEvaluationContractTests : IAsyncLifetime
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
    public async Task PostEvaluateEligibility_ReturnsOkWithSchema()
    {
        var payload = new
        {
            stateCode = "IL",
            effectiveDate = DateTime.UtcNow.Date.ToString("yyyy-MM-dd"),
            answers = new { isCitizen = true }
        };

        var response = await _httpClient!.PostAsJsonAsync("/api/eligibility/evaluate", payload);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var json = await response.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;

        root.TryGetProperty("status", out _).Should().BeTrue();
        root.TryGetProperty("matchedPrograms", out var matches).Should().BeTrue();
        root.TryGetProperty("confidenceScore", out _).Should().BeTrue();
        root.TryGetProperty("explanation", out _).Should().BeTrue();
        root.TryGetProperty("ruleVersionUsed", out _).Should().BeTrue();
        root.TryGetProperty("evaluatedAt", out _).Should().BeTrue();

        matches.ValueKind.Should().Be(JsonValueKind.Array);
        if (matches.GetArrayLength() > 0)
        {
            var first = matches[0];
            first.TryGetProperty("programCode", out _).Should().BeTrue();
            first.TryGetProperty("programName", out _).Should().BeTrue();
            first.TryGetProperty("confidenceScore", out _).Should().BeTrue();
            first.TryGetProperty("explanation", out _).Should().BeTrue();
        }
    }

    [Fact]
    public async Task PostEvaluateEligibility_InvalidRequest_ReturnsBadRequest()
    {
        var payload = new
        {
            effectiveDate = DateTime.UtcNow.Date.ToString("yyyy-MM-dd"),
            answers = new { isCitizen = true }
        };

        var response = await _httpClient!.PostAsJsonAsync("/api/eligibility/evaluate", payload);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task PostEvaluateEligibility_RulesMissing_ReturnsNotFound()
    {
        var payload = new
        {
            stateCode = "ZZ",
            effectiveDate = DateTime.UtcNow.Date.ToString("yyyy-MM-dd"),
            answers = new { isCitizen = true }
        };

        var response = await _httpClient!.PostAsJsonAsync("/api/eligibility/evaluate", payload);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
