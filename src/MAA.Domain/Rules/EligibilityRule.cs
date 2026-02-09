namespace MAA.Domain.Rules;

/// <summary>
/// Domain Entity: Eligibility Rule
/// Represents a versioned, dated rule for evaluating eligibility for a specific program
/// Stores rule logic as JSON to enable flexible evaluation without code compilation
/// 
/// Phase 2 Implementation: T012
/// 
/// Example:
/// - Program: "MAGI Adult"
/// - Version: 1.0
/// - EffectiveDate: 2026-01-01
/// - RuleLogic: { "if": [ { "<=": [ { "var": "monthly_income" }, 2500 ] }, "eligible", "ineligible" ] }
/// </summary>
public class EligibilityRule
{
    /// <summary>
    /// Unique identifier for this rule
    /// </summary>
    public Guid RuleId { get; set; }

    /// <summary>
    /// Foreign key: References the MedicaidProgram this rule belongs to
    /// </summary>
    public required Guid ProgramId { get; set; }

    /// <summary>
    /// The program this rule belongs to (navigation property)
    /// </summary>
    public MedicaidProgram? Program { get; set; }

    /// <summary>
    /// State code (denormalized for efficient state-scoped queries)
    /// Examples: "IL", "CA", "NY", "TX", "FL"
    /// </summary>
    public required string StateCode { get; set; }

    /// <summary>
    /// Human-readable name for this rule
    /// Example: "IL MAGI Adult Income Threshold 2026"
    /// </summary>
    public required string RuleName { get; set; }

    /// <summary>
    /// Version number for this rule (e.g., 1.0, 1.1, 2.0)
    /// Allows tracking of rule evolution and historical evaluation
    /// </summary>
    public required decimal Version { get; set; }

    /// <summary>
    /// Rule logic stored as JSON (JSONLogic format or custom DSL)
    /// Examples:
    /// - Income threshold: { "<=": [ { "var": "monthly_income" }, 2500 ] }
    /// - And condition: { "and": [ condition1, condition2, ... ] }
    /// - If-then: { "if": [ condition, "eligible", "ineligible" ] }
    /// </summary>
    public required string RuleLogic { get; set; }

    /// <summary>
    /// Date when this rule becomes active for evaluations
    /// Example: 2026-01-01 (new fiscal year)
    /// </summary>
    public required DateTime EffectiveDate { get; set; }

    /// <summary>
    /// Date when this rule is superseded by a newer version
    /// When NULL, rule is currently active
    /// Example: 2026-06-30 (rule expires at end of June)
    /// </summary>
    public DateTime? EndDate { get; set; }

    /// <summary>
    /// ID of the user/admin who created this rule
    /// Used for audit trail and accountability
    /// </summary>
    public Guid? CreatedBy { get; set; }

    /// <summary>
    /// Timestamp when this rule was created
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Timestamp when this rule was last updated
    /// </summary>
    public DateTime UpdatedAt { get; set; }

    /// <summary>
    /// Plain-language description of what this rule does
    /// Helps admins understand the rule's purpose without parsing JSON
    /// Example: "Eligible if monthly income is at or below $2,500 for household of 2 in IL"
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Returns true if this rule is currently active (effective but not ended)
    /// </summary>
    public bool IsActive => DateTime.UtcNow.Date >= EffectiveDate.Date && (!EndDate.HasValue || DateTime.UtcNow.Date <= EndDate.Value.Date);
}
