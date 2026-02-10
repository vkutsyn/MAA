using MAA.Domain.Rules;
using Microsoft.EntityFrameworkCore;

namespace MAA.Infrastructure.Data.Rules;

/// <summary>
/// Repository for state-specific rule and program queries
/// 
/// Phase 5 Implementation: T045
/// 
/// Purpose:
/// - Provides efficient state-scoped queries for multi-program evaluation
/// - Loads all programs for a state with their active rules in one query (N+1 optimization)
/// - Supports state-specific caching by StateRuleLoader
/// - Handles effective date filtering for rule versioning
/// 
/// Key Features:
/// - GetAllProgramsForState: Fetch all programs for a state
/// - GetProgramsWithRules: Fetch programs with their active rules (join query)
/// - Handles effective date logic (active rules only)
/// - Supports concurrent access with async/await
/// 
/// Performance Target:
/// - State query: <100ms (with database indexes on state_code, effective_date)
/// - Cacheability: Results cached by StateRuleLoader for 1 hour
/// 
/// Database Indexes Assumed:
/// - medicaid_programs(state_code, eligibility_pathway)
/// - eligibility_rules(program_id, effective_date, end_date)
/// - eligibility_rules(state_code, effective_date)
/// 
/// Usage:
///   var repo = new StateRuleRepository(context);
///   var programs = await repo.GetAllProgramsForStateAsync("IL");
///   var programsWithRules = await repo.GetProgramsWithRulesAsync("IL", DateTime.UtcNow);
/// 
/// Integration:
/// - Used by StateRuleLoader (T044) to load state rules
/// - Results are cached by StateRuleLoader for subsequent evaluations
/// - Supports multi-program matching in ProgramMatchingHandler
/// 
/// Reference:
/// - Spec: specs/002-rules-engine/spec.md US3
/// - Phase 5 Task: T045 State-Specific Rule Evaluation
/// </summary>
public class StateRuleRepository
{
    private readonly SessionContext _context;

    public StateRuleRepository(SessionContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    /// <summary>
    /// Gets all active programs for a specified state
    /// Returns only the program metadata (not rules)
    /// </summary>
    /// <param name="stateCode">Two-letter state code (IL, CA, NY, TX, FL)</param>
    /// <returns>List of MedicaidProgram objects for the state</returns>
    public async Task<IEnumerable<MedicaidProgram>> GetAllProgramsForStateAsync(string stateCode)
    {
        if (string.IsNullOrWhiteSpace(stateCode))
            throw new ArgumentException("State code cannot be null or empty", nameof(stateCode));

        stateCode = stateCode.ToUpperInvariant();

        return await _context.MedicaidPrograms
            .AsNoTracking()
            .Where(p => p.StateCode == stateCode)
            .OrderBy(p => p.ProgramName)
            .ToListAsync();
    }

    /// <summary>
    /// Gets all programs for a state with their active rules for a given date
    /// Performs a single query to retrieve programs with rules (N+1 optimization)
    /// </summary>
    /// <param name="stateCode">Two-letter state code</param>
    /// <param name="effectiveDate">The date to check rule effectiveness (typically today)</param>
    /// <returns>List of (MedicaidProgram, EligibilityRule) tuples with active rules</returns>
    public async Task<IEnumerable<(MedicaidProgram program, EligibilityRule rule)>> GetProgramsWithRulesAsync(
        string stateCode,
        DateTime effectiveDate)
    {
        if (string.IsNullOrWhiteSpace(stateCode))
            throw new ArgumentException("State code cannot be null or empty", nameof(stateCode));

        stateCode = stateCode.ToUpperInvariant();

        // Get all programs with their active rules for the given date
        var result = await _context.MedicaidPrograms
            .AsNoTracking()
            .Where(p => p.StateCode == stateCode)
            .SelectMany(p => p.Rules
                .Where(r =>
                    // Rule must be active on the effective date
                    r.EffectiveDate <= effectiveDate.Date &&
                    (r.EndDate == null || r.EndDate >= effectiveDate.Date))
                .Select(r => new { program = p, rule = r }))
            .ToListAsync();

        return result.Select(x => (x.program, x.rule)).ToList();
    }

    /// <summary>
    /// Gets all programs for a state with their currently active rules
    /// Uses DateTime.UtcNow as the effective date (typically what you want)
    /// </summary>
    /// <param name="stateCode">Two-letter state code</param>
    /// <returns>List of (MedicaidProgram, EligibilityRule) tuples</returns>
    public async Task<IEnumerable<(MedicaidProgram program, EligibilityRule rule)>> GetProgramsWithCurrentlyActiveRulesAsync(
        string stateCode)
    {
        return await GetProgramsWithRulesAsync(stateCode, DateTime.UtcNow);
    }

    /// <summary>
    /// Gets a count of programs in a state (useful for validation)
    /// </summary>
    /// <param name="stateCode">Two-letter state code</param>
    /// <returns>Number of programs in the state</returns>
    public async Task<int> GetProgramCountForStateAsync(string stateCode)
    {
        if (string.IsNullOrWhiteSpace(stateCode))
            throw new ArgumentException("State code cannot be null or empty", nameof(stateCode));

        stateCode = stateCode.ToUpperInvariant();

        return await _context.MedicaidPrograms
            .AsNoTracking()
            .CountAsync(p => p.StateCode == stateCode);
    }

    /// <summary>
    /// Gets all supported pilot states (returns distinct state codes)
    /// </summary>
    /// <returns>List of state codes that have programs defined</returns>
    public async Task<IEnumerable<string>> GetAllSupportedStatesAsync()
    {
        return await _context.MedicaidPrograms
            .AsNoTracking()
            .Select(p => p.StateCode)
            .Distinct()
            .OrderBy(s => s)
            .ToListAsync();
    }

    /// <summary>
    /// Validates that a state code has programs and active rules
    /// Returns true if state is properly initialized, false otherwise
    /// </summary>
    /// <param name="stateCode">Two-letter state code</param>
    /// <returns>True if state has programs with active rules</returns>
    public async Task<bool> IsStateInitializedAsync(string stateCode)
    {
        if (string.IsNullOrWhiteSpace(stateCode))
            return false;

        stateCode = stateCode.ToUpperInvariant();

        // Check if state has at least one program with an active rule
        var hasActiveRules = await _context.MedicaidPrograms
            .AsNoTracking()
            .Where(p => p.StateCode == stateCode)
            .SelectMany(p => p.Rules)
            .Where(r =>
                r.EffectiveDate <= DateTime.UtcNow.Date &&
                (r.EndDate == null || r.EndDate >= DateTime.UtcNow.Date))
            .AnyAsync();

        return hasActiveRules;
    }
}
