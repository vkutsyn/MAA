using MAA.Application.StateContext.DTOs;

namespace MAA.Application.StateContext.Commands;

/// <summary>
/// Command to initialize state context from ZIP code
/// </summary>
public record InitializeStateContextCommand
{
    public required Guid SessionId { get; init; }
    public required string ZipCode { get; init; }
    public string? StateCodeOverride { get; init; }
}

/// <summary>
/// Result of initializing state context
/// </summary>
public record InitializeStateContextResult
{
    public required StateContextResponse StateContextResponse { get; init; }
}
