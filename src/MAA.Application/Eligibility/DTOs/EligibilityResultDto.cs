namespace MAA.Application.Eligibility.DTOs;

/// <summary>
/// Eligibility Evaluation Result Data Transfer Object
/// Contains all information about an eligibility evaluation outcome
/// 
/// Phase 2 Implementation: T018
/// Phase 3 Enhancement: T021 (Additional properties for multi-program evaluation)
/// </summary>
public class EligibilityResultDto
{
    /// <summary>
    /// When the evaluation was performed
    /// </summary>
    /// <remarks>
    /// Format: DateTime (ISO 8601, UTC)
    /// Constraints: Required
    /// Purpose: Audit trail and caching invalidation
    /// Example: "2026-02-10T15:00:00Z"
    /// </remarks>
    public required DateTime EvaluationDate { get; set; }

    /// <summary>
    /// Overall eligibility status (Likely Eligible, Possibly Eligible, Unlikely Eligible)
    /// </summary>
    /// <remarks>
    /// Allowed values: "Likely Eligible", "Possibly Eligible", "Unlikely Eligible"
    /// Constraints: Required, case-sensitive
    /// Usage: Primary decision indicator for UI display
    /// Scores:
    ///   - "Likely Eligible": ConfidenceScore >= 80%
    ///   - "Possibly Eligible": ConfidenceScore 50-79%
    ///   - "Unlikely Eligible": ConfidenceScore < 50%
    /// </remarks>
    public required string OverallStatus { get; set; }

    /// <summary>
    /// Overall confidence score (0-100)
    /// </summary>
    /// <remarks>
    /// Format: Integer percentage (0-100)
    /// Constraints: Required, non-negative
    /// Calculation: Weighted average of all program matches
    /// Interpretation:
    ///   - 90-100: Very confident in determination
    ///   - 70-89: Fairly confident
    ///   - 50-69: Moderate confidence (user should verify)
    ///   - < 50: Low confidence (recommend human review)
    /// </remarks>
    public required int ConfidenceScore { get; set; }

    /// <summary>
    /// Plain-language explanation of the evaluation result
    /// Flesch-Kincaid target: ≤8th grade reading level
    /// </summary>
    /// <remarks>
    /// Format: Plain English narrative (not technical)
    /// Constraints: Required, max 2000 characters
    /// Audience: End users (low literacy baseline)
    /// Purpose: Explain WHY the determination was made
    /// Example: "Based on your household income and family size, you may qualify for Medicaid. 
    ///           Please verify your information is correct before submitting."
    /// </remarks>
    public required string Explanation { get; set; }

    /// <summary>
    /// Programs user qualifies for (for multi-program evaluations)
    /// Sorted by confidence score descending
    /// </summary>
    /// <remarks>
    /// Format: Array of ProgramMatchDto objects
    /// Constraints: Can be empty if no matches found
    /// Ordering: Highest confidence score first
    /// Example: [{ "programName": "MAGI Adult Medicaid", "confidenceScore": 92 }, ...]
    /// </remarks>
    public List<ProgramMatchDto> MatchedPrograms { get; set; } = new();

    /// <summary>
    /// Programs where evaluation failed or returned unlikely status
    /// Useful for debugging and detailed analysis
    /// </summary>
    /// <remarks>
    /// Format: Array of ProgramMatchDto objects
    /// Constraints: Can be empty if all programs evaluated
    /// Usage: Helps explain negative determinations to users
    /// Content: Contains DisqualifyingFactors for each program
    /// </remarks>
    public List<ProgramMatchDto> FailedProgramEvaluations { get; set; } = new();

    /// <summary>
    /// Version of the rule used for evaluation
    /// Supports audit trail and rule versioning
    /// </summary>
    /// <remarks>
    /// Format: Decimal version number (e.g., 1.0, 2.3)
    /// Constraints: Can be null if rule versioning not tracked
    /// Purpose: Audit trail - which rule version made this decision
    /// Example: 2.1
    /// </remarks>
    public decimal? RuleVersionUsed { get; set; }

    /// <summary>
    /// State code evaluated
    /// </summary>
    /// <remarks>
    /// Format: Two-letter state code (ISO 3166-2)
    /// Constraints: Required, must be valid state code
    /// Example: "CA", "NY", "TX"
    /// Used: To apply state-specific eligibility rules
    /// </remarks>
    public required string StateCode { get; set; }

    /// <summary>
    /// Time taken to evaluate in milliseconds
    /// Performance metric for monitoring SLA compliance
    /// </summary>
    /// <remarks>
    /// Format: Integer milliseconds
    /// Constraints: Non-negative
    /// Purpose: Performance monitoring and SLA compliance
    /// Target: < 100ms per requirements
    /// Example: 45 (milliseconds)
    /// </remarks>
    public long EvaluationDurationMs { get; set; }

    /// <summary>
    /// Summary of user input used for evaluation
    /// Helps trace evaluation back to user submission
    /// Example: "Household: 4, Income: $2500M"
    /// </summary>
    /// <remarks>
    /// Format: Human-readable summary string
    /// Constraints: Can be null, max 500 characters
    /// Purpose: Audit trail and debugging
    /// Example: "Household: 4 persons, Income: $45000/year, Age: 35"
    /// </remarks>
    public string? UserInputSummary { get; set; }
}

/// <summary>
/// Program Match Data Transfer Object
/// Represents a single program match within a multi-program evaluation
/// 
/// Phase 2 Implementation: T018
/// Phase 3 Enhancement: T021 (Additional properties for detailed results)
/// </summary>
public class ProgramMatchDto
{
    /// <summary>
    /// Unique identifier for the Medicaid program (GUID)
    /// </summary>
    /// <remarks>
    /// Format: UUID v4
    /// Constraints: Non-empty
    /// Reference: Program database record
    /// Example: "550e8400-e29b-41d4-a716-446655440000"
    /// </remarks>
    public Guid ProgramId { get; set; }

    /// <summary>
    /// Human-readable program name (e.g., "MAGI Adult Medicaid")
    /// </summary>
    /// <remarks>
    /// Format: Plain English program name
    /// Constraints: Required, max 200 characters
    /// Examples: "MAGI Adult Medicaid", "CHIP", "SSI-Linked Medicaid", "Pregnancy Medicaid"
    /// Usage: For display to end users
    /// </remarks>
    public required string ProgramName { get; set; }

    /// <summary>
    /// Eligibility status for this program (Likely, Possibly, Unlikely Eligible)
    /// </summary>
    /// <remarks>
    /// Allowed values: "Likely Eligible", "Possibly Eligible", "Unlikely Eligible"
    /// Constraints: Can be null if not evaluated
    /// Semantics: Same as OverallStatus but per-program
    /// </remarks>
    public string? EligibilityStatus { get; set; }

    /// <summary>
    /// Confidence score specific to this program match (0-100)
    /// </summary>
    /// <remarks>
    /// Format: Integer percentage (0-100)
    /// Constraints: Required, non-negative
    /// Calculation: Based on rule evaluation for this program
    /// Usage: Developers can prioritize programs with higher scores
    /// Example: 92
    /// </remarks>
    public required int ConfidenceScore { get; set; }

    /// <summary>
    /// Program-specific explanation of eligibility
    /// Plain-language, ≤8th grade reading level
    /// </summary>
    /// <remarks>
    /// Format: Plain English narrative
    /// Constraints: Can be null, max 1000 characters
    /// Audience: End users
    /// Example: "Your household income qualifies you for MAGI Medicaid in your state."
    /// </remarks>
    public string? Explanation { get; set; }

    /// <summary>
    /// Factors that made user eligible for this program
    /// Used to explain the positive decision
    /// </summary>
    /// <remarks>
    /// Format: Array of human-readable factor descriptions
    /// Examples: ["Household income below 138% FPL", "Age >= 19", "Citizen or national"]
    /// Usage: Show to users why they qualify
    /// </remarks>
    public List<string> MatchingFactors { get; set; } = new();

    /// <summary>
    /// Factors that might disqualify user from this program
    /// If populated, may indicate conditional eligibility (needs verification)
    /// </summary>
    /// <remarks>
    /// Format: Array of human-readable factor descriptions
    /// Examples: ["Income exceeds threshold", "Not a citizen of this state"]
    /// Usage: Help users understand what could affect their eligibility
    /// </remarks>
    public List<string> DisqualifyingFactors { get; set; } = new();

    /// <summary>
    /// Version of the rule used to evaluate this program
    /// Supports audit trail and debugging
    /// </summary>
    /// <remarks>
    /// Format: Decimal version number
    /// Constraints: Can be null
    /// Example: 1.5
    /// </remarks>
    public decimal? RuleVersionUsed { get; set; }

    /// <summary>
    /// When this program was evaluated
    /// </summary>
    /// <remarks>
    /// Format: DateTime (ISO 8601, UTC)
    /// Constraints: Can be null
    /// Example: "2026-02-10T15:00:00Z"
    /// </remarks>
    public DateTime? EvaluatedAt { get; set; }

    /// <summary>
    /// Eligibility pathway for this program
    /// (MAGI, NonMAGI_Aged, NonMAGI_Disabled, SSI_Linked, Pregnancy, Other)
    /// </summary>
    /// <remarks>
    /// Allowed values: "MAGI", "NonMAGI_Aged", "NonMAGI_Disabled", "SSI_Linked", "Pregnancy", "Other"
    /// Constraints: Can be null
    /// Purpose: Indicates which rules were applied
    /// Example: "MAGI"
    /// </remarks>
    public string? EligibilityPathway { get; set; }
}
