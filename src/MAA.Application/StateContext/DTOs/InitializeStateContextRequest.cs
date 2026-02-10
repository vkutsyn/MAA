namespace MAA.Application.StateContext.DTOs;

/// <summary>
/// Request to initialize state context from ZIP code
/// </summary>
public record InitializeStateContextRequest
{
    /// <summary>
    /// The session ID this state context belongs to
    /// </summary>
    public required Guid SessionId { get; init; }

    /// <summary>
    /// 5-digit ZIP code for state resolution
    /// </summary>
    public required string ZipCode { get; init; }

    /// <summary>
    /// Optional manual state code override (e.g., for users who recently moved)
    /// </summary>
    public string? StateCodeOverride { get; init; }
}
