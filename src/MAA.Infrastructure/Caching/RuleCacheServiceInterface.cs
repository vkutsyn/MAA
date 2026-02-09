namespace MAA.Infrastructure.Caching;

/// <summary>
/// Caching service for rules engine
/// Provides in-memory caching of rules and FPL data with invalidation support
/// 
/// Cache strategy:
/// - Rules: Cached for 1 hour with manual invalidation on update
/// - FPL: Cached for 1 year (updated annually)
/// - Key format: "rules:{state_code}:{program_id}" or "fpl:{year}:{household_size}"
/// 
/// Phase 2 Implementation: Not yet implemented
/// Phase 3 Implementation: T025 RuleCacheService
/// </summary>
public interface IRuleCacheService
{
    // Placeholder for Phase 2/3 implementation
    // Methods like: GetRuleFromCache, InvalidateRuleCache, SetFplCache, etc.
}
