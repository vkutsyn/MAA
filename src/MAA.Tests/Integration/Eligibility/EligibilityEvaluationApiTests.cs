using FluentAssertions;
using MAA.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;
using EligibilityDomain = MAA.Domain.Eligibility;

namespace MAA.Tests.Integration.Eligibility;

[Collection("Database collection")]
[Trait("Category", "Integration")]
public class EligibilityEvaluationApiTests : IAsyncLifetime
{
    private readonly DatabaseFixture _databaseFixture;
    private TestWebApplicationFactory? _factory;
    private HttpClient? _httpClient;

    public EligibilityEvaluationApiTests(DatabaseFixture databaseFixture)
    {
        _databaseFixture = databaseFixture;
    }

    public async Task InitializeAsync()
    {
        _factory = TestWebApplicationFactory.CreateWithDatabase(_databaseFixture);
        _httpClient = _factory.CreateClient();

        await _databaseFixture.ClearAllDataAsync();
        await SeedEligibilityRulesAsync();
    }

    public async Task DisposeAsync()
    {
        _httpClient?.Dispose();
        if (_factory != null)
            await _factory.DisposeAsync();
    }

    [Fact]
    public async Task EvaluateEligibility_ReturnsOk()
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
        var status = document.RootElement.GetProperty("status").GetString();
        status.Should().Be("Likely");
    }

    [Fact]
    public async Task EvaluateEligibility_RulesMissing_ReturnsNotFound()
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

    private async Task SeedEligibilityRulesAsync()
    {
        await using var context = _databaseFixture.CreateContext();

        await context.EligibilityRulesV2.ExecuteDeleteAsync();
        await context.ProgramDefinitions.ExecuteDeleteAsync();
        await context.EligibilityRuleSetVersions.ExecuteDeleteAsync();

        var now = DateTime.UtcNow;
        var ruleSet = new EligibilityDomain.RuleSetVersion
        {
            RuleSetVersionId = Guid.NewGuid(),
            StateCode = "IL",
            Version = "v1",
            EffectiveDate = now.Date.AddDays(-1),
            EndDate = null,
            Status = EligibilityDomain.RuleSetStatus.Active,
            CreatedAt = now
        };

        var program = new EligibilityDomain.ProgramDefinition
        {
            ProgramCode = "IL_BASIC",
            StateCode = "IL",
            ProgramName = "IL Basic Program",
            Description = "Integration test program",
            Category = EligibilityDomain.ProgramCategory.Magi,
            IsActive = true
        };

        var rule = new EligibilityDomain.EligibilityRule
        {
            EligibilityRuleId = Guid.NewGuid(),
            RuleSetVersionId = ruleSet.RuleSetVersionId,
            RuleSetVersion = ruleSet,
            ProgramCode = program.ProgramCode,
            Program = program,
            RuleLogic = "{ \"==\": [ { \"var\": \"isCitizen\" }, true ] }",
            Priority = 0,
            CreatedAt = now
        };

        context.EligibilityRuleSetVersions.Add(ruleSet);
        context.ProgramDefinitions.Add(program);
        context.EligibilityRulesV2.Add(rule);
        await context.SaveChangesAsync();
    }
}
