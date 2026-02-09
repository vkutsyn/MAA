using System.Threading.Tasks;

namespace MAA.Application.Eligibility.Services;

/// <summary>
/// Interface for calculating Federal Poverty Level (FPL) based income thresholds
/// 
/// Phase 7 Implementation: T059 (interface definition)
/// 
/// Responsibilities:
/// - Calculate income thresholds using FPL percentages
/// - Retrieve FPL amounts by year, household size, and state
/// - Support household size 1-8+ with per-person increments
/// - Apply state-specific adjustments
/// </summary>
public interface IFPLThresholdCalculator
{
    /// <summary>
    /// Calculates threshold from a given FPL amount and percentage.
    /// Pure calculation without database access.
    /// </summary>
    long CalculateThreshold(long fplAmountCents, int percentageMultiplier);

    /// <summary>
    /// Gets baseline FPL for a year and household size (no state adjustments).
    /// </summary>
    Task<long> GetBaselineFplAsync(int year, int householdSize);

    /// <summary>
    /// Gets state-adjusted FPL including Alaska/Hawaii multipliers.
    /// Falls back to baseline if no state-specific record exists.
    /// </summary>
    Task<long> GetStateFplAsync(int year, int householdSize, string stateCode);

    /// <summary>
    /// Gets FPL for current year with optional state adjustment.
    /// </summary>
    Task<long> GetCurrentYearFplAsync(int householdSize, string? stateCode = null);

    /// <summary>
    /// Calculates threshold by looking up FPL and applying percentage.
    /// Combines GetFpl + CalculateThreshold in one convenient call.
    /// </summary>
    Task<long> CalculateThresholdAsync(
        int year,
        int householdSize,
        int percentageMultiplier,
        string? stateCode = null);

    /// <summary>
    /// Gets per-person increment for household 8+ calculations.
    /// Used to calculate FPL for household sizes beyond 8.
    /// </summary>
    Task<long> GetPerPersonIncrementAsync(int year, string? stateCode = null);

    /// <summary>
    /// Calculates FPL for household size 8+ using per-person increment.
    /// </summary>
    Task<long> GetFplForExtendedHouseholdAsync(
        int year,
        int householdSize,
        string? stateCode = null);
}

