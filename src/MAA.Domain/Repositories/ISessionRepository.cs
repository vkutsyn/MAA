using MAA.Domain.Sessions;

namespace MAA.Domain.Repositories;

/// <summary>
/// Repository interface for Session entity operations.
/// Implements repository pattern for data access abstraction.
/// </summary>
public interface ISessionRepository
{
    /// <summary>
    /// Creates a new session in the database.
    /// </summary>
    /// <param name="session">Session entity to create</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created session with generated ID</returns>
    Task<Session> CreateAsync(Session session, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a session by its ID.
    /// </summary>
    /// <param name="id">Session ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Session if found, null otherwise</returns>
    Task<Session?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing session.
    /// </summary>
    /// <param name="session">Session entity with updated values</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated session</returns>
    Task<Session> UpdateAsync(Session session, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a session and all associated data.
    /// </summary>
    /// <param name="id">Session ID to delete</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists all expired sessions for cleanup.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of expired sessions</returns>
    Task<IEnumerable<Session>> ListExpiredAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets sessions by user ID (Phase 5 feature).
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of user's sessions</returns>
    Task<IEnumerable<Session>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
}
