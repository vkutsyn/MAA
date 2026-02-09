using MAA.Application.Eligibility.Caching;
using MAA.Application.Eligibility.Repositories;
using MAA.Domain.Rules;
using MAA.Domain.Rules.Exceptions;

namespace MAA.Application.Eligibility.Services;

/// <summary>
/// Service for loading and caching Medicaid rules for specific states
/// 
/// Phase 5 Implementation: T044
/// 
/// Purpose:
/// - Provides state-scoped rule loading for multi-program evaluation
/// - Coordinates with IRuleCacheService for state-specific caching
/// - Validates state codes and handles missing data gracefully
/// - Supports efficient rule lookup by state
/// 
/// Key Features:
/// - Cache-first approach: Check cache before hitting database
/// - State-specific caching: Rules keyed by state_code (e.g., "IL", "CA", "NY", "TX", "FL")
/// - Lazy loading: Populates cache on first access
/// - Cache invalidation: Supports per-state cache clearing
/// 
/// Performance Target:
/// - Cache hit: <5ms (in-memory lookup)
/// - Cache miss: <100ms (database query + cache population)
/// - FPL lookups: <10ms
/// 
/// Error Handling:
/// - Invalid state codes throw ArgumentException with helpful message
/// - Missing rules throw EligibilityEvaluationException
/// - Database errors propagate as-is (handled by caller)
/// 
/// Usage:
///   var loader = new StateRuleLoader(repository, cacheService);
///   var rules = await loader.LoadRulesForStateAsync("IL");
///   // Returns: List<EligibilityRule> for all IL programs
/// 
/// Integration:
/// - Used by ProgramMatchingHandler (T032) to fetch programs for multi-program evaluation
/// - Supports "Same user in IL vs TX → different results" requirement (Phase 5)
/// - Extends cache management (T046)
/// 
/// Reference:
/// - Spec: specs/002-rules-engine/spec.md US3
/// - Phase 5 Task: T044 State-Specific Rule Evaluation
/// </summary>
public interface IStateRuleLoader
{
    /// <summary>
    /// Loads all active rules for a specific state
    /// Uses cache where available, fetches from database on miss
    /// </summary>
    /// <param name="stateCode">Two-letter state code (IL, CA, NY, TX, FL, etc.)</param>
    /// <returns>List of active EligibilityRule objects for the state (never null)</returns>
    /// <exception cref="ArgumentException">If state code is invalid or unsupported</exception>
    /// <exception cref="EligibilityEvaluationException">If no rules found for valid state</exception>
    Task<List<EligibilityRule>> LoadRulesForStateAsync(string stateCode);

    /// <summary>
    /// Gets all programs with their active rules for a state (combined entity)
    /// Useful for multi-program evaluation workflows
    /// </summary>
    /// <param name="stateCode">Two-letter state code</param>
    /// <returns>List of (MedicaidProgram, EligibilityRule) tuples</returns>
    Task<List<(MedicaidProgram program, EligibilityRule rule)>> LoadProgramsWithRulesForStateAsync(string stateCode);

    /// <summary>
    /// Invalidates cached rules for a specific state
    /// Called when rules are updated or new rules are added
    /// </summary>
    /// <param name="stateCode">Two-letter state code</param>
    void InvalidateCacheForState(string stateCode);

    /// <summary>
    /// Refreshes cache for all pilot states from database
    /// Useful for initialization or periodic cache refresh
    /// </summary>
    Task RefreshAllStatesCacheAsync();
}

/// <summary>
/// Concrete implementation of IStateRuleLoader
/// </summary>
public class StateRuleLoader : IStateRuleLoader
{
    private readonly IRuleRepository _ruleRepository;
    private readonly IRuleCacheService _cacheService;

    // Supported pilot states per Phase 5 requirements
    private static readonly string[] SupportedStates = new[] { "IL", "CA", "NY", "TX", "FL" };

    public StateRuleLoader(
        IRuleRepository ruleRepository,
        IRuleCacheService cacheService)
    {
        _ruleRepository = ruleRepository ?? throw new ArgumentNullException(nameof(ruleRepository));
        _cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));
    }

    /// <summary>
    /// Loads all active rules for a specific state with caching support
    /// </summary>
    public async Task<List<EligibilityRule>> LoadRulesForStateAsync(string stateCode)
    {
        if (string.IsNullOrWhiteSpace(stateCode))
            throw new ArgumentException("State code cannot be null or empty", nameof(stateCode));

        stateCode = stateCode.ToUpperInvariant();

        // Validate state code against supported pilot states
        if (!SupportedStates.Contains(stateCode))
        {
            throw new ArgumentException(
                $"State code '{stateCode}' is not supported. Supported states: {string.Join(", ", SupportedStates)}",
                nameof(stateCode));
        }

        // Check cache first
        var cachedRules = _cacheService.GetCachedRulesByState(stateCode);
        if (cachedRules?.Any() == true)
        {
            return cachedRules.ToList();
        }

        // Cache miss: fetch from repository
        var rulesFromDb = (await _ruleRepository.GetRulesByStateAsync(stateCode)).ToList();

        if (!rulesFromDb.Any())
        {
            throw new EligibilityEvaluationException(
                $"No active rules found for state '{stateCode}'. " +
                $"State rules may not be initialized. Please contact support.");
        }

        // Cache each rule individually for consistency with IRuleCacheService expectations
        foreach (var rule in rulesFromDb)
        {
            _cacheService.SetCachedRule(stateCode, rule.ProgramId, rule);
        }

        return rulesFromDb;
    }

    /// <summary>
    /// Loads programs with their active rules for a state
    /// Combines program and rule data for multi-program evaluation workflows
    /// </summary>
    public async Task<List<(MedicaidProgram program, EligibilityRule rule)>> LoadProgramsWithRulesForStateAsync(string stateCode)
    {
        if (string.IsNullOrWhiteSpace(stateCode))
            throw new ArgumentException("State code cannot be null or empty", nameof(stateCode));

        stateCode = stateCode.ToUpperInvariant();

        // Validate state code
        if (!SupportedStates.Contains(stateCode))
        {
            throw new ArgumentException(
                $"State code '{stateCode}' is not supported. Supported states: {string.Join(", ", SupportedStates)}",
                nameof(stateCode));
        }

        // Use repository method that returns combined (program, rule) tuples
        var programsWithRules = (await _ruleRepository.GetProgramsWithActiveRulesByStateAsync(stateCode)).ToList();

        if (!programsWithRules.Any())
        {
            throw new EligibilityEvaluationException(
                $"No programs with active rules found for state '{stateCode}'. " +
                $"State may not be initialized. Please contact support.");
        }

        // Cache the rules individually
        foreach (var (program, rule) in programsWithRules)
        {
            _cacheService.SetCachedRule(stateCode, rule.ProgramId, rule);
        }

        return programsWithRules;
    }

    /// <summary>
    /// Invalidates cached rules for a specific state
    /// Phase 5 Enhancement: T046 - Uses new InvalidateState method
    /// </summary>
    public void InvalidateCacheForState(string stateCode)
    {
        if (string.IsNullOrWhiteSpace(stateCode))
            throw new ArgumentException("State code cannot be null or empty", nameof(stateCode));

        stateCode = stateCode.ToUpperInvariant();
        _cacheService.InvalidateState(stateCode);
    }

    /// <summary>
    /// Refreshes cache for all pilot states
    /// </summary>
    public async Task RefreshAllStatesCacheAsync()
    {
        foreach (var state in SupportedStates)
        {
            try
            {
                await LoadRulesForStateAsync(state);
            }
            catch (EligibilityEvaluationException ex)
            {
                // Log but don't fail entire refresh if one state has no rules
                // This handles cases where pilot states are added incrementally
                System.Diagnostics.Debug.WriteLine($"Failed to refresh cache for state {state}: {ex.Message}");
            }
        }
    }
}
