using MAA.Application.StateContext.DTOs;

namespace MAA.Application.StateContext.Commands;

/// <summary>
/// Command to update an existing state context
/// </summary>
public record UpdateStateContextCommand
{
    public required Guid SessionId { get; init; }
    public required string StateCode { get; init; }
    public required string ZipCode { get; init; }
    public required bool IsManualOverride { get; init; }
}

/// <summary>
/// Result of updating state context
/// </summary>
public record UpdateStateContextResult
{
    public required StateContextResponse StateContextResponse { get; init; }
}
