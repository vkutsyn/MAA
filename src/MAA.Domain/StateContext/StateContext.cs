using MAA.Domain.Sessions;

namespace MAA.Domain.StateContext;

/// <summary>
/// Represents the established Medicaid jurisdiction context for a user's application session.
/// This entity captures the state where the user will apply for Medicaid coverage.
/// </summary>
public class StateContext
{
    /// <summary>
    /// Primary key (auto-generated UUID)
    /// </summary>
    public Guid Id { get; private set; }

    /// <summary>
    /// Foreign key to Session entity
    /// </summary>
    public Guid SessionId { get; private set; }

    /// <summary>
    /// 2-letter state abbreviation (e.g., "CA", "NY")
    /// </summary>
    public string StateCode { get; private set; } = string.Empty;

    /// <summary>
    /// Full state name (e.g., "California", "New York")
    /// </summary>
    public string StateName { get; private set; } = string.Empty;

    /// <summary>
    /// User-entered 5-digit ZIP code
    /// </summary>
    public string ZipCode { get; private set; } = string.Empty;

    /// <summary>
    /// True if user manually selected state (vs auto-detected from ZIP)
    /// </summary>
    public bool IsManualOverride { get; private set; }

    /// <summary>
    /// UTC timestamp when state context was established
    /// </summary>
    public DateTime EffectiveDate { get; private set; }

    /// <summary>
    /// Auto-set on creation (UTC)
    /// </summary>
    public DateTime CreatedAt { get; private set; }

    /// <summary>
    /// Auto-set on update (UTC)
    /// </summary>
    public DateTime? UpdatedAt { get; private set; }

    // Navigation properties
    public Session Session { get; private set; } = null!;
    public StateConfiguration StateConfiguration { get; private set; } = null!;

    /// <summary>
    /// Private constructor for EF Core
    /// </summary>
    private StateContext() { }

    /// <summary>
    /// Factory method to create a new StateContext (testable, no I/O)
    /// </summary>
    /// <param name="sessionId">The session ID this context belongs to</param>
    /// <param name="stateCode">2-letter state code</param>
    /// <param name="stateName">Full state name</param>
    /// <param name="zipCode">5-digit ZIP code</param>
    /// <param name="isManualOverride">Whether the state was manually selected</param>
    /// <returns>A new StateContext instance</returns>
    /// <exception cref="ArgumentException">Thrown when validation fails</exception>
    public static StateContext Create(
        Guid sessionId,
        string stateCode,
        string stateName,
        string zipCode,
        bool isManualOverride)
    {
        // Validation
        if (sessionId == Guid.Empty)
            throw new ArgumentException("SessionId cannot be empty", nameof(sessionId));

        if (string.IsNullOrWhiteSpace(stateCode))
            throw new ArgumentException("StateCode is required", nameof(stateCode));

        if (stateCode.Length != 2 || !stateCode.All(char.IsUpper))
            throw new ArgumentException("StateCode must be 2 uppercase letters", nameof(stateCode));

        if (string.IsNullOrWhiteSpace(stateName))
            throw new ArgumentException("StateName is required", nameof(stateName));

        if (stateName.Length > 50)
            throw new ArgumentException("StateName cannot exceed 50 characters", nameof(stateName));

        if (string.IsNullOrWhiteSpace(zipCode))
            throw new ArgumentException("ZipCode is required", nameof(zipCode));

        if (zipCode.Length != 5 || !zipCode.All(char.IsDigit))
            throw new ArgumentException("ZipCode must be exactly 5 digits", nameof(zipCode));

        var now = DateTime.UtcNow;

        return new StateContext
        {
            Id = Guid.NewGuid(),
            SessionId = sessionId,
            StateCode = stateCode,
            StateName = stateName,
            ZipCode = zipCode,
            IsManualOverride = isManualOverride,
            EffectiveDate = now,
            CreatedAt = now
        };
    }

    /// <summary>
    /// Update the state context (testable, for manual state override)
    /// </summary>
    /// <param name="stateCode">New 2-letter state code</param>
    /// <param name="stateName">New full state name</param>
    /// <param name="isManualOverride">Whether this is a manual override</param>
    /// <exception cref="ArgumentException">Thrown when validation fails</exception>
    public void UpdateState(string stateCode, string stateName, bool isManualOverride)
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

        StateCode = stateCode;
        StateName = stateName;
        IsManualOverride = isManualOverride;
        UpdatedAt = DateTime.UtcNow;
    }
}
