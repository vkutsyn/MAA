using MAA.Application.Eligibility.Repositories;
using MAA.Domain.Rules.Exceptions;
using System;
using System.Threading.Tasks;

namespace MAA.Application.Eligibility.Services;

/// <summary>
/// Service for calculating Medicaid income thresholds based on Federal Poverty Level (FPL) data
/// 
/// Phase 7 Implementation: T059
/// 
/// Key Responsibilities:
/// - Calculate income thresholds for eligibility (e.g., 138% FPL)
/// - Handle household size 1-8+ with per-person increment for 8+
/// - Apply state-specific FPL adjustments (Alaska 1.25x, Hawaii 1.15x)
/// - Support year-based FPL lookups (enables annual updates)
/// - Provide flexible percentage-based calculations (138%, 150%, 160%, 200%, 213% FPL)
/// 
/// Algorithm for Household 8+:
///   1. Get FPL for household size 8
///   2. Calculate per-person increment: (FPL_8 - FPL_7) / 1
///   3. For household X (where X > 8):
///      - Base: FPL_8
///      - Additional: (X - 8) × per-person increment
///      - Total: FPL_8 + ((X - 8) × per-person increment)
/// 
/// Example (2026 baseline):
///   - FPL for household 1: $14,580
///   - FPL for household 4: $29,960  
///   - FPL for household 8: $61,560
///   - Per-person increment: $7,700
///   - FPL for household 10: $61,560 + (2 × $7,700) = $76,960
///   - 138% FPL for household 4: $29,960 × 1.38 = $41,346
/// 
/// Design Patterns:
/// - Service pattern: Orchestrates repository calls and calculations
/// - Dependency injection: FPL repository injected
/// - Pure computation: Threshold calculations are deterministic
/// - Error handling: Validates inputs, provides meaningful exceptions
/// 
/// Performance:
/// - Single FPL repository call per threshold calculation
/// - Results can be cached at application layer (see FPLCacheService T061)
/// 
/// Constitutional Alignment:
/// - CONST-I (Code Quality): Single responsibility (threshold calculation)
/// - CONST-IV (Performance): <10ms lookup time with caching
/// </summary>
public class FPLThresholdCalculator : IFPLThresholdCalculator
{
    private readonly IFplRepository _fplRepository;

    public FPLThresholdCalculator(IFplRepository fplRepository)
    {
        _fplRepository = fplRepository ?? throw new ArgumentNullException(nameof(fplRepository));
    }

    /// <summary>
    /// Calculates income threshold for a given FPL percentage and household size.
    /// Uses current year (2026) by default.
    /// </summary>
    /// <param name="fplAmountCents">FPL baseline in cents (e.g., 1458000 for $14,580)</param>
    /// <param name="percentageMultiplier">Percentage multiplier (e.g., 138 for 138% FPL)</param>
    /// <returns>Threshold in cents</returns>
    public long CalculateThreshold(long fplAmountCents, int percentageMultiplier)
    {
        if (fplAmountCents < 0)
            throw new ArgumentException("FPL amount must be non-negative", nameof(fplAmountCents));

        if (percentageMultiplier < 0 || percentageMultiplier > 1000)
            throw new ArgumentException("Percentage multiplier must be between 0-1000", nameof(percentageMultiplier));

        // Calculate threshold: FPL × (percentage / 100)
        decimal threshold = (decimal)fplAmountCents * ((decimal)percentageMultiplier / 100m);

        return (long)Math.Round(threshold);
    }

    /// <summary>
    /// Gets baseline FPL for a specific year and household size.
    /// Returns FPL in cents.
    /// </summary>
    public async Task<long> GetBaselineFplAsync(int year, int householdSize)
    {
        if (year < 2000 || year > 2100)
            throw new ArgumentException("Year must be between 2000-2100", nameof(year));

        if (householdSize < 1 || householdSize > 8)
            throw new ArgumentException("Household size must be 1-8", nameof(householdSize));

        var fpl = await _fplRepository.GetFplByYearAndHouseholdSizeAsync(year, householdSize)
            .ConfigureAwait(false);

        if (fpl == null)
        {
            throw new EligibilityEvaluationException(
                $"FPL data not found for year {year}, household size {householdSize}");
        }

        return fpl.AnnualIncomeCents;
    }

    /// <summary>
    /// Gets state-adjusted FPL for a specific year and household size.
    /// Includes state-specific adjustments (Alaska 1.25x, Hawaii 1.15x).
    /// Returns FPL in cents.
    /// </summary>
    public async Task<long> GetStateFplAsync(int year, int householdSize, string stateCode)
    {
        if (year < 2000 || year > 2100)
            throw new ArgumentException("Year must be between 2000-2100", nameof(year));

        if (householdSize < 1 || householdSize > 8)
            throw new ArgumentException("Household size must be 1-8", nameof(householdSize));

        ArgumentException.ThrowIfNullOrWhiteSpace(stateCode, nameof(stateCode));

        var fpl = await _fplRepository.GetFplForStateAsync(year, householdSize, stateCode)
            .ConfigureAwait(false);

        if (fpl == null)
        {
            throw new EligibilityEvaluationException(
                $"FPL data not found for year {year}, household size {householdSize}, state {stateCode}");
        }

        return fpl.AnnualIncomeCents;
    }

    /// <summary>
    /// Gets FPL for current year (2026).
    /// Convenience method for evaluation without specifying year.
    /// </summary>
    public async Task<long> GetCurrentYearFplAsync(int householdSize, string? stateCode = null)
    {
        int currentYear = DateTime.UtcNow.Year;

        if (string.IsNullOrWhiteSpace(stateCode))
        {
            return await GetBaselineFplAsync(currentYear, householdSize)
                .ConfigureAwait(false);
        }

        return await GetStateFplAsync(currentYear, householdSize, stateCode)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Calculates threshold for a given FPL percentage and year/household/state.
    /// Convenience method combining FPL lookup and threshold calculation.
    /// </summary>
    public async Task<long> CalculateThresholdAsync(
        int year,
        int householdSize,
        int percentageMultiplier,
        string? stateCode = null)
    {
        long fplAmountCents;

        if (string.IsNullOrWhiteSpace(stateCode))
        {
            fplAmountCents = await GetBaselineFplAsync(year, householdSize)
                .ConfigureAwait(false);
        }
        else
        {
            fplAmountCents = await GetStateFplAsync(year, householdSize, stateCode)
                .ConfigureAwait(false);
        }

        return CalculateThreshold(fplAmountCents, percentageMultiplier);
    }

    /// <summary>
    /// Gets per-person increment for household 8+ calculations.
    /// This is the difference between FPL for household 8 and 7.
    /// </summary>
    public async Task<long> GetPerPersonIncrementAsync(int year, string? stateCode = null)
    {
        long fpl7, fpl8;

        if (string.IsNullOrWhiteSpace(stateCode))
        {
            fpl7 = await GetBaselineFplAsync(year, 7).ConfigureAwait(false);
            fpl8 = await GetBaselineFplAsync(year, 8).ConfigureAwait(false);
        }
        else
        {
            fpl7 = await GetStateFplAsync(year, 7, stateCode).ConfigureAwait(false);
            fpl8 = await GetStateFplAsync(year, 8, stateCode).ConfigureAwait(false);
        }

        return fpl8 - fpl7;
    }

    /// <summary>
    /// Calculates FPL for household size 8+ using per-person increment.
    /// For household sizes 1-8: returns exact FPL.
    /// For household size 9+: calculates using formula.
    /// </summary>
    public async Task<long> GetFplForExtendedHouseholdAsync(
        int year,
        int householdSize,
        string? stateCode = null)
    {
        if (householdSize < 1)
            throw new ArgumentException("Household size must be at least 1", nameof(householdSize));

        // For household 1-8, return exact FPL
        if (householdSize <= 8)
        {
            return await GetCurrentYearFplAsync(householdSize, stateCode)
                .ConfigureAwait(false);
        }

        // For household 9+, calculate using per-person increment
        long fpl8 = await GetCurrentYearFplAsync(8, stateCode).ConfigureAwait(false);
        long increment = await GetPerPersonIncrementAsync(year, stateCode).ConfigureAwait(false);

        long additionalPersons = householdSize - 8;
        long additionalCost = additionalPersons * increment;

        return fpl8 + additionalCost;
    }
}

