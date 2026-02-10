using System.Text.Json.Serialization;

namespace MAA.Application.Eligibility.DTOs;

/// <summary>
/// User Eligibility Input Data Transfer Object
/// Contains household and personal data for eligibility evaluation
/// 
/// Phase 2 Implementation: T017
/// </summary>
public class UserEligibilityInputDto
{
    /// <summary>
    /// State code (IL, CA, NY, TX, FL)
    /// Required for state-specific rule evaluation
    /// </summary>
    /// <remarks>
    /// Format: Two-letter state code (ISO 3166-2)
    /// Constraints: Required, must be valid state code
    /// Validation: Checked against list of supported states
    /// Example: "CA"
    /// Used for: Applying state-specific income thresholds and rules
    /// </remarks>
    [JsonPropertyName("state_code")]
    public required string StateCode { get; set; }

    /// <summary>
    /// Household size (1-8+)
    /// Used to calculate Federal Poverty Level thresholds
    /// </summary>
    /// <remarks>
    /// Format: Integer number of household members
    /// Constraints: Required, 1-8 (8 means 8+)
    /// Validation: Must be positive integer
    /// Example: 4 (represents household of 4)
    /// Used to calculate: FPL thresholds for income eligibility
    /// Definition: Anyone who shares food/living expenses
    /// </remarks>
    [JsonPropertyName("household_size")]
    public required int HouseholdSize { get; set; }

    /// <summary>
    /// Monthly household income in cents (to avoid floating-point precision issues)
    /// Example: $2,100/month = 210000 cents
    /// </summary>
    /// <remarks>
    /// Format: Integer cents (no decimal point)
    /// Constraints: Required, non-negative
    /// Conversion: Multiply dollars by 100; e.g., $2,100.50 = 210050 cents
    /// Validation: Must not exceed 999,999,999 cents (~$9,999,999/month)
    /// Example: 450000 (represents $4,500.00/month)
    /// Used for: Calculating income-to-FPL ratio for eligibility
    /// Warning: Include all household members' income
    /// </remarks>
    [JsonPropertyName("monthly_income_cents")]
    public required long MonthlyIncomeCents { get; set; }

    /// <summary>
    /// User's age in years
    /// Determines eligibility pathway (18-64 = MAGI, 65+ = Aged)
    /// </summary>
    /// <remarks>
    /// Format: Integer age in years
    /// Constraints: Required, 0-150 (range covers birth certificate to very old age)
    /// Pathways:
    ///   - 0-17: Dependent Child pathway
    ///   - 18-64: MAGI Adult pathway
    ///   - 65+: Aged pathway (different eligibility rules)
    /// Example: 42
    /// Used for: Determining which eligibility rules to apply
    /// </remarks>
    [JsonPropertyName("age")]
    public required int Age { get; set; }

    /// <summary>
    /// Whether user has documented disability
    /// Enables Disabled Medicaid pathway eligibility
    /// </summary>
    /// <remarks>
    /// Format: Boolean flag
    /// Constraints: Optional, defaults to false
    /// Documentation: Must be medically documented/certified
    /// Pathway: Enables non-MAGI Disabled pathway
    /// Assets: Subject to asset limits under disabled pathway
    /// Example: false (no disability)
    /// Note: Even if true, user must still meet other criteria
    /// </remarks>
    [JsonPropertyName("has_disability")]
    public bool HasDisability { get; set; }

    /// <summary>
    /// Whether user is currently pregnant
    /// Enables Pregnancy-Related Medicaid pathway with enhanced income limits
    /// </summary>
    /// <remarks>
    /// Format: Boolean flag
    /// Constraints: Optional, defaults to false
    /// Pathway: Enables Pregnancy-Related Medicaid (usually 185% FPL)
    /// Duration: Typically covers pregnancy + 60 days postpartum
    /// Example: true (if currently pregnant)
    /// Note: Must provide documentation if claiming pregnancy status
    /// </remarks>
    [JsonPropertyName("is_pregnant")]
    public bool IsPregnant { get; set; }

    /// <summary>
    /// Whether user receives Supplemental Security Income (SSI)
    /// SSI receipt provides categorical eligibility in all states
    /// </summary>
    /// <remarks>
    /// Format: Boolean flag
    /// Constraints: Optional, defaults to false
    /// Pathway: SSI-Linked Medicaid (automatic eligibility)
    /// Verification: Must provide SSI award letter
    /// Example: false (not receiving SSI)
    /// Impact: If true, income/asset limits don't apply
    /// </remarks>
    [JsonPropertyName("receives_ssi")]
    public bool ReceivesSsi { get; set; }

    /// <summary>
    /// Whether user is U.S. citizen or qualified immigrant
    /// Required for most Medicaid programs (emergency Medicaid excluded)
    /// </summary>
    /// <remarks>
    /// Format: Boolean flag
    /// Constraints: Required
    /// Citizenship: Must be U.S. citizen or qualified immigrant
    /// Documentation: Requires proof (passport, birth certificate, SSN, etc.)
    /// Exception: Emergency Medicaid may apply to non-citizens
    /// Example: true (user is U.S. citizen)
    /// Impact: If false, eligibility severely restricted
    /// </remarks>
    [JsonPropertyName("is_citizen")]
    public required bool IsCitizen { get; set; }

    /// <summary>
    /// Household assets in cents (savings, investments, property)
    /// Some programs have asset limits or consider assets for categorically eligibility
    /// Optional: not all programs check assets
    /// </summary>
    /// <remarks>
    /// Format: Long integer cents (same format as income)
    /// Constraints: Optional (can be null), non-negative if provided
    /// Includes: Bank accounts, investments, property
    /// Excludes: Primary residence (usually), vehicle (usually)
    /// Conversion: Multiply dollars by 100; e.g., $5,000.00 = 500000 cents
    /// Example: 1000000 (represents $10,000.00 in household assets)
    /// Note: Used by some programs for asset limits (aged/disabled). MAGI programs ignore assets.
    /// </remarks>
    [JsonPropertyName("assets_cents")]
    public long? AssetsCents { get; set; }
}
