using MAA.Domain.Rules;
using MAA.Infrastructure.DataAccess;
using MAA.Application.Eligibility.Caching;
using MAA.Application.Eligibility.Repositories;
using System.Collections.Concurrent;

namespace MAA.Infrastructure.Caching;

/// <summary>
/// In-Memory Cache for Eligibility Rules
/// 
/// Phase 3 Implementation: T025
/// 
/// Key Responsibilities:
/// - Cache active rules with configurable TTL (default 1 hour)
/// - Cache key strategy: (state_code, program_id) for efficient lookups
/// - Thread-safe concurrent access via ConcurrentDictionary
/// - Manual invalidation support for rule updates
/// - Performance optimization: Reduces database hits for rule lookups
/// 
/// Performance Target:
/// - Cache hit reduces database latency from ~50ms to <1ms
/// - Supports ≤2 second p95 for evaluation with 1,000 concurrent users
/// - Expected hit rate: 90%+ (rules change rarely, users change frequently)
/// 
/// Memory Considerations:
/// - 30 pilot programs × 5 states = 150 max rules
/// - Per rule ~5KB (rule logic JSON), so ~750KB max memory usage
/// - Negligible in modern deployment (typical server has GBs available)
/// 
/// Invalidation Strategy:
/// - Automatic TTL expiration (1 hour default)
/// - Manual invalidation via InvalidateRule() or InvalidateProgram()
/// - Called by handler after rule update operations
/// 
/// Thread Safety:
/// - Uses ConcurrentDictionary for all operations
/// - No lock statements needed, safe for concurrent access
/// </summary>
/// <summary>
/// Concurrent in-memory cache for rules
/// Uses state_code:program_id as cache key
/// </summary>
public class RuleCacheService : IRuleCacheService
{
    private readonly ConcurrentDictionary<string, CacheEntry> _cache;
    private readonly TimeSpan _defaultTtl;
    private long _cacheHits;
    private long _cacheMisses;

    /// <summary>
    /// Default TTL: 1 hour (rules rarely change during user session)
    /// </summary>
    private const int DefaultTtlMinutes = 60;

    public RuleCacheService(TimeSpan? customTtl = null)
    {
        _cache = new ConcurrentDictionary<string, CacheEntry>();
        _defaultTtl = customTtl ?? TimeSpan.FromMinutes(DefaultTtlMinutes);
        _cacheHits = 0;
        _cacheMisses = 0;
    }

    /// <summary>
    /// Gets a cached rule with automatic TTL checking
    /// Updates hit/miss statistics
    /// </summary>
    public EligibilityRule? GetCachedRule(string stateCode, Guid programId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(stateCode, nameof(stateCode));
        ArgumentException.ThrowIfNullOrEmpty(programId.ToString(), nameof(programId));

        var cacheKey = BuildCacheKey(stateCode, programId);

        if (_cache.TryGetValue(cacheKey, out var entry))
        {
            if (!entry.IsExpired)
            {
                Interlocked.Increment(ref _cacheHits);
                return entry.Rule;
            }

            // Entry expired, remove it
            _cache.TryRemove(cacheKey, out _);
        }

        Interlocked.Increment(ref _cacheMisses);
        return null;
    }

    /// <summary>
    /// Adds or updates a rule in cache
    /// Sets expiration based on default TTL
    /// </summary>
    public void SetCachedRule(string stateCode, Guid programId, EligibilityRule rule)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(stateCode, nameof(stateCode));
        ArgumentException.ThrowIfNullOrEmpty(programId.ToString(), nameof(programId));
        ArgumentNullException.ThrowIfNull(rule, nameof(rule));

        var cacheKey = BuildCacheKey(stateCode, programId);
        var expiration = DateTime.UtcNow.Add(_defaultTtl);

        var entry = new CacheEntry
        {
            Rule = rule,
            CachedAt = DateTime.UtcNow,
            ExpiresAt = expiration
        };

        _cache.AddOrUpdate(cacheKey, entry, (key, existing) => entry);
    }

    /// <summary>
    /// Gets all cached rules for a state
    /// Filters out expired entries
    /// </summary>
    public IEnumerable<EligibilityRule> GetCachedRulesByState(string stateCode)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(stateCode, nameof(stateCode));

        var statePrefix = stateCode.ToUpperInvariant() + ":";

        return _cache
            .Where(kvp => kvp.Key.StartsWith(statePrefix))
            .Where(kvp => !kvp.Value.IsExpired)
            .Select(kvp => kvp.Value.Rule)
            .ToList();
    }

    /// <summary>
    /// Removes a specific cache entry (invalidation)
    /// Called after rule updates
    /// </summary>
    public void InvalidateRule(string stateCode, Guid programId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(stateCode, nameof(stateCode));
        ArgumentException.ThrowIfNullOrEmpty(programId.ToString(), nameof(programId));

        var cacheKey = BuildCacheKey(stateCode, programId);
        _cache.TryRemove(cacheKey, out _);
    }

    /// <summary>
    /// Removes all versions of a program from cache
    /// </summary>
    public void InvalidateProgram(Guid programId)
    {
        ArgumentException.ThrowIfNullOrEmpty(programId.ToString(), nameof(programId));

        var programIdStr = programId.ToString();
        var keysToRemove = _cache
            .Where(kvp => kvp.Key.EndsWith(":" + programIdStr))
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var key in keysToRemove)
        {
            _cache.TryRemove(key, out _);
        }
    }

    /// <summary>
    /// Clears all cache entries
    /// </summary>
    public void InvalidateAll()
    {
        _cache.Clear();
        _cacheHits = 0;
        _cacheMisses = 0;
    }

    /// <summary>
    /// Gets cache statistics for monitoring
    /// </summary>
    public CacheStatistics GetStatistics()
    {
        var now = DateTime.UtcNow;
        var total = _cache.Count;
        var valid = _cache.Values.Count(e => !e.IsExpired);
        var expired = total - valid;

        var hitRate = _cacheHits + _cacheMisses == 0
            ? 0
            : (double)_cacheHits / (_cacheHits + _cacheMisses);

        // Approximate memory: key length + entry size
        var approxMemory = _cache.Sum(kvp =>
            System.Text.Encoding.UTF8.GetByteCount(kvp.Key) + 512); // 512 = rough size of CacheEntry + Rule

        return new CacheStatistics
        {
            TotalEntries = total,
            ValidEntries = valid,
            ExpiredEntries = expired,
            LastRefreshed = DateTime.UtcNow,
            ApproximateMemoryBytes = approxMemory,
            HitRate = hitRate
        };
    }

    /// <summary>
    /// Refreshes cache from database
    /// Can be called periodically or after rule updates
    /// </summary>
    public async Task RefreshCacheAsync(IRuleRepository repository, string? stateCode = null)
    {
        ArgumentNullException.ThrowIfNull(repository, nameof(repository));

        IEnumerable<EligibilityRule> rules;

        if (string.IsNullOrWhiteSpace(stateCode))
        {
            // Refresh not implemented for "refresh everything"
            // Instead, populate cache on-demand via GetCachedRule
            return;
        }

        // Refresh specific state
        rules = await repository.GetRulesByStateAsync(stateCode!);

        foreach (var rule in rules)
        {
            SetCachedRule(stateCode, rule.ProgramId, rule);
        }
    }

    /// <summary>
    /// Builds cache key from state and program ID
    /// Format: "IL:550e8400-e29b-41d4-a716-446655440000"
    /// </summary>
    private string BuildCacheKey(string stateCode, Guid programId)
    {
        return $"{stateCode.ToUpperInvariant()}:{programId}";
    }

    /// <summary>
    /// Internal cache entry wrapper
    /// Holds rule data with expiration tracking
    /// </summary>
    private class CacheEntry
    {
        /// <summary>
        /// The cached rule
        /// </summary>
        required public EligibilityRule Rule { get; set; }

        /// <summary>
        /// Timestamp when entry was cached
        /// </summary>
        required public DateTime CachedAt { get; set; }

        /// <summary>
        /// Timestamp when entry expires
        /// </summary>
        required public DateTime ExpiresAt { get; set; }

        /// <summary>
        /// Computed property: true if entry has expired
        /// </summary>
        public bool IsExpired => DateTime.UtcNow > ExpiresAt;
    }
}
