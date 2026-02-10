namespace MAA.Domain.Rules;

/// <summary>
/// Pure Federal Poverty Level (FPL) Calculation Engine
/// Calculates income thresholds for eligibility based on FPL amounts and percentages
/// 
/// Phase 3 Implementation: T020
/// 
/// Key Properties:
/// - Pure function: Same input always produces same output
/// - No I/O: Does not access database or cache
/// - Deterministic: Results reproducible for audit and testing
/// - Works with centime precision (long → cents) to avoid floating-point errors
/// 
/// Example Usage:
///   var calculator = new FPLCalculator();
///   var fplAmount = 1458000; // 2026 FPL for household=1 in cents ($14,580)
///   var threshold138Percent = calculator.CalculateThreshold(fplAmount, 138, 1);
///   // Returns: 2012040 (cents) = $20,120.40
/// 
/// Income Conversion:
///   - Annual to monthly: divide by 12
///   - Cents to dollars: divide by 100
///   - So monthly income in cents = annual in cents / 1200
/// </summary>
public class FPLCalculator
{
    /// <summary>
    /// Per-person income increment for household size 8 and above
    /// 2026 value: $6,140 per person = 614,000 cents
    /// </summary>
    private const long PerPersonIncrementCents = 614000;

    /// <summary>
    /// Calculates an income threshold based on FPL and a percentage
    /// 
    /// Common thresholds:
    /// - 138%: MAGI Adult Medicaid in most states
    /// - 100%: Disabled/Aged baseline
    /// - 150%: Pregnant women pathway in some states
    /// - 133%: Texas Medicaid threshold
    /// </summary>
    /// <param name="fplAnnualIncomeCents">2026 FPL annual income in cents (e.g., 1458000 for $14,580)</param>
    /// <param name="percentageOfFpl">Percentage of FPL (e.g., 138 for 138%)</param>
    /// <param name="householdSize">Household size (1-8+)</param>
    /// <returns>Income threshold in cents for annual income</returns>
    /// <exception cref="ArgumentException">If inputs are invalid</exception>
    public long CalculateThresholdAnnualInCents(long fplAnnualIncomeCents, int percentageOfFpl, int householdSize)
    {
        ValidateInputs(fplAnnualIncomeCents, percentageOfFpl, householdSize);

        // Calculate adjusted FPL for household size if needed
        var adjustedFplCents = householdSize > 8
            ? CalculateFplForLargeHousehold(fplAnnualIncomeCents, householdSize)
            : fplAnnualIncomeCents;

        // Apply percentage
        var thresholdCents = (adjustedFplCents * percentageOfFpl) / 100;

        return thresholdCents;
    }

    /// <summary>
    /// Calculates monthly income threshold (for comparing against monthly income)
    /// </summary>
    /// <param name="fplAnnualIncomeCents">2026 FPL annual income in cents</param>
    /// <param name="percentageOfFpl">Percentage of FPL</param>
    /// <param name="householdSize">Household size (1-8+)</param>
    /// <returns>Income threshold in cents for monthly income</returns>
    public long CalculateThresholdMonthlyInCents(long fplAnnualIncomeCents, int percentageOfFpl, int householdSize)
    {
        var annualThreshold = CalculateThresholdAnnualInCents(fplAnnualIncomeCents, percentageOfFpl, householdSize);

        // Convert annual to monthly: divide by 12
        // Using 1200 = 100 (cents) * 12 (months)
        return annualThreshold / 12;
    }

    /// <summary>
    /// Determines if a user's income is at or below a threshold (income-eligible)
    /// </summary>
    /// <param name="userMonthlyIncomeCents">User's monthly income in cents</param>
    /// <param name="thresholdMonthlyIncomeCents">Threshold monthly income in cents</param>
    /// <returns>True if user income is at or below threshold</returns>
    public bool IsIncomeEligible(long userMonthlyIncomeCents, long thresholdMonthlyIncomeCents)
    {
        return userMonthlyIncomeCents <= thresholdMonthlyIncomeCents;
    }

    /// <summary>
    /// Calculates how far above or below a threshold the user's income is
    /// Useful for generating explanations ("Your income exceeds the limit by $X")
    /// </summary>
    /// <param name="userMonthlyIncomeCents">User's monthly income in cents</param>
    /// <param name="thresholdMonthlyIncomeCents">Threshold monthly income in cents</param>
    /// <returns>Difference in cents (negative if below, positive if above)</returns>
    public long CalculateIncomeDifference(long userMonthlyIncomeCents, long thresholdMonthlyIncomeCents)
    {
        return userMonthlyIncomeCents - thresholdMonthlyIncomeCents;
    }

    /// <summary>
    /// Gets the FPL amount for a specific household size, handling the 8+ increment rule
    /// </summary>
    /// <param name="baselineFplEightPersonCents">FPL for household of 8</param>
    /// <param name="householdSize">Household size (1-8+)</param>
    /// <returns>Adjusted FPL in cents</returns>
    public long GetFplForHouseholdSize(long baselineFplEightPersonCents, int householdSize)
    {
        ValidateHouseholdSize(householdSize);

        if (householdSize <= 8)
        {
            // Shouldn't happen in this context, but handle gracefully
            // Normally FPL for size 1-8 comes from the FPL table
            throw new ArgumentException(
                $"GetFplForHouseholdSize is for household size > 8. Use FPL table lookup for size 1-8. Got: {householdSize}",
                nameof(householdSize));
        }

        return CalculateFplForLargeHousehold(baselineFplEightPersonCents, householdSize);
    }

    /// <summary>
    /// Calculates FPL for household size > 8 using per-person increment
    /// Formula: FPL(8) + ((householdSize - 8) * perPersonIncrement)
    /// </summary>
    private long CalculateFplForLargeHousehold(long fplBaselineCents, int householdSize)
    {
        if (householdSize <= 8)
            return fplBaselineCents;

        var additionalPersons = householdSize - 8;
        var additionalIncome = PerPersonIncrementCents * additionalPersons;

        return fplBaselineCents + additionalIncome;
    }

    /// <summary>
    /// Validates input parameters
    /// </summary>
    private void ValidateInputs(long fplAnnualIncomeCents, int percentageOfFpl, int householdSize)
    {
        if (fplAnnualIncomeCents < 0)
            throw new ArgumentException("FPL annual income cannot be negative", nameof(fplAnnualIncomeCents));

        if (percentageOfFpl < 0 || percentageOfFpl > 500)
            throw new ArgumentException(
                "Percentage of FPL must be 0-500% (got: " + percentageOfFpl + "%)",
                nameof(percentageOfFpl));

        ValidateHouseholdSize(householdSize);
    }

    /// <summary>
    /// Validates household size
    /// </summary>
    private void ValidateHouseholdSize(int householdSize)
    {
        if (householdSize < 1 || householdSize > 50)
            throw new ArgumentException(
                "Household size must be 1-50 (got: " + householdSize + ")",
                nameof(householdSize));
    }

    /// <summary>
    /// Formats a cents value as a dollar amount string
    /// Useful for explanations and logging
    /// </summary>
    /// <param name="cents">Amount in cents</param>
    /// <returns>Formatted string like "$1,234.56"</returns>
    public static string FormatCentsAsDollars(long cents)
    {
        var dollars = cents / 100m;
        return dollars.ToString("C");
    }

    /// <summary>
    /// Formats a monthly income in cents as display string
    /// </summary>
    public static string FormatMonthlyIncome(long monthlyIncomeCents)
    {
        return FormatCentsAsDollars(monthlyIncomeCents);
    }

    /// <summary>
    /// Formats an annual income in cents as display string
    /// </summary>
    public static string FormatAnnualIncome(long annualIncomeCents)
    {
        return FormatCentsAsDollars(annualIncomeCents);
    }
}
