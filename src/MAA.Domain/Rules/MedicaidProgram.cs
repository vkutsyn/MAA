namespace MAA.Domain.Rules;

/// <summary>
/// Domain Entity: Medicaid Program
/// Represents a Medicaid program offered by a state
/// Example: "MAGI Adult", "Aged Medicaid" in Illinois
/// 
/// Phase 2 Implementation: T011
/// 
/// Properties follow Clean Architecture principles:
/// - No database attributes (no [Column], [Table] decorators)
/// - Relationships are through foreign keys (IDs)
/// - Timestamps for audit trail
/// </summary>
public class MedicaidProgram
{
    /// <summary>
    /// Unique identifier for this program
    /// </summary>
    public Guid ProgramId { get; set; }

    /// <summary>
    /// State code (e.g., "IL", "CA", "NY")
    /// </summary>
    public required string StateCode { get; set; }

    /// <summary>
    /// Human-readable program name (e.g., "MAGI Adult", "Aged Medicaid")
    /// </summary>
    public required string ProgramName { get; set; }

    /// <summary>
    /// Unique program code for this state (e.g., "IL_MAGI_ADULT")
    /// </summary>
    public required string ProgramCode { get; set; }

    /// <summary>
    /// The eligibility pathway this program follows
    /// Examples: MAGI, NonMAGI_Aged, NonMAGI_Disabled, SSI_Linked, Pregnancy
    /// </summary>
    public required EligibilityPathway EligibilityPathway { get; set; }

    /// <summary>
    /// Plain-language description of the program for non-technical stakeholders
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Timestamp when this program was created in the system
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Timestamp when this program was last updated
    /// </summary>
    public DateTime UpdatedAt { get; set; }

    /// <summary>
    /// Navigation property: Rules associated with this program
    /// </summary>
    public ICollection<EligibilityRule> Rules { get; set; } = new List<EligibilityRule>();
}

/// <summary>
/// Enumeration of eligibility pathways
/// Represents different Medicaid eligibility routes available
/// </summary>
public enum EligibilityPathway
{
    /// <summary>Modified Adjusted Gross Income pathway (income-based for working-age adults)</summary>
    MAGI,

    /// <summary>Non-MAGI pathway for elderly (65+) individuals</summary>
    NonMAGI_Aged,

    /// <summary>Non-MAGI pathway for individuals with disabilities</summary>
    NonMAGI_Disabled,

    /// <summary>SSI-Linked (Supplemental Security Income recipients)</summary>
    SSI_Linked,

    /// <summary>Pregnancy-related Medicaid pathway</summary>
    Pregnancy,

    /// <summary>Other state-specific pathways</summary>
    Other
}
