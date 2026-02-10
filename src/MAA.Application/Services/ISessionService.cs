using MAA.Domain.Sessions;

namespace MAA.Application.Services;

/// <summary>
/// Service interface for session business logic.
/// Orchestrates session creation, validation, and state transitions.
/// </summary>
public interface ISessionService
{
    /// <summary>
    /// Creates a new anonymous session.
    /// </summary>
    /// <param name="ipAddress">Client IP address</param>
    /// <param name="userAgent">Browser user agent</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created session</returns>
    Task<Session> CreateSessionAsync(
        string ipAddress,
        string userAgent,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates that a session exists, is not expired, and is not revoked.
    /// </summary>
    /// <param name="sessionId">Session ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if session is valid</returns>
    Task<bool> ValidateSessionAsync(Guid sessionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a session by ID with validation.
    /// </summary>
    /// <param name="sessionId">Session ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Session if valid</returns>
    /// <exception cref="InvalidOperationException">If session not found or invalid</exception>
    Task<Session> GetSessionAsync(Guid sessionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Transitions a session to a new state with validation.
    /// </summary>
    /// <param name="sessionId">Session ID</param>
    /// <param name="newState">Target state</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated session</returns>
    Task<Session> TransitionStateAsync(
        Guid sessionId,
        SessionState newState,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks a session as timed out and abandoned.
    /// </summary>
    /// <param name="sessionId">Session ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task TimeoutSessionAsync(Guid sessionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Resets the inactivity timeout (sliding window).
    /// Called on each user request.
    /// </summary>
    /// <param name="sessionId">Session ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task ResetInactivityTimeoutAsync(Guid sessionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the standard CONST-III compliant session timeout error message.
    /// </summary>
    /// <returns>Session timeout error message</returns>
    Task<string> GetSessionTimeoutMessageAsync();

    /// <summary>
    /// Creates an authenticated session for a registered user (Phase 5).
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="ipAddress">Client IP address</param>
    /// <param name="userAgent">Browser user agent</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created session</returns>
    Task<Session> CreateAuthenticatedSessionAsync(
        Guid userId,
        string ipAddress,
        string userAgent,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all active (non-revoked, non-expired) sessions for a user (Phase 5).
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of active sessions</returns>
    Task<List<Session>> GetActiveSessionsAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Revokes a specific session (Phase 5).
    /// Marks session as IsRevoked = true.
    /// </summary>
    /// <param name="sessionId">Session ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task RevokeSessionAsync(Guid sessionId, CancellationToken cancellationToken = default);
}
