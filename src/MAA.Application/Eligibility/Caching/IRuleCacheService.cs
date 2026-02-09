using MAA.Domain.Rules;
using MAA.Application.Eligibility.Repositories;

namespace MAA.Application.Eligibility.Caching;

/// <summary>
/// Cache service interface for caching eligibility rules
/// Defined in Application layer (dependency inversion)
/// Implemented in Infrastructure layer
/// </summary>
public interface IRuleCacheService
{
    EligibilityRule? GetCachedRule(string stateCode, Guid programId);
    void SetCachedRule(string stateCode, Guid programId, EligibilityRule rule);
    IEnumerable<EligibilityRule> GetCachedRulesByState(string stateCode);
    void InvalidateRule(string stateCode, Guid programId);
    void InvalidateProgram(Guid programId);
    void InvalidateState(string stateCode);
    void InvalidateAll();
    Task RefreshCacheAsync(IRuleRepository repository, string? stateCode = null);
}

/// <summary>
/// Cache statistics for monitoring
/// </summary>
public class CacheStatistics
{
    public int TotalEntries { get; set; }
    public int ValidEntries { get; set; }
    public int ExpiredEntries { get; set; }
    public DateTime LastRefreshed { get; set; }
    public long ApproximateMemoryBytes { get; set; }
    public double HitRate { get; set; }
}
