namespace MAA.Application.Eligibility.DTOs;

/// <summary>
/// Eligibility Evaluation Result Data Transfer Object
/// Contains all information about an eligibility evaluation outcome
/// 
/// Phase 2 Implementation: T018
/// </summary>
public class EligibilityResultDto
{
    /// <summary>
    /// When the evaluation was performed
    /// </summary>
    public required DateTime EvaluationDate { get; set; }

    /// <summary>
    /// Overall eligibility status (Likely Eligible, Possibly Eligible, Unlikely Eligible)
    /// </summary>
    public required string Status { get; set; }

    /// <summary>
    /// Overall confidence score (0-100)
    /// </summary>
    public required int ConfidenceScore { get; set; }

    /// <summary>
    /// Plain-language explanation of the evaluation result
    /// Flesch-Kincaid target: ≤8th grade reading level
    /// </summary>
    public required string Explanation { get; set; }

    /// <summary>
    /// Programs user qualifies for (for multi-program evaluations)
    /// </summary>
    public List<ProgramMatchDto> MatchedPrograms { get; set; } = new();

    /// <summary>
    /// Version of the rule used for evaluation
    /// Supports audit trail and rule versioning
    /// </summary>
    public decimal? RuleVersionUsed { get; set; }

    /// <summary>
    /// State code evaluated
    /// </summary>
    public required string StateCode { get; set; }
}

/// <summary>
/// Program Match Data Transfer Object
/// Represents a single program match within a multi-program evaluation
/// 
/// Phase 4 Implementation: T038
/// </summary>
public class ProgramMatchDto
{
    /// <summary>
    /// Unique identifier for the Medicaid program (GUID)
    /// </summary>
    public Guid ProgramId { get; set; }

    /// <summary>
    /// Human-readable program name (e.g., "MAGI Adult Medicaid")
    /// </summary>
    public required string ProgramName { get; set; }

    /// <summary>
    /// Confidence score specific to this program match (0-100)
    /// </summary>
    public required int ConfidenceScore { get; set; }

    /// <summary>
    /// Program-specific explanation of eligibility
    /// Plain-language, ≤8th grade reading level
    /// </summary>
    public required string Explanation { get; set; }

    /// <summary>
    /// Factors that made user eligible for this program
    /// Used to explain the positive decision
    /// </summary>
    public List<string> MatchingFactors { get; set; } = new();

    /// <summary>
    /// Factors that might disqualify user from this program
    /// If populated, may indicate conditional eligibility (needs verification)
    /// </summary>
    public List<string> DisqualifyingFactors { get; set; } = new();

    /// <summary>
    /// Eligibility pathway for this program
    /// (MAGI, NonMAGI_Aged, NonMAGI_Disabled, SSI_Linked, Pregnancy, Other)
    /// </summary>
    public string? EligibilityPathway { get; set; }
}
