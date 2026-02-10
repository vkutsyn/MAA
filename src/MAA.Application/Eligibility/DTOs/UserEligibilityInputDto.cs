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
    [JsonPropertyName("state_code")]
    public required string StateCode { get; set; }

    /// <summary>
    /// Household size (1-8+)
    /// Used to calculate Federal Poverty Level thresholds
    /// </summary>
    [JsonPropertyName("household_size")]
    public required int HouseholdSize { get; set; }

    /// <summary>
    /// Monthly household income in cents (to avoid floating-point precision issues)
    /// Example: $2,100/month = 210000 cents
    /// </summary>
    [JsonPropertyName("monthly_income_cents")]
    public required long MonthlyIncomeCents { get; set; }

    /// <summary>
    /// User's age in years
    /// Determines eligibility pathway (18-64 = MAGI, 65+ = Aged)
    /// </summary>
    [JsonPropertyName("age")]
    public required int Age { get; set; }

    /// <summary>
    /// Whether user has documented disability
    /// Enables Disabled Medicaid pathway eligibility
    /// </summary>
    [JsonPropertyName("has_disability")]
    public bool HasDisability { get; set; }

    /// <summary>
    /// Whether user is currently pregnant
    /// Enables Pregnancy-Related Medicaid pathway with enhanced income limits
    /// </summary>
    [JsonPropertyName("is_pregnant")]
    public bool IsPregnant { get; set; }

    /// <summary>
    /// Whether user receives Supplemental Security Income (SSI)
    /// SSI receipt provides categorical eligibility in all states
    /// </summary>
    [JsonPropertyName("receives_ssi")]
    public bool ReceivesSsi { get; set; }

    /// <summary>
    /// Whether user is U.S. citizen or qualified immigrant
    /// Required for most Medicaid programs (emergency Medicaid excluded)
    /// </summary>
    [JsonPropertyName("is_citizen")]
    public required bool IsCitizen { get; set; }

    /// <summary>
    /// Household assets in cents (savings, investments, property)
    /// Some programs have asset limits or consider assets for categorically eligibility
    /// Optional: not all programs check assets
    /// </summary>
    [JsonPropertyName("assets_cents")]
    public long? AssetsCents { get; set; }
}
