using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace MAA.Infrastructure.StateContext;

/// <summary>
/// Service that loads and provides access to ZIP code to state code mappings.
/// Data is loaded from CSV file and cached in memory for fast lookups.
/// </summary>
public class ZipCodeMappingService
{
    private readonly IReadOnlyDictionary<string, string> _zipToStateMapping;
    private readonly ILogger<ZipCodeMappingService> _logger;
    private readonly string _dataFilePath;

    /// <summary>
    /// Constructor that loads ZIP code mappings from CSV file
    /// </summary>
    /// <param name="logger">Logger instance</param>
    /// <param name="dataFilePath">Optional path to CSV file (defaults to embedded resource)</param>
    public ZipCodeMappingService(ILogger<ZipCodeMappingService> logger, string? dataFilePath = null)
    {
        _logger = logger;
        _dataFilePath = dataFilePath ?? GetDefaultDataFilePath();
        _zipToStateMapping = LoadMappings();

        _logger.LogInformation("ZIP code mapping service initialized with {Count} entries", _zipToStateMapping.Count);
    }

    /// <summary>
    /// Gets the read-only dictionary of ZIP to state mappings
    /// </summary>
    public IReadOnlyDictionary<string, string> Mappings => _zipToStateMapping;

    /// <summary>
    /// Gets the state code for a given ZIP code
    /// </summary>
    /// <param name="zipCode">5-digit ZIP code</param>
    /// <returns>State code if found, null otherwise</returns>
    public string? GetStateCode(string zipCode)
    {
        if (string.IsNullOrWhiteSpace(zipCode))
            return null;

        return _zipToStateMapping.TryGetValue(zipCode, out var stateCode) ? stateCode : null;
    }

    /// <summary>
    /// Checks if a ZIP code exists in the mapping
    /// </summary>
    /// <param name="zipCode">5-digit ZIP code</param>
    /// <returns>True if ZIP exists, false otherwise</returns>
    public bool Contains(string zipCode)
    {
        if (string.IsNullOrWhiteSpace(zipCode))
            return false;

        return _zipToStateMapping.ContainsKey(zipCode);
    }

    /// <summary>
    /// Loads ZIP to state mappings from CSV file
    /// </summary>
    /// <returns>Dictionary of ZIP code to state code</returns>
    private IReadOnlyDictionary<string, string> LoadMappings()
    {
        try
        {
            if (!File.Exists(_dataFilePath))
            {
                _logger.LogError("ZIP code data file not found at path: {Path}", _dataFilePath);
                throw new FileNotFoundException($"ZIP code data file not found: {_dataFilePath}");
            }

            var mappings = new ConcurrentDictionary<string, string>(StringComparer.Ordinal);
            var lines = File.ReadAllLines(_dataFilePath);

            // Skip header row
            for (int i = 1; i < lines.Length; i++)
            {
                var line = lines[i];
                if (string.IsNullOrWhiteSpace(line))
                    continue;

                var parts = line.Split(',');
                if (parts.Length >= 2)
                {
                    var zipCode = parts[0].Trim();
                    var stateCode = parts[1].Trim();

                    if (!string.IsNullOrEmpty(zipCode) && !string.IsNullOrEmpty(stateCode))
                    {
                        mappings.TryAdd(zipCode, stateCode);
                    }
                }
            }

            _logger.LogInformation("Loaded {Count} ZIP code mappings from {Path}", mappings.Count, _dataFilePath);
            return mappings;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load ZIP code mappings from {Path}", _dataFilePath);
            throw;
        }
    }

    /// <summary>
    /// Gets the default path to the ZIP code data file
    /// </summary>
    /// <returns>Full path to zip-to-state.csv</returns>
    private static string GetDefaultDataFilePath()
    {
        var executingPath = AppDomain.CurrentDomain.BaseDirectory;
        return Path.Combine(executingPath, "Data", "zip-to-state.csv");
    }
}
