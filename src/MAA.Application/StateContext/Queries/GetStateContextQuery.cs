namespace MAA.Application.StateContext.Queries;

/// <summary>
/// Query to get state context by session ID
/// </summary>
public record GetStateContextQuery
{
    public required Guid SessionId { get; init; }
}
