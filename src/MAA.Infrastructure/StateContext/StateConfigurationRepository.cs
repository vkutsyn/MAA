using MAA.Application.StateContext;
using MAA.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace MAA.Infrastructure.StateContext;

/// <summary>
/// Repository implementation for StateConfiguration entity with caching
/// </summary>
public class StateConfigurationRepository : IStateConfigurationRepository
{
    private readonly SessionContext _context;
    private readonly IMemoryCache _cache;
    private readonly ILogger<StateConfigurationRepository> _logger;
    private const string CacheKeyPrefix = "state-config:";
    private const string AllStatesCacheKey = "state-config:all";
    private static readonly TimeSpan CacheDuration = TimeSpan.FromHours(24);

    public StateConfigurationRepository(
        SessionContext context,
        IMemoryCache cache,
        ILogger<StateConfigurationRepository> logger)
    {
        _context = context;
        _cache = cache;
        _logger = logger;
    }

    /// <summary>
    /// Gets an active state configuration by state code with caching
    /// </summary>
    public async Task<Domain.StateContext.StateConfiguration?> GetActiveByStateCodeAsync(string stateCode)
    {
        var cacheKey = $"{CacheKeyPrefix}{stateCode}";

        // Try to get from cache
        if (_cache.TryGetValue<Domain.StateContext.StateConfiguration>(cacheKey, out var cachedConfig))
        {
            _logger.LogDebug("Retrieved state configuration for {StateCode} from cache", stateCode);
            return cachedConfig;
        }

        // Not in cache, get from database
        var config = await _context.StateConfigurations
            .FirstOrDefaultAsync(sc => sc.StateCode == stateCode && sc.IsActive);

        if (config != null)
        {
            // Store in cache with 24-hour expiration
            _cache.Set(cacheKey, config, CacheDuration);
            _logger.LogDebug("Stored state configuration for {StateCode} in cache", stateCode);
        }

        return config;
    }

    /// <summary>
    /// Gets all active state configurations with caching
    /// </summary>
    public async Task<List<Domain.StateContext.StateConfiguration>> GetAllActiveAsync()
    {
        // Try to get from cache
        if (_cache.TryGetValue<List<Domain.StateContext.StateConfiguration>>(AllStatesCacheKey, out var cachedConfigs))
        {
            _logger.LogDebug("Retrieved all state configurations from cache");
            return cachedConfigs!;
        }

        // Not in cache, get from database
        var configs = await _context.StateConfigurations
            .Where(sc => sc.IsActive)
            .OrderBy(sc => sc.StateName)
            .ToListAsync();

        // Store in cache with 24-hour expiration
        _cache.Set(AllStatesCacheKey, configs, CacheDuration);
        _logger.LogDebug("Stored all state configurations in cache ({Count} states)", configs.Count);

        return configs;
    }

    /// <summary>
    /// Checks if a state configuration exists
    /// </summary>
    public async Task<bool> ExistsAsync(string stateCode)
    {
        // Check cache first
        var cacheKey = $"{CacheKeyPrefix}{stateCode}";
        if (_cache.TryGetValue<Domain.StateContext.StateConfiguration>(cacheKey, out _))
        {
            return true;
        }

        // Check database
        return await _context.StateConfigurations
            .AnyAsync(sc => sc.StateCode == stateCode && sc.IsActive);
    }
}
