namespace MAA.Domain.Rules;

/// <summary>
/// Domain Entity: Federal Poverty Level (FPL)
/// Stores annual Federal Poverty Level thresholds used to calculate income eligibility
/// FPL tables are updated annually (typically January by HHS)
/// 
/// Phase 2 Implementation: T013
/// 
/// Example (2026 data):
/// - Household size 1: $14,580 annual = $1,215/month
/// - Household size 2: $19,720 annual = $1,643/month
/// - Household size 8+: Per-person increment applied
/// - Alaska: 1.25x multiplier for higher cost of living
/// - Hawaii: 1.15x multiplier for higher cost of living
/// </summary>
public class FederalPovertyLevel
{
    /// <summary>
    /// Unique identifier for this FPL record
    /// </summary>
    public Guid FplId { get; set; }

    /// <summary>
    /// The year this FPL applies to (e.g., 2026)
    /// </summary>
    public required int Year { get; set; }

    /// <summary>
    /// Household size (1-8, where 8 represents "8 or more")
    /// For household sizes 9+, the per-person increment formula is applied
    /// </summary>
    public required int HouseholdSize { get; set; }

    /// <summary>
    /// Annual FPL amount in cents (to ensure penny-precision without floating-point errors)
    /// For 2026 household of 1: 1,458,000 cents = $14,580
    /// 
    /// To convert to monthly: divide by 1200 (divide by 100 for cents, divide by 12 for months)
    /// To calculate a percentage: multiply by percentage / 100
    /// </summary>
    public required long AnnualIncomeCents { get; set; }

    /// <summary>
    /// State code for state-specific adjustments
    /// Examples: "AK" (Alaska), "HI" (Hawaii)
    /// When NULL, represents baseline FPL for 48 contiguous states + DC
    /// </summary>
    public string? StateCode { get; set; }

    /// <summary>
    /// Cost-of-living adjustment multiplier applied for specific states
    /// Examples: 
    /// - Alaska: 1.25 (25% higher)
    /// - Hawaii: 1.15 (15% higher)
    /// When NULL, no adjustment (1.0x is implied)
    /// </summary>
    public decimal? AdjustmentMultiplier { get; set; }

    /// <summary>
    /// Timestamp when this FPL record was created
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Timestamp when this FPL record was last updated
    /// </summary>
    public DateTime UpdatedAt { get; set; }

    /// <summary>
    /// Per-person increment for household sizes beyond 8
    /// Stored at entity level for reference during calculations
    /// Example (2026): $6,140 per additional person
    /// 
    /// Calculation for household of 9: BaseAmount(8) + PerPersonIncrement
    /// </summary>
    public static long PerPersonIncrementCents { get; } = 614000; // $6,140 cents representation

    /// <summary>
    /// Calculate the FPL for a given household size, accounting for per-person increments
    /// Used when HouseholdSize > 8
    /// </summary>
    /// <param name="baseAmountCents">FPL amount for household size 8</param>
    /// <param name="householdSizeAbove8">Household size minus 8 (e.g., 9-person household = 1)</param>
    /// <returns>Adjusted FPL amount in cents</returns>
    public static long CalculateForLargeHousehold(long baseAmountCents, int householdSizeAbove8)
    {
        return baseAmountCents + (PerPersonIncrementCents * householdSizeAbove8);
    }

    /// <summary>
    /// Returns the annual FPL in the base unit (cents) with state adjustment applied
    /// </summary>
    public long GetAdjustedAnnualIncomeCents()
    {
        if (AdjustmentMultiplier.HasValue && AdjustmentMultiplier != 1)
        {
            return (long)(AnnualIncomeCents * AdjustmentMultiplier.Value);
        }
        return AnnualIncomeCents;
    }

    /// <summary>
    /// Returns the monthly FPL threshold (annual divided by 12)
    /// Converts cents to dollars in cents (divide by 100 for dollars, divide by 12 for months = divide by 1200)
    /// </summary>
    public long GetMonthlyIncomeCents()
    {
        return GetAdjustedAnnualIncomeCents() / 12;
    }
}
