using MAA.Domain.Rules;
using MAA.Infrastructure.Data;
using MAA.Application.Eligibility.Repositories;
using Microsoft.EntityFrameworkCore;

namespace MAA.Infrastructure.DataAccess;

/// <summary>
/// Repository for accessing Federal Poverty Level (FPL) data from the database
/// 
/// Phase 3 Implementation: T024
/// 
/// Key Responsibilities:
/// - Query FPL amounts by year and household size
/// - Support state-specific adjustments (Alaska 1.25x, Hawaii 1.15x)
/// - Handle household size 8+ with per-person increment
/// - Enable efficient lookups with proper indexing
/// 
/// Database Schema (from SessionContext):
///   federal_poverty_levels table:
///   - fpl_id (PK, Guid)
///   - year (int, e.g., 2026)
///   - household_size (int, 1-8)
///   - annual_income_cents (long, in cents)
///   - state_code (string, nullable - NULL for baseline, "AK"/"HI" for adjustments)
///   - adjustment_multiplier (decimal, 1.0 for baseline, 1.25 for AK, 1.15 for HI)
///   - created_at (DateTime)
/// 
///   Index: (year, household_size, state_code)
///   
/// 2026 Data (from Phase 2 seeding):
///   - Baseline FPL (IL, CA, NY, TX, FL): $14,580 for household=1
///   - Alaska adjustment: × 1.25
///   - Hawaii adjustment: × 1.15
///   - Per-person increment (8+): $6,140 per additional person
/// </summary>
/// <summary>
/// EF Core implementation of FPL repository
/// </summary>
public class FplRepository : IFplRepository
{
    private readonly SessionContext _context;

    public FplRepository(SessionContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    /// <summary>
    /// Gets baseline FPL for a household size
    /// Queries for state_code = NULL, which represents baseline values
    /// </summary>
    public async Task<FederalPovertyLevel?> GetFplByYearAndHouseholdSizeAsync(int year, int householdSize)
    {
        if (year < 2000 || year > 2100)
            throw new ArgumentException("Year must be between 2000-2100", nameof(year));
        
        if (householdSize < 1 || householdSize > 8)
            throw new ArgumentException("Household size must be 1-8", nameof(householdSize));

        return await _context.FederalPovertyLevels
            .FirstOrDefaultAsync(f => f.Year == year 
                                     && f.HouseholdSize == householdSize
                                     && f.StateCode == null)  // Baseline has null state code
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Gets state-adjusted FPL for a household size
    /// For Alaska (AK) and Hawaii (HI) which have higher cost of living
    /// Falls back to baseline if state adjustment not found
    /// </summary>
    public async Task<FederalPovertyLevel?> GetFplForStateAsync(int year, int householdSize, string stateCode)
    {
        if (year < 2000 || year > 2100)
            throw new ArgumentException("Year must be between 2000-2100", nameof(year));
        
        if (householdSize < 1 || householdSize > 8)
            throw new ArgumentException("Household size must be 1-8", nameof(householdSize));

        ArgumentException.ThrowIfNullOrWhiteSpace(stateCode, nameof(stateCode));

        // First try state-specific adjustment
        var stateFpl = await _context.FederalPovertyLevels
            .FirstOrDefaultAsync(f => f.Year == year 
                                     && f.HouseholdSize == householdSize
                                     && f.StateCode == stateCode)
            .ConfigureAwait(false);

        if (stateFpl != null)
            return stateFpl;

        // Fall back to baseline if no state-specific record
        return await GetFplByYearAndHouseholdSizeAsync(year, householdSize)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Gets all FPL records for a year (baseline + all state adjustments)
    /// Includes NULL state_code (baseline) and specific state codes (AK, HI)
    /// </summary>
    public async Task<IEnumerable<FederalPovertyLevel>> GetFplsByYearAsync(int year)
    {
        if (year < 2000 || year > 2100)
            throw new ArgumentException("Year must be between 2000-2100", nameof(year));

        return await _context.FederalPovertyLevels
            .Where(f => f.Year == year)
            .OrderBy(f => f.StateCode ?? "")  // Baseline (null) sorts first
            .ThenBy(f => f.HouseholdSize)
            .ToListAsync()
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Gets baseline FPL records (state_code = NULL)
    /// Excludes state-specific adjustments
    /// </summary>
    public async Task<IEnumerable<FederalPovertyLevel>> GetBaselineFplsByYearAsync(int year)
    {
        if (year < 2000 || year > 2100)
            throw new ArgumentException("Year must be between 2000-2100", nameof(year));

        return await _context.FederalPovertyLevels
            .Where(f => f.Year == year && f.StateCode == null)
            .OrderBy(f => f.HouseholdSize)
            .ToListAsync()
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Gets state-specific adjustments (for Alaska and Hawaii)
    /// </summary>
    public async Task<IEnumerable<FederalPovertyLevel>> GetStateFplAdjustmentsByYearAsync(int year, string stateCode)
    {
        if (year < 2000 || year > 2100)
            throw new ArgumentException("Year must be between 2000-2100", nameof(year));

        ArgumentException.ThrowIfNullOrWhiteSpace(stateCode, nameof(stateCode));

        return await _context.FederalPovertyLevels
            .Where(f => f.Year == year && f.StateCode == stateCode)
            .OrderBy(f => f.HouseholdSize)
            .ToListAsync()
            .ConfigureAwait(false);
    }
}
