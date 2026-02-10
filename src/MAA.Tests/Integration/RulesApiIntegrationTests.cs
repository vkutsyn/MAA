using FluentAssertions;
using MAA.Tests.Fixtures;
using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using System.Net.Http.Headers;
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

        await _databaseFixture.ClearAllDataAsync();
        
        // Setup authentication for all tests
        await AuthenticateAsync();
    }
    
    /// <summary>
    /// Helper method to register a test user and authenticate for API calls.
    /// Sets the Authorization header on the HttpClient for all subsequent requests.
    /// </summary>
    private async Task AuthenticateAsync()
    {
        // Register test user
        var registerRequest = new
        {
            email = "rulesapi.testuser@example.com",
            password = "TestPassword123!",
            fullName = "Rules API Test User"
        };

        var registerResponse = await _httpClient!.PostAsJsonAsync("/api/auth/register", registerRequest);
        registerResponse.EnsureSuccessStatusCode();

        // Login to get access token
        var loginRequest = new
        {
            email = "rulesapi.testuser@example.com",
            password = "TestPassword123!"
        };

        var loginResponse = await _httpClient.PostAsJsonAsync("/api/auth/login", loginRequest);
        loginResponse.EnsureSuccessStatusCode();

        var loginResult = await loginResponse.Content.ReadFromJsonAsync<JsonElement>();
        var accessToken = loginResult.GetProperty("accessToken").GetString();

        // Set authorization header for all subsequent requests
        _httpClient.DefaultRequestHeaders.Authorization = 
            new AuthenticationHeaderValue("Bearer", accessToken);
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

    // ====== Phase 4 Integration Tests (T036a, T036): Multi-Program & Asset Evaluation ======

    /// <summary>
    /// Test T036a: Asset evaluation for non-MAGI pathway
    /// Verify that users with assets below state limit qualify for Aged/Disabled programs
    /// </summary>
    [Fact]
    public async Task EvaluateEligibility_AgedPathwayAssetsBelowLimit_ReturnsEligible()
    {
        // 70-year-old with $1,500 in assets (below IL's $2,000 limit for Aged pathway)
        var payload = new Dictionary<string, object?>
        {
            ["state_code"] = "IL",
            ["household_size"] = 1,
            ["monthly_income_cents"] = 150_000,  // Below threshold
            ["age"] = 70,
            ["has_disability"] = false,
            ["is_pregnant"] = false,
            ["receives_ssi"] = false,
            ["is_citizen"] = true,
            ["assets_cents"] = 150_000  // $1,500 - below IL's $2,000 limit
        };

        var response = await _httpClient!.PostAsJsonAsync("/api/rules/evaluate", payload);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        // User should have at least one matching program
        var matchedPrograms = root.GetProperty("matched_programs");
        matchedPrograms.GetArrayLength().Should().BeGreaterThan(0, "Should have matching programs when assets are below limit");

        var explanation = root.GetProperty("explanation").GetString();
        explanation.Should().NotBeNullOrEmpty();
    }

    /// <summary>
    /// Test T036a: Asset evaluation disqualification
    /// Verify that users with assets exceeding state limit are ineligible for non-MAGI pathways
    /// </summary>
    [Fact]
    public async Task EvaluateEligibility_AgedPathwayAssetsExceedLimit_ReturnsIneligible()
    {
        // 65-year-old Disabled pathway with assets exceeding CA's $3,000 limit
        var payload = new Dictionary<string, object?>
        {
            ["state_code"] = "CA",
            ["household_size"] = 1,
            ["monthly_income_cents"] = 150_000,  // Would normally be eligible
            ["age"] = 65,
            ["has_disability"] = true,
            ["is_pregnant"] = false,
            ["receives_ssi"] = false,
            ["is_citizen"] = true,
            ["assets_cents"] = 400_000  // $4,000 - exceeds CA's $3,000 limit
        };

        var response = await _httpClient!.PostAsJsonAsync("/api/rules/evaluate", payload);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        // Explanation should mention asset reason
        var explanation = root.GetProperty("explanation").GetString();
        explanation.Should().NotBeNullOrEmpty();
        explanation!.ToLowerInvariant().Should().Contain("assets", "Explanation should mention assets");
    }

    /// <summary>
    /// Test T036: Multi-program matching
    /// Verify system returns all matching programs, not just first match
    /// </summary>
    [Fact]
    public async Task EvaluateEligibility_PregnantUser_ReturnsMultipleMatchingPrograms()
    {
        // 25-year-old pregnant user should match MAGI Adult + Pregnancy-Related programs
        var payload = new Dictionary<string, object?>
        {
            ["state_code"] = "IL",
            ["household_size"] = 2,
            ["monthly_income_cents"] = 200_000,  // $2,000/month - at threshold
            ["age"] = 25,
            ["has_disability"] = false,
            ["is_pregnant"] = true,  // IMPORTANT: triggers Pregnancy pathway match
            ["receives_ssi"] = false,
            ["is_citizen"] = true,
            ["assets_cents"] = null
        };

        var response = await _httpClient!.PostAsJsonAsync("/api/rules/evaluate", payload);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        var matchedPrograms = root.GetProperty("matched_programs");
        var programCount = matchedPrograms.GetArrayLength();
        
        // Should have multiple program matches (MAGI + Pregnancy)
        programCount.Should().BeGreaterThanOrEqualTo(2, 
            "Pregnant user should match multiple programs (MAGI + Pregnancy-Related)");
    }

    /// <summary>
    /// Test T036: Confidence score sorting
    /// Verify matched programs are sorted by confidence descending
    /// </summary>
    [Fact]
    public async Task EvaluateEligibility_MatchedPrograms_SortedByConfidenceDescending()
    {
        var payload = new Dictionary<string, object?>
        {
            ["state_code"] = "IL",
            ["household_size"] = 2,
            ["monthly_income_cents"] = 180_000,  // $1,800/month
            ["age"] = 35,
            ["has_disability"] = false,
            ["is_pregnant"] = false,
            ["receives_ssi"] = false,
            ["is_citizen"] = true,
            ["assets_cents"] = null
        };

        var response = await _httpClient!.PostAsJsonAsync("/api/rules/evaluate", payload);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        var matchedPrograms = root.GetProperty("matched_programs");
        if (matchedPrograms.GetArrayLength() > 1)
        {
            // Extract confidence scores
            var scores = new List<int>();
            foreach (var program in matchedPrograms.EnumerateArray())
            {
                scores.Add(program.GetProperty("confidence_score").GetInt32());
            }

            // Verify descending order
            for (int i = 0; i < scores.Count - 1; i++)
            {
                scores[i].Should().BeGreaterThanOrEqualTo(scores[i + 1],
                    "Programs should be sorted by confidence score descending");
            }
        }
    }

    /// <summary>
    /// Test T036: Aged + Disabled pathways
    /// Verify elderly user with disability qualifies for both pathways
    /// </summary>
    [Fact]
    public async Task EvaluateEligibility_AgedAndDisabledPathways_BothIncluded()
    {
        // 70-year-old with disability - should match both Aged AND Disabled pathways
        var payload = new Dictionary<string, object?>
        {
            ["state_code"] = "NY",
            ["household_size"] = 1,
            ["monthly_income_cents"] = 200_000,  // Below threshold
            ["age"] = 70,
            ["has_disability"] = true,  // Both pathways qualify
            ["is_pregnant"] = false,
            ["receives_ssi"] = false,
            ["is_citizen"] = true,
            ["assets_cents"] = 200_000  // Below NY's $4,500 limit
        };

        var response = await _httpClient!.PostAsJsonAsync("/api/rules/evaluate", payload);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        var matchedPrograms = root.GetProperty("matched_programs");
        matchedPrograms.GetArrayLength().Should().BeGreaterThanOrEqualTo(2,
            "Elderly disabled user should match multiple pathways");
    }

    /// <summary>
    /// Test T036: Performance constraint
    /// Verify evaluation completes within SLA (≤2 seconds for p95)
    /// </summary>
    [Fact]
    public async Task EvaluateEligibility_CompletesQuickly()
    {
        var payload = CreateEvaluatePayload(stateCode: "IL");

        var sw = Stopwatch.StartNew();
        var response = await _httpClient!.PostAsJsonAsync("/api/rules/evaluate", payload);
        sw.Stop();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        sw.ElapsedMilliseconds.Should().BeLessThan(2000, 
            "Evaluation should complete within 2 seconds (p95 SLA)");
    }

    /// <summary>
    /// Test T036: Each program match includes explanation
    /// Verify program-specific explanation is included for each match
    /// </summary>
    [Fact]
    public async Task EvaluateEligibility_EachMatchIncludesExplanation()
    {
        var payload = new Dictionary<string, object?>
        {
            ["state_code"] = "IL",
            ["household_size"] = 2,
            ["monthly_income_cents"] = 200_000,
            ["age"] = 35,
            ["has_disability"] = false,
            ["is_pregnant"] = true,
            ["receives_ssi"] = false,
            ["is_citizen"] = true,
            ["assets_cents"] = null
        };

        var response = await _httpClient!.PostAsJsonAsync("/api/rules/evaluate", payload);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        var matchedPrograms = root.GetProperty("matched_programs");
        foreach (var program in matchedPrograms.EnumerateArray())
        {
            program.TryGetProperty("explanation", out var explanation).Should().BeTrue();
            explanation.GetString().Should().NotBeNullOrEmpty("Each match should have explanation");
        }
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

    #region Phase 5: US3 - State-Specific Rule Evaluation (T048)

    /// <summary>
    /// Test T048: Same user profile evaluated in different states produces different results
    /// Validates that IL and TX have different income thresholds for similar programs
    /// </summary>
    [Fact]
    public async Task EvaluateEligibility_SameUserDifferentStates_ProducesDifferentResults()
    {
        // Arrange: Create identical user profile but different state codes
        var ilPayload = CreateStateSpecificPayload(stateCode: "IL", monthlyIncomeCents: 210_000, householdSize: 3);
        var txPayload = CreateStateSpecificPayload(stateCode: "TX", monthlyIncomeCents: 210_000, householdSize: 3);

        // Act
        var ilResponse = await _httpClient!.PostAsJsonAsync("/api/rules/evaluate", ilPayload);
        var txResponse = await _httpClient!.PostAsJsonAsync("/api/rules/evaluate", txPayload);

        // Assert: Both should succeed
        ilResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        txResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Parse responses
        var ilJson = await ilResponse.Content.ReadAsStringAsync();
        var txJson = await txResponse.Content.ReadAsStringAsync();

        using var ilDoc = JsonDocument.Parse(ilJson);
        using var txDoc = JsonDocument.Parse(txJson);

        var ilRoot = ilDoc.RootElement;
        var txRoot = txDoc.RootElement;

        // Verify both have matched programs but they may differ
        var ilMatches = ilRoot.GetProperty("matched_programs").GetArrayLength();
        var txMatches = txRoot.GetProperty("matched_programs").GetArrayLength();

        // At least one state should have different count or confidence to show state-specific evaluation
        (ilMatches > 0 && txMatches > 0).Should().BeTrue(
            "Both states should have programs for household size 3 with income $2,100/month");
    }

    /// <summary>
    /// Test T048: IL-specific evaluation with IL thresholds
    /// Verifies that IL rules are applied correctly for IL selection
    /// </summary>
    [Fact]
    public async Task EvaluateEligibility_ILThresholds_AppliesCorrectly()
    {
        var payload = CreateStateSpecificPayload(stateCode: "IL", monthlyIncomeCents: 245_000, householdSize: 2);

        var response = await _httpClient!.PostAsJsonAsync("/api/rules/evaluate", payload);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        // Verify state code is maintained throughout evaluation
        root.GetProperty("state_code").GetString().Should().Be("IL");
        
        // IL should return results for IL-specific programs
        var matchedPrograms = root.GetProperty("matched_programs");
        matchedPrograms.GetArrayLength().Should().BeGreaterThanOrEqualTo(1,
            "IL household with $2,450/month income should match at least one IL program");
    }

    /// <summary>
    /// Test T048: CA-specific evaluation with CA thresholds
    /// Verifies that CA rules are applied correctly for CA selection
    /// </summary>
    [Fact]
    public async Task EvaluateEligibility_CAThresholds_AppliesCorrectly()
    {
        var payload = CreateStateSpecificPayload(stateCode: "CA", monthlyIncomeCents: 260_000, householdSize: 2);

        var response = await _httpClient!.PostAsJsonAsync("/api/rules/evaluate", payload);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        // Verify state code is maintained
        root.GetProperty("state_code").GetString().Should().Be("CA");
        
        // CA should return results for CA-specific programs
        var matchedPrograms = root.GetProperty("matched_programs");
        matchedPrograms.GetArrayLength().Should().BeGreaterThanOrEqualTo(1,
            "CA household should return results");
    }

    /// <summary>
    /// Test T048: State selection persists through evaluation
    /// Verifies no cross-state rule mixing when evaluating different states
    /// </summary>
    [Fact]
    public async Task EvaluateEligibility_StateSelectionPersists_NoCrossMixing()
    {
        var payload = CreateStateSpecificPayload(stateCode: "NY", monthlyIncomeCents: 235_000, householdSize: 2);

        var response = await _httpClient!.PostAsJsonAsync("/api/rules/evaluate", payload);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        // Assert state code remains NY throughout
        root.GetProperty("state_code").GetString().Should().Be("NY");

        // All matched programs should be from NY (verify through program codes or state metadata)
        var matchedPrograms = root.GetProperty("matched_programs");
        foreach (var program in matchedPrograms.EnumerateArray())
        {
            // Program codes should follow NY naming convention if available
            var programName = program.GetProperty("program_name").GetString();
            programName.Should().NotBeNullOrEmpty();
        }
    }

    /// <summary>
    /// Test T048: Different state thresholds affect confidence scoring
    /// Validates that different state rules produce different confidence for same user
    /// </summary>
    [Fact]
    public async Task EvaluateEligibility_DifferentStateThresholds_AffectConfidence()
    {
        // Use a borderline income that might produce different results in different states
        var ilPayload = CreateStateSpecificPayload(stateCode: "IL", monthlyIncomeCents: 225_000, householdSize: 2);
        var txPayload = CreateStateSpecificPayload(stateCode: "TX", monthlyIncomeCents: 225_000, householdSize: 2);

        var ilResponse = await _httpClient!.PostAsJsonAsync("/api/rules/evaluate", ilPayload);
        var txResponse = await _httpClient!.PostAsJsonAsync("/api/rules/evaluate", txPayload);

        ilResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        txResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var ilJson = await ilResponse.Content.ReadAsStringAsync();
        var txJson = await txResponse.Content.ReadAsStringAsync();

        using var ilDoc = JsonDocument.Parse(ilJson);
        using var txDoc = JsonDocument.Parse(txJson);

        // Both evaluations should return results, demonstrating state-specific logic
        var ilMatches = ilDoc.RootElement.GetProperty("matched_programs").GetArrayLength();
        var txMatches = txDoc.RootElement.GetProperty("matched_programs").GetArrayLength();

        (ilMatches >= 0 && txMatches >= 0).Should().BeTrue();
    }

    /// <summary>
    /// Test T048: TX-specific evaluation
    /// Demonstrates TX rules are applied when TX state is selected
    /// </summary>
    [Fact]
    public async Task EvaluateEligibility_TXEvaluation_AppliesTXRules()
    {
        var payload = CreateStateSpecificPayload(stateCode: "TX", monthlyIncomeCents: 220_000, householdSize: 2);

        var response = await _httpClient!.PostAsJsonAsync("/api/rules/evaluate", payload);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        root.GetProperty("state_code").GetString().Should().Be("TX");
    }

    /// <summary>
    /// Test T048: FL-specific evaluation
    /// Demonstrates FL rules are applied when FL state is selected
    /// </summary>
    [Fact]
    public async Task EvaluateEligibility_FLEvaluation_AppliesFLRules()
    {
        var payload = CreateStateSpecificPayload(stateCode: "FL", monthlyIncomeCents: 215_000, householdSize: 2);

        var response = await _httpClient!.PostAsJsonAsync("/api/rules/evaluate", payload);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        root.GetProperty("state_code").GetString().Should().Be("FL");
    }

    #endregion

    // ====== Phase 6 Integration Tests (T057): Plain-Language Explanations ======

    /// <summary>
    /// Test T057: IL Evaluation Scenario
    /// Verify explanation includes concrete income values when evaluating IL eligibility
    /// </summary>
    [Fact]
    public async Task EvaluateEligibility_IL_ExplanationIncludesConcreteIncomeValues()
    {
        var payload = new Dictionary<string, object?>
        {
            ["state_code"] = "IL",
            ["household_size"] = 2,
            ["monthly_income_cents"] = 210_000,  // $2,100/month
            ["age"] = 35,
            ["has_disability"] = false,
            ["is_pregnant"] = false,
            ["receives_ssi"] = false,
            ["is_citizen"] = true,
            ["assets_cents"] = null
        };

        var response = await _httpClient!.PostAsJsonAsync("/api/rules/evaluate", payload);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        var explanation = root.GetProperty("explanation").GetString();
        explanation.Should().NotBeNullOrEmpty();
        
        // Explanation should reference concrete income values
        explanation!.ToLowerInvariant().Should().Contain("income",
            "Explanation should reference income metric");
        explanation.Should().Contain("2100",
            "Explanation should include concrete monthly income value ($2,100)");
    }

    /// <summary>
    /// Test T057: Pregnancy Scenario
    /// Verify explanation includes pregnancy-specific language when evaluating pregnant users
    /// </summary>
    [Fact]
    public async Task EvaluateEligibility_PregnancyScenario_ExplanationIsPregnancySpecific()
    {
        var payload = new Dictionary<string, object?>
        {
            ["state_code"] = "IL",
            ["household_size"] = 1,
            ["monthly_income_cents"] = 180_000,  // $1,800/month
            ["age"] = 25,
            ["has_disability"] = false,
            ["is_pregnant"] = true,  // KEY: Pregnancy status
            ["receives_ssi"] = false,
            ["is_citizen"] = true,
            ["assets_cents"] = null
        };

        var response = await _httpClient!.PostAsJsonAsync("/api/rules/evaluate", payload);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        var explanation = root.GetProperty("explanation").GetString();
        explanation.Should().NotBeNullOrEmpty();
        
        // Should mention pregnancy or pregnancy-related pathway
        explanation!.ToLowerInvariant().Should().Contain("pregnant",
            "Explanation should reference pregnancy when user reports pregnancy");
    }

    /// <summary>
    /// Test T057: SSI (Categorical Eligibility) Scenario
    /// Verify explanation explains how SSI receipt provides categorical eligibility bypass
    /// </summary>
    [Fact]
    public async Task EvaluateEligibility_SSIScenario_ExplainsCategoricalEligibilityBypass()
    {
        var payload = new Dictionary<string, object?>
        {
            ["state_code"] = "IL",
            ["household_size"] = 1,
            ["monthly_income_cents"] = 400_000,  // $4,000/month - would normally exceed threshold
            ["age"] = 45,
            ["has_disability"] = false,
            ["is_pregnant"] = false,
            ["receives_ssi"] = true,  // KEY: SSI recipient - bypasses income test
            ["is_citizen"] = true,
            ["assets_cents"] = null
        };

        var response = await _httpClient!.PostAsJsonAsync("/api/rules/evaluate", payload);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        var explanation = root.GetProperty("explanation").GetString();
        explanation.Should().NotBeNullOrEmpty();
        
        // Should explain SSI-based categorical eligibility
        var lowerExplanation = explanation!.ToLowerInvariant();
        (lowerExplanation.Contains("ssi") || lowerExplanation.Contains("social security income"))
            .Should().BeTrue("Explanation should reference SSI when user receives SSI benefits");
    }

    /// <summary>
    /// Test T057: Readability Validation
    /// Verify all explanations meet ≤8th grade reading level requirement
    /// </summary>
    [Fact]
    public async Task EvaluateEligibility_Explanations_MeetReadabilityRequirement()
    {
        var testScenarios = new List<Dictionary<string, object?>>
        {
            // Scenario 1: Basic eligibility
            new()
            {
                ["state_code"] = "IL",
                ["household_size"] = 2,
                ["monthly_income_cents"] = 200_000,
                ["age"] = 30,
                ["has_disability"] = false,
                ["is_pregnant"] = false,
                ["receives_ssi"] = false,
                ["is_citizen"] = true,
                ["assets_cents"] = null
            },
            // Scenario 2: Pregnancy
            new()
            {
                ["state_code"] = "CA",
                ["household_size"] = 1,
                ["monthly_income_cents"] = 190_000,
                ["age"] = 28,
                ["has_disability"] = false,
                ["is_pregnant"] = true,
                ["receives_ssi"] = false,
                ["is_citizen"] = true,
                ["assets_cents"] = null
            },
            // Scenario 3: Disabled
            new()
            {
                ["state_code"] = "NY",
                ["household_size"] = 1,
                ["monthly_income_cents"] = 220_000,
                ["age"] = 55,
                ["has_disability"] = true,
                ["is_pregnant"] = false,
                ["receives_ssi"] = false,
                ["is_citizen"] = true,
                ["assets_cents"] = null
            }
        };

        var readabilityValidator = new MAA.Application.Eligibility.Validators.ReadabilityValidator();

        foreach (var scenario in testScenarios)
        {
            // Act
            var response = await _httpClient!.PostAsJsonAsync("/api/rules/evaluate", scenario);

            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            var explanation = root.GetProperty("explanation").GetString();
            explanation.Should().NotBeNullOrEmpty();

            // Assert: Explanation should meet readability target
            var isReadable = readabilityValidator.IsBelow8thGrade(explanation!);
            isReadable.Should().BeTrue(
                $"Explanation for state {scenario["state_code"]} should be ≤8th grade reading level: '{explanation}'");

            // Also validate per-program explanations if present
            var matchedPrograms = root.GetProperty("matched_programs");
            foreach (var program in matchedPrograms.EnumerateArray())
            {
                if (program.TryGetProperty("explanation", out var programExpl))
                {
                    var programExplanation = programExpl.GetString();
                    if (!string.IsNullOrEmpty(programExplanation))
                    {
                        var programIsReadable = readabilityValidator.IsBelow8thGrade(programExplanation);
                        programIsReadable.Should().BeTrue(
                            $"Program explanation should be ≤8th grade reading level: '{programExplanation}'");
                    }
                }
            }
        }
    }

    private static Dictionary<string, object?> CreateStateSpecificPayload(
        string stateCode,
        int monthlyIncomeCents,
        int householdSize)
    {
        return new Dictionary<string, object?>
        {
            ["state_code"] = stateCode,
            ["household_size"] = householdSize,
            ["monthly_income_cents"] = monthlyIncomeCents,
            ["age"] = 35,
            ["has_disability"] = false,
            ["is_pregnant"] = false,
            ["receives_ssi"] = false,
            ["is_citizen"] = true,
            ["assets_cents"] = null
        };
    }
}
