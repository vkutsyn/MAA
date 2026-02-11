namespace MAA.Domain.Eligibility;

public class EligibilityResult
{
    public EligibilityStatus Status { get; set; }
    public List<ProgramMatch> MatchedPrograms { get; set; } = new();
    public int ConfidenceScore { get; set; }
    public required string Explanation { get; set; }
    public List<ExplanationItem> ExplanationItems { get; set; } = new();
    public string? RuleVersionUsed { get; set; }
    public DateTime EvaluatedAt { get; set; }
}

public enum EligibilityStatus
{
    Likely,
    Possibly,
    Unlikely
}

