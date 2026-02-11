using System.Text.Json.Serialization;

namespace MAA.Application.Eligibility.DTOs;

public class EligibilityEvaluateResponseDto
{
    [JsonPropertyName("status")]
    public required string Status { get; set; }

    [JsonPropertyName("matchedPrograms")]
    public List<EligibilityEvaluateProgramMatchDto> MatchedPrograms { get; set; } = new();

    [JsonPropertyName("confidenceScore")]
    public int ConfidenceScore { get; set; }

    [JsonPropertyName("explanation")]
    public required string Explanation { get; set; }

    [JsonPropertyName("explanationItems")]
    public List<EligibilityExplanationItemDto> ExplanationItems { get; set; } = new();

    [JsonPropertyName("ruleVersionUsed")]
    public string? RuleVersionUsed { get; set; }

    [JsonPropertyName("evaluatedAt")]
    public DateTime EvaluatedAt { get; set; }
}

public class EligibilityEvaluateProgramMatchDto
{
    [JsonPropertyName("programCode")]
    public required string ProgramCode { get; set; }

    [JsonPropertyName("programName")]
    public required string ProgramName { get; set; }

    [JsonPropertyName("confidenceScore")]
    public int ConfidenceScore { get; set; }

    [JsonPropertyName("explanation")]
    public required string Explanation { get; set; }
}

public class EligibilityExplanationItemDto
{
    [JsonPropertyName("criterionId")]
    public required string CriterionId { get; set; }

    [JsonPropertyName("message")]
    public required string Message { get; set; }

    [JsonPropertyName("status")]
    public required string Status { get; set; }

    [JsonPropertyName("glossaryReference")]
    public string? GlossaryReference { get; set; }
}
