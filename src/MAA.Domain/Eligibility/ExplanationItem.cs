namespace MAA.Domain.Eligibility;

/// <summary>
/// Represents a single explanation item for an eligibility determination criterion.
/// </summary>
public class ExplanationItem
{
    /// <summary>
    /// Unique identifier for the criterion being explained (e.g., "citizenship_requirement").
    /// </summary>
    public required string CriterionId { get; set; }

    /// <summary>
    /// Plain-language explanation of the criterion and its result.
    /// </summary>
    public required string Message { get; set; }

    /// <summary>
    /// Status of the criterion: Met, Unmet, or Missing.
    /// </summary>
    public required ExplanationItemStatus Status { get; set; }

    /// <summary>
    /// Optional glossary term if jargon is used and needs explanation.
    /// </summary>
    public string? GlossaryReference { get; set; }
}

/// <summary>
/// Indicates the status of a criterion in the eligibility determination.
/// </summary>
public enum ExplanationItemStatus
{
    /// <summary>
    /// The criterion was satisfied by the applicant's data.
    /// </summary>
    Met = 0,

    /// <summary>
    /// The criterion was not satisfied by the applicant's data.
    /// </summary>
    Unmet = 1,

    /// <summary>
    /// The criterion could not be evaluated due to missing data.
    /// </summary>
    Missing = 2
}
