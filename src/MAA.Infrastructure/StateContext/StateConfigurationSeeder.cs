using System.Text.Json;
using MAA.Infrastructure.Data;
using Microsoft.Extensions.Logging;

namespace MAA.Infrastructure.StateContext;

/// <summary>
/// Service that seeds state configuration data into the database.
/// Loads state configurations from JSON file and inserts into database.
/// </summary>
public class StateConfigurationSeeder
{
    private readonly SessionContext _context;
    private readonly ILogger<StateConfigurationSeeder> _logger;
    private readonly string _dataFilePath;

    /// <summary>
    /// Constructor with dependency injection
    /// </summary>
    /// <param name="context">Database context</param>
    /// <param name="logger">Logger instance</param>
    /// <param name="dataFilePath">Optional path to JSON file (defaults to embedded resource)</param>
    public StateConfigurationSeeder(
        SessionContext context,
        ILogger<StateConfigurationSeeder> logger,
        string? dataFilePath = null)
    {
        _context = context;
        _logger = logger;
        _dataFilePath = dataFilePath ?? GetDefaultDataFilePath();
    }

    /// <summary>
    /// Seeds state configurations into the database if they don't already exist
    /// </summary>
    /// <returns>Number of configurations seeded</returns>
    public async Task<int> SeedAsync()
    {
        try
        {
            // Check if configurations already exist
            if (_context.StateConfigurations.Any())
            {
                _logger.LogInformation("State configurations already exist, skipping seed");
                return 0;
            }

            if (!File.Exists(_dataFilePath))
            {
                _logger.LogError("State configuration data file not found at path: {Path}", _dataFilePath);
                throw new FileNotFoundException($"State configuration data file not found: {_dataFilePath}");
            }

            var jsonContent = await File.ReadAllTextAsync(_dataFilePath);
            var configDtos = JsonSerializer.Deserialize<List<StateConfigurationDto>>(jsonContent, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (configDtos == null || !configDtos.Any())
            {
                _logger.LogWarning("No state configurations found in {Path}", _dataFilePath);
                return 0;
            }

            var configurations = new List<Domain.StateContext.StateConfiguration>();
            foreach (var dto in configDtos)
            {
                var configData = JsonSerializer.Serialize(dto);
                var configuration = Domain.StateContext.StateConfiguration.Create(
                    dto.StateCode,
                    dto.StateName,
                    dto.MedicaidProgramName,
                    configData,
                    DateTime.Parse(dto.EffectiveDate),
                    dto.Version,
                    dto.IsActive
                );
                configurations.Add(configuration);
            }

            _context.StateConfigurations.AddRange(configurations);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Seeded {Count} state configurations", configurations.Count);
            return configurations.Count;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to seed state configurations from {Path}", _dataFilePath);
            throw;
        }
    }

    /// <summary>
    /// Gets the default path to the state configuration data file
    /// </summary>
    /// <returns>Full path to state-configs.json</returns>
    private static string GetDefaultDataFilePath()
    {
        var executingPath = AppDomain.CurrentDomain.BaseDirectory;
        return Path.Combine(executingPath, "Data", "state-configs.json");
    }

    /// <summary>
    /// DTO for deserializing state configuration JSON
    /// </summary>
    private class StateConfigurationDto
    {
        public string StateCode { get; set; } = string.Empty;
        public string StateName { get; set; } = string.Empty;
        public string MedicaidProgramName { get; set; } = string.Empty;
        public string EffectiveDate { get; set; } = string.Empty;
        public int Version { get; set; }
        public bool IsActive { get; set; }
        public ContactInfoDto? ContactInfo { get; set; }
        public EligibilityThresholdsDto? EligibilityThresholds { get; set; }
        public List<string>? RequiredDocuments { get; set; }
        public string? AdditionalNotes { get; set; }
    }

    private class ContactInfoDto
    {
        public string? Phone { get; set; }
        public string? Website { get; set; }
        public string? ApplicationUrl { get; set; }
    }

    private class EligibilityThresholdsDto
    {
        public FplPercentagesDto? FplPercentages { get; set; }
        public AssetLimitsDto? AssetLimits { get; set; }
    }

    private class FplPercentagesDto
    {
        public int Adults { get; set; }
        public int Children { get; set; }
        public int Pregnant { get; set; }
    }

    private class AssetLimitsDto
    {
        public int Individual { get; set; }
        public int Couple { get; set; }
    }
}
