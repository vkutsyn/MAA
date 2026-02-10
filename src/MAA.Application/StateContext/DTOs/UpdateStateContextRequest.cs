namespace MAA.Application.StateContext.DTOs;

/// <summary>
/// Request to update an existing state context with a new state code
/// </summary>
public record UpdateStateContextRequest
{
    /// <summary>
    /// The session ID whose state context should be updated
    /// </summary>
    public required Guid SessionId { get; init; }

    /// <summary>
    /// 2-letter state code to set (e.g., "CA", "NY", "NJ")
    /// </summary>
    public required string StateCode { get; init; }

    /// <summary>
    /// ZIP code to associate with this state (for audit trail purposes)
    /// </summary>
    public required string ZipCode { get; init; }

    /// <summary>
    /// Indicates that this is a manual override by the user
    /// </summary>
    public bool IsManualOverride { get; init; } = true;
}
