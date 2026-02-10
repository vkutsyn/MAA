using System.Text.Json;

namespace MAA.Application.StateContext.DTOs;

/// <summary>
/// Data transfer object for StateConfiguration entity
/// </summary>
public record StateConfigurationDto
{
    /// <summary>
    /// 2-letter state code (e.g., "CA", "NY")
    /// </summary>
    public required string StateCode { get; init; }

    /// <summary>
    /// Full state name (e.g., "California")
    /// </summary>
    public required string StateName { get; init; }

    /// <summary>
    /// State's Medicaid program name (e.g., "Medi-Cal")
    /// </summary>
    public required string MedicaidProgramName { get; init; }

    /// <summary>
    /// Contact information for state Medicaid program
    /// </summary>
    public ContactInfoDto? ContactInfo { get; init; }

    /// <summary>
    /// Eligibility thresholds specific to this state
    /// </summary>
    public EligibilityThresholdsDto? EligibilityThresholds { get; init; }

    /// <summary>
    /// List of required documents for this state
    /// </summary>
    public List<string>? RequiredDocuments { get; init; }

    /// <summary>
    /// Additional notes about the state's Medicaid program
    /// </summary>
    public string? AdditionalNotes { get; init; }

    /// <summary>
    /// Creates a StateConfigurationDto from a domain entity
    /// </summary>
    public static StateConfigurationDto FromDomain(Domain.StateContext.StateConfiguration config)
    {
        // Deserialize ConfigData JSON to populate nested properties
        var configData = JsonSerializer.Deserialize<ConfigDataDto>(config.ConfigData, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        return new StateConfigurationDto
        {
            StateCode = config.StateCode,
            StateName = config.StateName,
            MedicaidProgramName = config.MedicaidProgramName,
            ContactInfo = configData?.ContactInfo,
            EligibilityThresholds = configData?.EligibilityThresholds,
            RequiredDocuments = configData?.RequiredDocuments,
            AdditionalNotes = configData?.AdditionalNotes
        };
    }

    private record ConfigDataDto
    {
        public ContactInfoDto? ContactInfo { get; init; }
        public EligibilityThresholdsDto? EligibilityThresholds { get; init; }
        public List<string>? RequiredDocuments { get; init; }
        public string? AdditionalNotes { get; init; }
    }
}

/// <summary>
/// Contact information for state Medicaid program
/// </summary>
public record ContactInfoDto
{
    /// <summary>
    /// Phone number for state Medicaid office
    /// </summary>
    public required string Phone { get; init; }

    /// <summary>
    /// Website URL for state Medicaid program
    /// </summary>
    public required string Website { get; init; }

    /// <summary>
    /// Application URL for online applications
    /// </summary>
    public required string ApplicationUrl { get; init; }
}

/// <summary>
/// Eligibility thresholds for state Medicaid program
/// </summary>
public record EligibilityThresholdsDto
{
    /// <summary>
    /// Federal Poverty Level percentages by category
    /// </summary>
    public required FplPercentagesDto FplPercentages { get; init; }

    /// <summary>
    /// Asset limits for eligibility
    /// </summary>
    public required AssetLimitsDto AssetLimits { get; init; }
}

/// <summary>
/// Federal Poverty Level percentages
/// </summary>
public record FplPercentagesDto
{
    /// <summary>
    /// FPL percentage for adults (e.g., 138 = 138% FPL)
    /// </summary>
    public required int Adults { get; init; }

    /// <summary>
    /// FPL percentage for children
    /// </summary>
    public required int Children { get; init; }

    /// <summary>
    /// FPL percentage for pregnant individuals
    /// </summary>
    public required int Pregnant { get; init; }
}

/// <summary>
/// Asset limits for Medicaid eligibility
/// </summary>
public record AssetLimitsDto
{
    /// <summary>
    /// Asset limit for individual applicants (in dollars)
    /// </summary>
    public required int Individual { get; init; }

    /// <summary>
    /// Asset limit for couples (in dollars)
    /// </summary>
    public required int Couple { get; init; }
}
