namespace MAA.Application.StateContext.DTOs;

/// <summary>
/// Data transfer object for StateContext entity
/// </summary>
public record StateContextDto
{
    /// <summary>
    /// State context ID
    /// </summary>
    public required Guid Id { get; init; }

    /// <summary>
    /// Session ID this context belongs to
    /// </summary>
    public required Guid SessionId { get; init; }

    /// <summary>
    /// 2-letter state code (e.g., "CA", "NY")
    /// </summary>
    public required string StateCode { get; init; }

    /// <summary>
    /// Full state name (e.g., "California")
    /// </summary>
    public required string StateName { get; init; }

    /// <summary>
    /// User-entered ZIP code
    /// </summary>
    public required string ZipCode { get; init; }

    /// <summary>
    /// True if user manually selected state vs auto-detected
    /// </summary>
    public required bool IsManualOverride { get; init; }

    /// <summary>
    /// When this state context was established (UTC)
    /// </summary>
    public required DateTime EffectiveDate { get; init; }

    /// <summary>
    /// Record creation timestamp (UTC)
    /// </summary>
    public required DateTime CreatedAt { get; init; }

    /// <summary>
    /// Record update timestamp (UTC), null if never updated
    /// </summary>
    public DateTime? UpdatedAt { get; init; }
}
