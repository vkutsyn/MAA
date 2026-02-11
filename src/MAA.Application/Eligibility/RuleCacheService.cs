using System.Collections.Concurrent;
using EligibilityDomain = MAA.Domain.Eligibility;
using Microsoft.Extensions.Caching.Memory;

namespace MAA.Application.Eligibility;

public interface IEligibilityCacheService
{
    EligibilityDomain.RuleSetVersion? GetRuleSetVersion(string stateCode, DateTime effectiveDate);
    void SetRuleSetVersion(string stateCode, DateTime effectiveDate, EligibilityDomain.RuleSetVersion ruleSet);
    IReadOnlyList<EligibilityDomain.EligibilityRule>? GetRules(Guid ruleSetVersionId);
    void SetRules(Guid ruleSetVersionId, IReadOnlyList<EligibilityDomain.EligibilityRule> rules);
    EligibilityDomain.FederalPovertyLevel? GetFpl(int year, int householdSize, string? stateCode);
    void SetFpl(int year, int householdSize, string? stateCode, EligibilityDomain.FederalPovertyLevel record);
    void InvalidateState(string stateCode);
    void Clear();
}

public class RuleCacheService : IEligibilityCacheService
{
    private const string DefaultStateKey = "__default__";
    private static readonly TimeSpan DefaultTtl = TimeSpan.FromMinutes(15);

    private readonly IMemoryCache _cache;
    private readonly MemoryCacheEntryOptions _cacheOptions = new()
    {
        AbsoluteExpirationRelativeToNow = DefaultTtl
    };

    private readonly ConcurrentDictionary<string, ConcurrentBag<string>> _stateKeys = new();
    private readonly ConcurrentDictionary<string, byte> _allKeys = new();

    public RuleCacheService(IMemoryCache cache)
    {
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
    }

    public EligibilityDomain.RuleSetVersion? GetRuleSetVersion(string stateCode, DateTime effectiveDate)
    {
        return _cache.TryGetValue(GetRuleSetKey(stateCode, effectiveDate), out EligibilityDomain.RuleSetVersion ruleSet)
            ? ruleSet
            : null;
    }

    public void SetRuleSetVersion(string stateCode, DateTime effectiveDate, EligibilityDomain.RuleSetVersion ruleSet)
    {
        var key = GetRuleSetKey(stateCode, effectiveDate);
        _cache.Set(key, ruleSet, _cacheOptions);
        TrackKey(stateCode, key);
    }

    public IReadOnlyList<EligibilityDomain.EligibilityRule>? GetRules(Guid ruleSetVersionId)
    {
        return _cache.TryGetValue(GetRulesKey(ruleSetVersionId), out IReadOnlyList<EligibilityDomain.EligibilityRule> rules)
            ? rules
            : null;
    }

    public void SetRules(Guid ruleSetVersionId, IReadOnlyList<EligibilityDomain.EligibilityRule> rules)
    {
        var key = GetRulesKey(ruleSetVersionId);
        _cache.Set(key, rules, _cacheOptions);
        TrackKey(null, key);
    }

    public EligibilityDomain.FederalPovertyLevel? GetFpl(int year, int householdSize, string? stateCode)
    {
        return _cache.TryGetValue(GetFplKey(year, householdSize, stateCode), out EligibilityDomain.FederalPovertyLevel record)
            ? record
            : null;
    }

    public void SetFpl(int year, int householdSize, string? stateCode, EligibilityDomain.FederalPovertyLevel record)
    {
        var key = GetFplKey(year, householdSize, stateCode);
        _cache.Set(key, record, _cacheOptions);
        TrackKey(stateCode, key);
    }

    public void InvalidateState(string stateCode)
    {
        if (_stateKeys.TryGetValue(NormalizeStateKey(stateCode), out var keys))
        {
            foreach (var key in keys)
            {
                _cache.Remove(key);
                _allKeys.TryRemove(key, out _);
            }
        }
    }

    public void Clear()
    {
        foreach (var key in _allKeys.Keys)
        {
            _cache.Remove(key);
        }

        _allKeys.Clear();
        _stateKeys.Clear();
    }

    private void TrackKey(string? stateCode, string key)
    {
        _allKeys.TryAdd(key, 0);

        var stateKey = NormalizeStateKey(stateCode);
        var bag = _stateKeys.GetOrAdd(stateKey, _ => new ConcurrentBag<string>());
        bag.Add(key);
    }

    private static string NormalizeStateKey(string? stateCode)
    {
        return string.IsNullOrWhiteSpace(stateCode) ? DefaultStateKey : stateCode.ToUpperInvariant();
    }

    private static string GetRuleSetKey(string stateCode, DateTime effectiveDate)
    {
        return $"eligibility:v2:ruleset:{stateCode.ToUpperInvariant()}:{effectiveDate:yyyy-MM-dd}";
    }

    private static string GetRulesKey(Guid ruleSetVersionId)
    {
        return $"eligibility:v2:rules:{ruleSetVersionId}";
    }

    private static string GetFplKey(int year, int householdSize, string? stateCode)
    {
        var stateKey = NormalizeStateKey(stateCode);
        return $"eligibility:v2:fpl:{stateKey}:{year}:{householdSize}";
    }
}
