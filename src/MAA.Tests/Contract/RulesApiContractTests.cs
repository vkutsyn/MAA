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

    // ====== Phase 4 Contract Tests (T037, T038): Multi-Program & Asset Validation ======

    /// <summary>
    /// Test T037: Asset evaluation field validation in contract
    /// Verify disqualifying_factors array includes asset-related reasons where applicable
    /// </summary>
    [Fact]
    public async Task PostEvaluateEligibility_WithAssets_IncludesDisqualifyingFactorsInSchema()
    {
        var payload = new Dictionary<string, object?>
        {
            ["state_code"] = "IL",
            ["household_size"] = 1,
            ["monthly_income_cents"] = 100_000,
            ["age"] = 70,
            ["has_disability"] = false,
            ["is_pregnant"] = false,
            ["receives_ssi"] = false,
            ["is_citizen"] = true,
            ["assets_cents"] = 250_000  // High assets - may become disqualifying factor
        };

        var response = await _httpClient!.PostAsJsonAsync("/api/rules/evaluate", payload);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var json = await response.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;

        // Verify matched_programs have disqualifying_factors array
        var matchedPrograms = root.GetProperty("matched_programs");
        foreach (var program in matchedPrograms.EnumerateArray())
        {
            program.TryGetProperty("disqualifying_factors", out var disqualifyingFactors)
                .Should().BeTrue("Each program match should have disqualifying_factors field");
            
            disqualifyingFactors.ValueKind.Should().Be(JsonValueKind.Array);
        }
    }

    /// <summary>
    /// Test T038: Multi-program response schema validation
    /// Verify matched_programs array structure and required fields
    /// </summary>
    [Fact]
    public async Task PostEvaluateEligibility_ResponseSchema_IncludesAllRequiredFieldsInProgramMatches()
    {
        var payload = CreateEvaluatePayload(stateCode: "IL");

        var response = await _httpClient!.PostAsJsonAsync("/api/rules/evaluate", payload);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var json = await response.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;

        // Verify matched_programs array exists
        root.TryGetProperty("matched_programs", out var matchedPrograms).Should().BeTrue();
        matchedPrograms.ValueKind.Should().Be(JsonValueKind.Array);

        if (matchedPrograms.GetArrayLength() > 0)
        {
            foreach (var program in matchedPrograms.EnumerateArray())
            {
                // Verify all required fields
                program.TryGetProperty("program_id", out _).Should().BeTrue("program_id required");
                program.TryGetProperty("program_name", out _).Should().BeTrue("program_name required");
                program.TryGetProperty("confidence_score", out var confidenceScore).Should().BeTrue("confidence_score required");
                program.TryGetProperty("explanation", out _).Should().BeTrue("explanation required");
                program.TryGetProperty("eligibility_pathway", out _).Should().BeTrue("eligibility_pathway required");
                program.TryGetProperty("matching_factors", out var matchingFactors).Should().BeTrue("matching_factors required");
                program.TryGetProperty("disqualifying_factors", out var disqualifyingFactors).Should().BeTrue("disqualifying_factors required");

                // Verify type constraints
                confidenceScore.ValueKind.Should().Be(JsonValueKind.Number, "confidence_score must be integer");
                var scoreValue = confidenceScore.GetInt32();
                scoreValue.Should().BeGreaterThanOrEqualTo(0).And.BeLessThanOrEqualTo(100);
                
                matchingFactors.ValueKind.Should().Be(JsonValueKind.Array);
                disqualifyingFactors.ValueKind.Should().Be(JsonValueKind.Array);
            }
        }
    }

    /// <summary>
    /// Test T038: Results sorted by confidence descending
    /// Verify matched_programs array is sorted by confidence_score descending
    /// </summary>
    [Fact]
    public async Task PostEvaluateEligibility_MatchedPrograms_SortedByConfidenceDescending()
    {
        var payload = new Dictionary<string, object?>
        {
            ["state_code"] = "IL",
            ["household_size"] = 2,
            ["monthly_income_cents"] = 200_000,
            ["age"] = 30,
            ["has_disability"] = false,
            ["is_pregnant"] = true,
            ["receives_ssi"] = false,
            ["is_citizen"] = true,
            ["assets_cents"] = null
        };

        var response = await _httpClient!.PostAsJsonAsync("/api/rules/evaluate", payload);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var json = await response.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;

        var matchedPrograms = root.GetProperty("matched_programs");
        if (matchedPrograms.GetArrayLength() > 1)
        {
            var scores = new List<int>();
            foreach (var program in matchedPrograms.EnumerateArray())
            {
                scores.Add(program.GetProperty("confidence_score").GetInt32());
            }

            // Verify descending order
            for (int i = 0; i < scores.Count - 1; i++)
            {
                scores[i].Should().BeGreaterThanOrEqualTo(scores[i + 1],
                    "matched_programs must be sorted by confidence_score descending");
            }
        }
    }

    /// <summary>
    /// Test T038: Confidence score is 0-100 integer
    /// Verify all confidence scores are valid integers in range [0, 100]
    /// </summary>
    [Fact]
    public async Task PostEvaluateEligibility_ConfidenceScores_AreValidIntegers()
    {
        var payload = CreateEvaluatePayload(stateCode: "IL");

        var response = await _httpClient!.PostAsJsonAsync("/api/rules/evaluate", payload);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var json = await response.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;

        // Check overall confidence_score
        var overallScore = root.GetProperty("confidence_score").GetInt32();
        overallScore.Should().BeGreaterThanOrEqualTo(0).And.BeLessThanOrEqualTo(100);

        // Check individual program scores
        var matchedPrograms = root.GetProperty("matched_programs");
        foreach (var program in matchedPrograms.EnumerateArray())
        {
            var score = program.GetProperty("confidence_score").GetInt32();
            score.Should().BeGreaterThanOrEqualTo(0).And.BeLessThanOrEqualTo(100);
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

    // ====== Phase 5 Contract Tests (T050): State-Specific Rules ======

    /// <summary>
    /// Test T050: state_code parameter validation
    /// Verify API validates state_code against supported pilot states: IL, CA, NY, TX, FL
    /// </summary>
    [Fact]
    public async Task PostEvaluateEligibility_UnsupportedStateCode_ReturnsBadRequestOrNotFound()
    {
        var payload = CreateEvaluatePayload(stateCode: "XX");

        var response = await _httpClient!.PostAsJsonAsync("/api/rules/evaluate", payload);

        // Should be either 400 (bad request) or 404 (not found) for unsupported state
        (response.StatusCode == HttpStatusCode.BadRequest || response.StatusCode == HttpStatusCode.NotFound)
            .Should().BeTrue("Unsupported state code should return 400 or 404");
    }

    /// <summary>
    /// Test T050: state_code is required in request
    /// Verify API requires state_code parameter (cannot be null or empty)
    /// </summary>
    [Fact]
    public async Task PostEvaluateEligibility_MissingStateCode_ReturnsBadRequest()
    {
        var payload = new Dictionary<string, object?>
        {
            ["household_size"] = 2,
            ["monthly_income_cents"] = 210_000,
            ["age"] = 35,
            ["has_disability"] = false,
            ["is_pregnant"] = false,
            ["receives_ssi"] = false,
            ["is_citizen"] = true,
            ["assets_cents"] = null
        };

        var response = await _httpClient!.PostAsJsonAsync("/api/rules/evaluate", payload);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    /// <summary>
    /// Test T050: IL state code returns IL results
    /// Verify response state_code matches request for IL
    /// </summary>
    [Fact]
    public async Task PostEvaluateEligibility_IL_ResponseContainsILStateCode()
    {
        var payload = CreateEvaluatePayload(stateCode: "IL");

        var response = await _httpClient!.PostAsJsonAsync("/api/rules/evaluate", payload);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var json = await response.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;

        root.GetProperty("state_code").GetString().Should().Be("IL");
    }

    /// <summary>
    /// Test T050: CA state code returns CA results
    /// Verify response state_code matches request for CA
    /// </summary>
    [Fact]
    public async Task PostEvaluateEligibility_CA_ResponseContainsCAStateCode()
    {
        var payload = CreateEvaluatePayload(stateCode: "CA");

        var response = await _httpClient!.PostAsJsonAsync("/api/rules/evaluate", payload);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var json = await response.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;

        root.GetProperty("state_code").GetString().Should().Be("CA");
    }

    /// <summary>
    /// Test T050: NY state code returns NY results
    /// Verify response state_code matches request for NY
    /// </summary>
    [Fact]
    public async Task PostEvaluateEligibility_NY_ResponseContainsNYStateCode()
    {
        var payload = CreateEvaluatePayload(stateCode: "NY");

        var response = await _httpClient!.PostAsJsonAsync("/api/rules/evaluate", payload);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var json = await response.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;

        root.GetProperty("state_code").GetString().Should().Be("NY");
    }

    /// <summary>
    /// Test T050: TX state code returns TX results
    /// Verify response state_code matches request for TX
    /// </summary>
    [Fact]
    public async Task PostEvaluateEligibility_TX_ResponseContainsTXStateCode()
    {
        var payload = CreateEvaluatePayload(stateCode: "TX");

        var response = await _httpClient!.PostAsJsonAsync("/api/rules/evaluate", payload);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var json = await response.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;

        root.GetProperty("state_code").GetString().Should().Be("TX");
    }

    /// <summary>
    /// Test T050: FL state code returns FL results
    /// Verify response state_code matches request for FL
    /// </summary>
    [Fact]
    public async Task PostEvaluateEligibility_FL_ResponseContainsFLStateCode()
    {
        var payload = CreateEvaluatePayload(stateCode: "FL");

        var response = await _httpClient!.PostAsJsonAsync("/api/rules/evaluate", payload);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var json = await response.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;

        root.GetProperty("state_code").GetString().Should().Be("FL");
    }

    /// <summary>
    /// Test T050: State codes are case-insensitive
    /// Verify API handles lowercase state codes (il, ca, ny, tx, fl)
    /// </summary>
    [Fact]
    public async Task PostEvaluateEligibility_LowercaseStateCode_IsAccepted()
    {
        var payload = CreateEvaluatePayload(stateCode: "il");

        var response = await _httpClient!.PostAsJsonAsync("/api/rules/evaluate", payload);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var json = await response.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;

        // Response should normalize to uppercase
        root.GetProperty("state_code").GetString().Should().Be("IL");
    }

    // ====== Phase 6 Contract Tests (T058): Explanation Field Validation ======

    /// <summary>
    /// Test T058: Explanation field is present in all responses
    /// Verify all evaluation responses include the explanation field
    /// </summary>
    [Fact]
    public async Task PostEvaluateEligibility_AllResponses_ContainExplanationField()
    {
        var testStates = new[] { "IL", "CA", "NY", "TX", "FL" };

        foreach (var state in testStates)
        {
            var payload = CreateEvaluatePayload(stateCode: state);

            var response = await _httpClient!.PostAsJsonAsync("/api/rules/evaluate", payload);

            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var json = await response.Content.ReadAsStringAsync();
            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;

            // Main explanation field must exist
            root.TryGetProperty("explanation", out var explanation).Should().BeTrue(
                $"Response for state {state} should contain 'explanation' field");

            // Explanation must not be null
            explanation.ValueKind.Should().NotBe(JsonValueKind.Null,
                $"Explanation field for state {state} should not be null");
        }
    }

    /// <summary>
    /// Test T058: Explanation is non-empty string
    /// Verify explanation always contains meaningful content, not empty or whitespace
    /// </summary>
    [Fact]
    public async Task PostEvaluateEligibility_Explanation_IsNonEmptyString()
    {
        var payload = CreateEvaluatePayload(stateCode: "IL");

        var response = await _httpClient!.PostAsJsonAsync("/api/rules/evaluate", payload);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var json = await response.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;

        var explanation = root.GetProperty("explanation").GetString();
        
        // Must be non-null
        explanation.Should().NotBeNull("Explanation should not be null");
        
        // Must be non-empty
        explanation.Should().NotBeEmpty("Explanation should not be empty string");
        
        // Must not be only whitespace
        explanation!.Trim().Should().NotBeEmpty("Explanation should contain meaningful content, not just whitespace");
        
        // Should have reasonable length (at least 20 characters for any useful explanation)
        explanation.Length.Should().BeGreaterThan(20,
            "Explanation should provide substantive content (>20 chars)");
    }

    /// <summary>
    /// Test T058: Each program match includes non-empty explanation
    /// Verify program-specific explanations are also non-empty
    /// </summary>
    [Fact]
    public async Task PostEvaluateEligibility_ProgramExplanations_AreNonEmpty()
    {
        var payload = CreateEvaluatePayload(stateCode: "IL");

        var response = await _httpClient!.PostAsJsonAsync("/api/rules/evaluate", payload);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var json = await response.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;

        var matchedPrograms = root.GetProperty("matched_programs");
        matchedPrograms.GetArrayLength().Should().BeGreaterThan(0, "Should have at least one matched program");

        foreach (var program in matchedPrograms.EnumerateArray())
        {
            program.TryGetProperty("explanation", out var programExpl).Should().BeTrue(
                "Each program match should have 'explanation' field");

            var programExplanation = programExpl.GetString();
            programExplanation.Should().NotBeNullOrEmpty(
                "Program explanation should not be null or empty");
            programExplanation!.Trim().Should().NotBeEmpty(
                "Program explanation should contain meaningful content");
        }
    }

    /// <summary>
    /// Test T058: Explanation does not contain unexplained jargon
    /// Verify explanations define all acronyms inline or avoid jargon entirely
    /// </summary>
    [Fact]
    public async Task PostEvaluateEligibility_Explanation_DefinesJargonTerms()
    {
        var payload = CreateEvaluatePayload(stateCode: "IL");

        var response = await _httpClient!.PostAsJsonAsync("/api/rules/evaluate", payload);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var json = await response.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;

        var explanation = root.GetProperty("explanation").GetString();
        explanation.Should().NotBeNullOrEmpty();

        // Common government acronyms that should be explained if used
        var jargonRules = new Dictionary<string, string>
        {
            { "MAGI", "Modified Adjusted Gross Income" },
            { "FPL", "Federal Poverty Level" },
            { "SSI", "Social Security Income" },
            { "SSDI", "Social Security Disability Insurance" },
            { "TANF", "Temporary Assistance for Needy Families" }
        };

        foreach (var (acronym, definition) in jargonRules)
        {
            // If acronym is present in explanation, ensure it's defined
            if (explanation!.Contains(acronym))
            {
                // Either the full definition should appear nearby, or the acronym should appear in explained form
                var isExplained = explanation.Contains(definition) ||
                                  explanation.Contains($"{definition} ({acronym})") ||
                                  explanation.Contains($"{acronym} ({definition.ToLower()})");

                isExplained.Should().BeTrue(
                    $"Acronym '{acronym}' should be defined as '{definition}' in the explanation");
            }
        }
    }

    /// <summary>
    /// Test T058: Explanation does not contain common undefined acronyms in different scenarios
    /// Verify SSI, pregnancy, and asset scenarios all provide jargon-free explanations
    /// </summary>
    [Fact]
    public async Task PostEvaluateEligibility_MultipleScenarios_AllHaveJargonFreeExplanations()
    {
        var scenarios = new List<(Dictionary<string, object?> payload, string description)>
        {
            (
                new Dictionary<string, object?>
                {
                    ["state_code"] = "IL",
                    ["household_size"] = 1,
                    ["monthly_income_cents"] = 300_000,
                    ["age"] = 50,
                    ["has_disability"] = false,
                    ["is_pregnant"] = false,
                    ["receives_ssi"] = true,
                    ["is_citizen"] = true,
                    ["assets_cents"] = null
                },
                "SSI scenario"
            ),
            (
                new Dictionary<string, object?>
                {
                    ["state_code"] = "CA",
                    ["household_size"] = 1,
                    ["monthly_income_cents"] = 180_000,
                    ["age"] = 25,
                    ["has_disability"] = false,
                    ["is_pregnant"] = true,
                    ["receives_ssi"] = false,
                    ["is_citizen"] = true,
                    ["assets_cents"] = null
                },
                "Pregnancy scenario"
            ),
            (
                new Dictionary<string, object?>
                {
                    ["state_code"] = "NY",
                    ["household_size"] = 1,
                    ["monthly_income_cents"] = 150_000,
                    ["age"] = 70,
                    ["has_disability"] = false,
                    ["is_pregnant"] = false,
                    ["receives_ssi"] = false,
                    ["is_citizen"] = true,
                    ["assets_cents"] = 300_000
                },
                "Aged pathway with assets"
            )
        };

        foreach (var (payload, description) in scenarios)
        {
            var response = await _httpClient!.PostAsJsonAsync("/api/rules/evaluate", payload);

            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var json = await response.Content.ReadAsStringAsync();
            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;

            var explanation = root.GetProperty("explanation").GetString();
            explanation.Should().NotBeNullOrEmpty($"Explanation for {description} should not be empty");

            // Verify explanation has meaningful content (not just generic error messages)
            explanation!.Length.Should().BeGreaterThan(30,
                $"Explanation for {description} should provide substantive content");

            // Ensure no undefined standalone acronyms (basic check)
            // This is a simple heuristic: lone uppercase 3-4 letter words followed by punctuation
            var suspiciousAcronyms = new[] { "AMI", "AGI", "CMS", "HHS", "DHH", "SOC" };
            foreach (var acronym in suspiciousAcronyms)
            {
                if (explanation.Contains($" {acronym} ") || explanation.Contains($" {acronym}.") || explanation.Contains($" {acronym},"))
                {
                    // If found, should have definition nearby (within same sentence)
                    var acronymIndex = explanation.IndexOf(acronym);
                    var contextBefore = acronymIndex > 30 ? explanation.Substring(acronymIndex - 30, 30) : explanation.Substring(0, acronymIndex);
                    var contextAfter = acronymIndex + acronym.Length + 40 < explanation.Length
                        ? explanation.Substring(acronymIndex, 40)
                        : explanation.Substring(acronymIndex);

                    var hasDefinition = contextBefore.Contains("(") || contextAfter.Contains("(") ||
                                        char.IsLower(contextAfter[acronym.Length + 1]);

                    hasDefinition.Should().BeTrue(
                        $"Acronym '{acronym}' in {description} should be defined or avoided: context = '{contextBefore + contextAfter}'");
                }
            }
        }
    }
}
