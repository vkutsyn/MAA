namespace MAA.Application.DTOs;

/// <summary>
/// Standard response for optimistic concurrency conflicts.
/// </summary>
public record ConcurrencyErrorResponse
{
    /// <summary>
    /// Error code for concurrency conflicts.
    /// </summary>
    public required string Error { get; init; }

    /// <summary>
    /// Human-readable message describing the conflict.
    /// </summary>
    public required string Message { get; init; }

    /// <summary>
    /// Optional current state payload for conflict resolution.
    /// </summary>
    public object? Current { get; init; }
}
