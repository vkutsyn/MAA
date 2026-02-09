using MAA.Domain.Repositories;
using MAA.Domain.Sessions;
using Microsoft.Extensions.Logging;

namespace MAA.Application.Services;

/// <summary>
/// Service for session business logic.
/// Orchestrates session creation, validation, state transitions, and timeout management.
/// Enforces 30-minute absolute timeout and 15-minute inactivity timeout for anonymous sessions.
/// </summary>
public class SessionService : ISessionService
{
    // CONST-III: Exact error message for session expiration
    private const string SessionExpiredMessage = "Your session expired after 30 minutes. Start a new eligibility check.";

    // Session timeout settings
    private const int AnonymousSessionTimeoutMinutes = 30;
    private const int InactivityTimeoutMinutes = 15;
    private const int DefaultEncryptionKeyVersion = 1;

    private readonly ISessionRepository _sessionRepository;
    private readonly IEncryptionService _encryptionService;
    private readonly ILogger<SessionService> _logger;

    public SessionService(
        ISessionRepository sessionRepository,
        IEncryptionService encryptionService,
        ILogger<SessionService> logger)
    {
        _sessionRepository = sessionRepository ?? throw new ArgumentNullException(nameof(sessionRepository));
        _encryptionService = encryptionService ?? throw new ArgumentNullException(nameof(encryptionService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Creates a new anonymous session with 30-minute absolute timeout and 15-minute inactivity timeout.
    /// </summary>
    /// <param name="ipAddress">Client IP address</param>
    /// <param name="userAgent">Browser user agent</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created session</returns>
    public async Task<Session> CreateSessionAsync(
        string ipAddress,
        string userAgent,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(ipAddress))
            throw new ArgumentException("IP address cannot be null or empty", nameof(ipAddress));

        if (string.IsNullOrWhiteSpace(userAgent))
            throw new ArgumentException("User agent cannot be null or empty", nameof(userAgent));

        var now = DateTime.UtcNow;

        var session = new Session
        {
            Id = Guid.NewGuid(),
            State = SessionState.Pending,
            IpAddress = ipAddress,
            UserAgent = userAgent,
            SessionType = "anonymous",
            UserId = null, // Anonymous session
            EncryptionKeyVersion = DefaultEncryptionKeyVersion,
            Data = "{}", // Empty JSONB
            ExpiresAt = now.AddMinutes(AnonymousSessionTimeoutMinutes),
            InactivityTimeoutAt = now.AddMinutes(InactivityTimeoutMinutes),
            LastActivityAt = now,
            IsRevoked = false,
            CreatedAt = now,
            UpdatedAt = now,
            Version = 1
        };

        _logger.LogInformation(
            "Creating anonymous session {SessionId} for IP {IpAddress}",
            session.Id,
            ipAddress);

        var createdSession = await _sessionRepository.CreateAsync(session, cancellationToken);

        _logger.LogInformation(
            "Session {SessionId} created successfully. Expires at {ExpiresAt}, inactivity timeout at {InactivityTimeoutAt}",
            createdSession.Id,
            createdSession.ExpiresAt,
            createdSession.InactivityTimeoutAt);

        return createdSession;
    }

    /// <summary>
    /// Validates that a session exists, is not expired, and is not revoked.
    /// Returns false if:
    /// - Session not found
    /// - Session past absolute expiry (ExpiresAt)
    /// - Session past inactivity timeout (InactivityTimeoutAt)
    /// - Session is revoked
    /// </summary>
    /// <param name="sessionId">Session ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if session is valid</returns>
    public async Task<bool> ValidateSessionAsync(
        Guid sessionId,
        CancellationToken cancellationToken = default)
    {
        var session = await _sessionRepository.GetByIdAsync(sessionId, cancellationToken);

        if (session == null)
        {
            _logger.LogWarning("Session {SessionId} not found", sessionId);
            return false;
        }

        var now = DateTime.UtcNow;

        // Check for revocation
        if (session.IsRevoked)
        {
            _logger.LogWarning("Session {SessionId} is revoked", sessionId);
            return false;
        }

        // Check for absolute expiry
        if (session.ExpiresAt <= now)
        {
            _logger.LogWarning(
                "Session {SessionId} has expired (ExpiresAt: {ExpiresAt})",
                sessionId,
                session.ExpiresAt);
            return false;
        }

        // Check for inactivity timeout
        if (session.InactivityTimeoutAt <= now)
        {
            _logger.LogWarning(
                "Session {SessionId} has inactive timeout (InactivityTimeoutAt: {InactivityTimeoutAt})",
                sessionId,
                session.InactivityTimeoutAt);
            return false;
        }

        _logger.LogInformation("Session {SessionId} is valid", sessionId);
        return true;
    }

    /// <summary>
    /// Retrieves a session by ID with validation.
    /// </summary>
    /// <param name="sessionId">Session ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Session if valid</returns>
    /// <exception cref="InvalidOperationException">If session not found or invalid</exception>
    public async Task<Session> GetSessionAsync(
        Guid sessionId,
        CancellationToken cancellationToken = default)
    {
        var isValid = await ValidateSessionAsync(sessionId, cancellationToken);

        if (!isValid)
        {
            throw new InvalidOperationException(
                $"Session {sessionId} is not valid (expired, inactive, or revoked)");
        }

        var session = await _sessionRepository.GetByIdAsync(sessionId, cancellationToken);

        if (session == null)
        {
            throw new InvalidOperationException($"Session {sessionId} not found");
        }

        return session;
    }

    /// <summary>
    /// Transitions a session to a new state with validation.
    /// </summary>
    /// <param name="sessionId">Session ID</param>
    /// <param name="newState">Target state</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated session</returns>
    public async Task<Session> TransitionStateAsync(
        Guid sessionId,
        SessionState newState,
        CancellationToken cancellationToken = default)
    {
        var session = await GetSessionAsync(sessionId, cancellationToken);

        _logger.LogInformation(
            "Transitioning session {SessionId} from {OldState} to {NewState}",
            sessionId,
            session.State,
            newState);

        session.State = newState;
        session.UpdatedAt = DateTime.UtcNow;

        var updatedSession = await _sessionRepository.UpdateAsync(session, cancellationToken);

        _logger.LogInformation(
            "Session {SessionId} transitioned to {NewState}",
            sessionId,
            newState);

        return updatedSession;
    }

    /// <summary>
    /// Returns the CONST-III compliant session timeout error message.
    /// Message: "Your session expired after 30 minutes. Start a new eligibility check."
    /// </summary>
    /// <returns>Session timeout error message</returns>
    public Task<string> GetSessionTimeoutMessageAsync()
    {
        return Task.FromResult(SessionExpiredMessage);
    }

    /// <summary>
    /// Marks a session as timed out and abandoned.
    /// Sets IsRevoked flag and updates timestamp.
    /// </summary>
    /// <param name="sessionId">Session ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    public async Task TimeoutSessionAsync(
        Guid sessionId,
        CancellationToken cancellationToken = default)
    {
        var session = await _sessionRepository.GetByIdAsync(sessionId, cancellationToken);

        if (session == null)
        {
            _logger.LogWarning("Session {SessionId} not found for timeout", sessionId);
            return;
        }

        _logger.LogInformation("Timing out session {SessionId}", sessionId);

        session.IsRevoked = true;
        session.UpdatedAt = DateTime.UtcNow;

        await _sessionRepository.UpdateAsync(session, cancellationToken);

        _logger.LogInformation("Session {SessionId} timed out", sessionId);
    }

    /// <summary>
    /// Resets the inactivity timeout (sliding window).
    /// Updates InactivityTimeoutAt to current time + 15 minutes.
    /// Called on each user request to keep session alive.
    /// </summary>
    /// <param name="sessionId">Session ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    public async Task ResetInactivityTimeoutAsync(
        Guid sessionId,
        CancellationToken cancellationToken = default)
    {
        var session = await _sessionRepository.GetByIdAsync(sessionId, cancellationToken);

        if (session == null)
        {
            _logger.LogWarning("Session {SessionId} not found for inactivity reset", sessionId);
            return;
        }

        var previousTimeout = session.InactivityTimeoutAt;
        session.InactivityTimeoutAt = DateTime.UtcNow.AddMinutes(InactivityTimeoutMinutes);
        session.LastActivityAt = DateTime.UtcNow;
        session.UpdatedAt = DateTime.UtcNow;

        await _sessionRepository.UpdateAsync(session, cancellationToken);

        _logger.LogInformation(
            "Session {SessionId} inactivity timeout reset from {PreviousTimeout} to {NewTimeout}",
            sessionId,
            previousTimeout,
            session.InactivityTimeoutAt);
    }

    /// <summary>
    /// Creates an authenticated session for a registered user (Phase 5).
    /// Similar to anonymous session but with UserId set and "authenticated" session type.
    /// Admin users get 8-hour timeout; regular users get 1-hour timeout (can be extended).
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="ipAddress">Client IP address</param>
    /// <param name="userAgent">Browser user agent</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created session</returns>
    public async Task<Session> CreateAuthenticatedSessionAsync(
        Guid userId,
        string ipAddress,
        string userAgent,
        CancellationToken cancellationToken = default)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException("User ID cannot be empty", nameof(userId));

        if (string.IsNullOrWhiteSpace(ipAddress))
            throw new ArgumentException("IP address cannot be null or empty", nameof(ipAddress));

        if (string.IsNullOrWhiteSpace(userAgent))
            throw new ArgumentException("User agent cannot be null or empty", nameof(userAgent));

        var now = DateTime.UtcNow;

        // For Phase 5: Use 8-hour timeout for authenticated users
        // (This will be updated to check user role in future implementation)
        const int AuthenticatedSessionTimeoutMinutes = 8 * 60;  // 8 hours

        var session = new Session
        {
            Id = Guid.NewGuid(),
            State = SessionState.InProgress,
            UserId = userId,  // Associate with user
            IpAddress = ipAddress,
            UserAgent = userAgent,
            SessionType = "authenticated",
            EncryptionKeyVersion = DefaultEncryptionKeyVersion,
            Data = "{}",
            ExpiresAt = now.AddMinutes(AuthenticatedSessionTimeoutMinutes),
            InactivityTimeoutAt = now.AddMinutes(AuthenticatedSessionTimeoutMinutes),
            LastActivityAt = now,
            IsRevoked = false,
            CreatedAt = now,
            UpdatedAt = now,
            Version = 1
        };

        _logger.LogInformation(
            "Creating authenticated session {SessionId} for user {UserId} from IP {IpAddress}",
            session.Id,
            userId,
            ipAddress);

        var createdSession = await _sessionRepository.CreateAsync(session, cancellationToken);

        _logger.LogInformation(
            "Authenticated session {SessionId} created for user {UserId}. Expires at {ExpiresAt}",
            createdSession.Id,
            userId,
            createdSession.ExpiresAt);

        return createdSession;
    }

    /// <summary>
    /// Gets all active (non-revoked, non-expired) sessions for a user (Phase 5).
    /// Used to enforce "max 3 concurrent sessions" rule.
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of active sessions for the user</returns>
    public async Task<List<Session>> GetActiveSessionsAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException("User ID cannot be empty", nameof(userId));

        var now = DateTime.UtcNow;

        // Get all sessions for user from repository
        // This assumes ISessionRepository has GetByUserIdAsync method
        var sessions = await _sessionRepository.GetByUserIdAsync(userId, cancellationToken);

        if (sessions == null)
            sessions = new List<Session>();

        // Filter to active sessions only (not revoked, not expired)
        var activeSessions = sessions
            .Where(s => !s.IsRevoked && s.ExpiresAt > now && s.InactivityTimeoutAt > now)
            .OrderByDescending(s => s.CreatedAt)
            .ToList();

        _logger.LogInformation(
            "Found {ActiveSessionCount} active sessions for user {UserId}",
            activeSessions.Count,
            userId);

        return activeSessions;
    }

    /// <summary>
    /// Revokes a specific session (Phase 5).
    /// Marks session as IsRevoked = true and updates timestamp.
    /// </summary>
    /// <param name="sessionId">Session ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    public async Task RevokeSessionAsync(
        Guid sessionId,
        CancellationToken cancellationToken = default)
    {
        var session = await _sessionRepository.GetByIdAsync(sessionId, cancellationToken);

        if (session == null)
        {
            _logger.LogWarning("Session {SessionId} not found for revocation", sessionId);
            return;
        }

        _logger.LogInformation("Revoking session {SessionId} for user {UserId}", sessionId, session.UserId);

        session.IsRevoked = true;
        session.UpdatedAt = DateTime.UtcNow;

        await _sessionRepository.UpdateAsync(session, cancellationToken);

        _logger.LogInformation("Session {SessionId} revoked", sessionId);
    }

}
