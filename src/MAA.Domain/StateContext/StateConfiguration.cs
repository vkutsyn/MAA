namespace MAA.Domain.StateContext;

/// <summary>
/// Represents state-specific Medicaid program configuration and eligibility metadata.
/// This entity stores immutable configuration data for each state's Medicaid program.
/// </summary>
public class StateConfiguration
{
    /// <summary>
    /// Primary key: 2-letter state abbreviation (e.g., "CA", "NY")
    /// </summary>
    public string StateCode { get; private set; } = string.Empty;

    /// <summary>
    /// Full state name (e.g., "California", "New York")
    /// </summary>
    public string StateName { get; private set; } = string.Empty;

    /// <summary>
    /// State's Medicaid program name (e.g., "Medi-Cal", "MassHealth")
    /// </summary>
    public string MedicaidProgramName { get; private set; } = string.Empty;

    /// <summary>
    /// JSONB: State-specific config (thresholds, documents, contact info, etc.)
    /// Stored as JSON string for flexibility
    /// </summary>
    public string ConfigData { get; private set; } = string.Empty;

    /// <summary>
    /// Date this config version became effective
    /// </summary>
    public DateTime EffectiveDate { get; private set; }

    /// <summary>
    /// Version number (increments on updates)
    /// </summary>
    public int Version { get; private set; }

    /// <summary>
    /// True if this is the active config for the state
    /// </summary>
    public bool IsActive { get; private set; }

    /// <summary>
    /// Auto-set on creation (UTC)
    /// </summary>
    public DateTime CreatedAt { get; private set; }

    /// <summary>
    /// Auto-set on update (UTC)
    /// </summary>
    public DateTime? UpdatedAt { get; private set; }

    // Navigation properties
    public ICollection<StateContext> StateContexts { get; private set; } = new List<StateContext>();

    /// <summary>
    /// Private constructor for EF Core
    /// </summary>
    private StateConfiguration() { }

    /// <summary>
    /// Factory method to create a new StateConfiguration (testable, no I/O)
    /// </summary>
    /// <param name="stateCode">2-letter state code</param>
    /// <param name="stateName">Full state name</param>
    /// <param name="medicaidProgramName">State's Medicaid program name</param>
    /// <param name="configData">JSON configuration data</param>
    /// <param name="effectiveDate">Date this config becomes effective</param>
    /// <param name="version">Version number</param>
    /// <param name="isActive">Whether this is the active version</param>
    /// <returns>A new StateConfiguration instance</returns>
    /// <exception cref="ArgumentException">Thrown when validation fails</exception>
    public static StateConfiguration Create(
        string stateCode,
        string stateName,
        string medicaidProgramName,
        string configData,
        DateTime effectiveDate,
        int version,
        bool isActive)
    {
        // Validation
        if (string.IsNullOrWhiteSpace(stateCode))
            throw new ArgumentException("StateCode is required", nameof(stateCode));

        if (stateCode.Length != 2 || !stateCode.All(char.IsUpper))
            throw new ArgumentException("StateCode must be 2 uppercase letters", nameof(stateCode));

        if (string.IsNullOrWhiteSpace(stateName))
            throw new ArgumentException("StateName is required", nameof(stateName));

        if (stateName.Length > 50)
            throw new ArgumentException("StateName cannot exceed 50 characters", nameof(stateName));

        if (string.IsNullOrWhiteSpace(medicaidProgramName))
            throw new ArgumentException("MedicaidProgramName is required", nameof(medicaidProgramName));

        if (medicaidProgramName.Length > 100)
            throw new ArgumentException("MedicaidProgramName cannot exceed 100 characters", nameof(medicaidProgramName));

        if (string.IsNullOrWhiteSpace(configData))
            throw new ArgumentException("ConfigData is required", nameof(configData));

        if (version <= 0)
            throw new ArgumentException("Version must be greater than 0", nameof(version));

        if (effectiveDate > DateTime.UtcNow.AddYears(1))
            throw new ArgumentException("EffectiveDate cannot be more than 1 year in the future", nameof(effectiveDate));

        return new StateConfiguration
        {
            StateCode = stateCode,
            StateName = stateName,
            MedicaidProgramName = medicaidProgramName,
            ConfigData = configData,
            EffectiveDate = effectiveDate,
            Version = version,
            IsActive = isActive,
            CreatedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Deactivate this configuration version
    /// </summary>
    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }
}
