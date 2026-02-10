using System.Text.Json;
using MAA.Application.Interfaces;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MAA.Infrastructure.Caching;

/// <summary>
/// Distributed cache for question definitions keyed by state/program.
/// </summary>
public class QuestionDefinitionsCache : IQuestionDefinitionsCache
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly IDistributedCache _cache;
    private readonly QuestionDefinitionsCacheOptions _options;
    private readonly ILogger<QuestionDefinitionsCache> _logger;

    public QuestionDefinitionsCache(
        IDistributedCache cache,
        IOptions<QuestionDefinitionsCacheOptions> options,
        ILogger<QuestionDefinitionsCache> logger)
    {
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<QuestionDefinitionsCacheEntry?> GetAsync(
        string stateCode,
        string programCode,
        CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled)
            return null;

        var cacheKey = BuildCacheKey(stateCode, programCode);
        if (cacheKey == null)
            return null;

        var cachedValue = await _cache.GetStringAsync(cacheKey, cancellationToken);
        if (string.IsNullOrWhiteSpace(cachedValue))
            return null;

        try
        {
            return JsonSerializer.Deserialize<QuestionDefinitionsCacheEntry>(cachedValue, SerializerOptions);
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Failed to deserialize cached question definitions for {CacheKey}", cacheKey);
            return null;
        }
    }

    public async Task SetAsync(
        string stateCode,
        string programCode,
        QuestionDefinitionsCacheEntry entry,
        CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled)
            return;

        if (entry == null)
            throw new ArgumentNullException(nameof(entry));

        var cacheKey = BuildCacheKey(stateCode, programCode);
        if (cacheKey == null)
            return;

        var ttlHours = _options.TtlHours <= 0 ? 24 : _options.TtlHours;
        var cacheOptions = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(ttlHours)
        };

        var payload = JsonSerializer.Serialize(entry, SerializerOptions);
        await _cache.SetStringAsync(cacheKey, payload, cacheOptions, cancellationToken);
    }

    public Task InvalidateAsync(
        string stateCode,
        string programCode,
        CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled)
            return Task.CompletedTask;

        var cacheKey = BuildCacheKey(stateCode, programCode);
        if (cacheKey == null)
            return Task.CompletedTask;

        return _cache.RemoveAsync(cacheKey, cancellationToken);
    }

    private static string? BuildCacheKey(string stateCode, string programCode)
    {
        if (string.IsNullOrWhiteSpace(stateCode) || string.IsNullOrWhiteSpace(programCode))
            return null;

        return $"question-defs:{stateCode.ToUpperInvariant()}:{programCode.ToUpperInvariant()}";
    }
}
