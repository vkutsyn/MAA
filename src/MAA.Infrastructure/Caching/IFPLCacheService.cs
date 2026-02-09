using MAA.Domain.Rules;
using System;
using System.Collections.Generic;

namespace MAA.Infrastructure.Caching;

/// <summary>
/// Interface for caching Federal Poverty Level (FPL) table data
/// 
/// Phase 7 Implementation: T061 (interface definition)
/// 
/// Responsibilities:
/// - Cache FPL records by year with automatic expiration
/// - Provide get/set operations for cache-aside pattern
/// - Support cache invalidation
/// - Track cache performance (optional stats)
/// </summary>
public interface IFPLCacheService
{
    /// <summary>
    /// Gets cached FPL records for a year if present and not expired.
    /// </summary>
    List<FederalPovertyLevel>? GetCachedFplsByYear(int year);

    /// <summary>
    /// Caches FPL records for a year until January 1st of the following year.
    /// </summary>
    void SetCachedFplsByYear(int year, IEnumerable<FederalPovertyLevel> records);

    /// <summary>
    /// Invalidates (removes) cache for a specific year.
    /// </summary>
    void InvalidateYear(int year);

    /// <summary>
    /// Invalidates cache for multiple years.
    /// </summary>
    void InvalidateYears(params int[] years);

    /// <summary>
    /// Gets cache statistics for monitoring.
    /// </summary>
    (int cachedYearsCount, int totalRecordsCached) GetCacheStats();

    /// <summary>
    /// Clears all cached FPL data.
    /// </summary>
    void ClearAll();

    /// <summary>
    /// Gets estimated memory usage of cached data in bytes.
    /// </summary>
    long GetEstimatedMemoryUsageBytes();
}

