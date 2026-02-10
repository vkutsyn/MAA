namespace MAA.Application.StateContext;

/// <summary>
/// Repository interface for StateContext entity
/// </summary>
public interface IStateContextRepository
{
    /// <summary>
    /// Gets a state context by session ID
    /// </summary>
    Task<Domain.StateContext.StateContext?> GetBySessionIdAsync(Guid sessionId);

    /// <summary>
    /// Adds a new state context
    /// </summary>
    Task<Domain.StateContext.StateContext> AddAsync(Domain.StateContext.StateContext stateContext);

    /// <summary>
    /// Updates an existing state context
    /// </summary>
    Task UpdateAsync(Domain.StateContext.StateContext stateContext);

    /// <summary>
    /// Checks if a state context exists for a session
    /// </summary>
    Task<bool> ExistsBySessionIdAsync(Guid sessionId);
}
