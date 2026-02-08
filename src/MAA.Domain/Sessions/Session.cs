using System.ComponentModel.DataAnnotations;

namespace MAA.Domain.Sessions;

/// <summary>
/// Represents an anonymous or authenticated user session.
/// Tracks state, timeouts, and encryption key version for session data.
/// </summary>
public class Session
{
    /// <summary>
    /// Unique session identifier (GUID).
    /// </summary>
    [Key]
    public Guid Id { get; set; }

    /// <summary>
    /// Current state in the session lifecycle.
    /// </summary>
    [Required]
    public SessionState State { get; set; }

    /// <summary>
    /// Foreign key to User (nullable - Phase 5 feature).
    /// Null for anonymous sessions (Phase 1).
    /// </summary>
    public Guid? UserId { get; set; }

    /// <summary>
    /// Client IP address (for anomaly detection, Phase 3).
    /// </summary>
    [Required]
    [MaxLength(45)] // IPv6 max length
    public string IpAddress { get; set; } = string.Empty;

    /// <summary>
    /// Browser user agent string (for device tracking).
    /// </summary>
    [Required]
    [MaxLength(500)]
    public string UserAgent { get; set; } = string.Empty;

    /// <summary>
    /// Session type: 'anonymous' or 'authenticated' (Phase 5).
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string SessionType { get; set; } = "anonymous";

    /// <summary>
    /// Which encryption key version was used to encrypt session data.
    /// References EncryptionKey.KeyVersion for key rotation support.
    /// </summary>
    [Required]
    public int EncryptionKeyVersion { get; set; }

    /// <summary>
    /// JSONB storage for flexible session data (answers, metadata).
    /// Structure defined in SessionDataSchema.
    /// </summary>
    [Required]
    public string Data { get; set; } = "{}";

    /// <summary>
    /// Absolute expiry time (30 min for anonymous, 8 hr for admin).
    /// Session invalid after this time regardless of activity.
    /// </summary>
    [Required]
    public DateTime ExpiresAt { get; set; }

    /// <summary>
    /// Sliding window timeout (resets on each request).
    /// Session invalid after this time if no activity.
    /// </summary>
    [Required]
    public DateTime InactivityTimeoutAt { get; set; }

    /// <summary>
    /// Timestamp of last user activity (for analytics, Phase 6).
    /// Updated on each API request.
    /// </summary>
    public DateTime LastActivityAt { get; set; }

    /// <summary>
    /// Explicit logout or abandonment flag (Phase 5).
    /// Once true, session cannot be reactivated.
    /// </summary>
    public bool IsRevoked { get; set; }

    /// <summary>
    /// Session creation timestamp.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Last update timestamp (for optimistic locking).
    /// </summary>
    public DateTime UpdatedAt { get; set; }

    /// <summary>
    /// Version number for optimistic concurrency control.
    /// Incremented on each update to detect conflicts.
    /// </summary>
    [ConcurrencyCheck]
    public int Version { get; set; }

    /// <summary>
    /// Navigation property to User (Phase 5).
    /// </summary>
    // public User? User { get; set; }

    /// <summary>
    /// Navigation property to SessionAnswers.
    /// </summary>
    // public ICollection<SessionAnswer> Answers { get; set; } = new List<SessionAnswer>();

    /// <summary>
    /// Domain validation: Ensures session configuration is valid.
    /// </summary>
    public void Validate()
    {
        if (Id == Guid.Empty)
            throw new InvalidOperationException("Session ID cannot be empty");

        if (ExpiresAt <= DateTime.UtcNow)
            throw new InvalidOperationException("Session ExpiresAt must be in the future");

        if (InactivityTimeoutAt <= DateTime.UtcNow)
            throw new InvalidOperationException("Session InactivityTimeoutAt must be in the future");

        if (Version < 0)
            throw new InvalidOperationException("Version cannot be negative");

        if (string.IsNullOrWhiteSpace(IpAddress))
            throw new InvalidOperationException("IpAddress is required");

        if (string.IsNullOrWhiteSpace(UserAgent))
            throw new InvalidOperationException("UserAgent is required");
    }

    /// <summary>
    /// Determines if session is currently valid (not expired, not revoked).
    /// </summary>
    public bool IsValid()
    {
        var now = DateTime.UtcNow;
        return !IsRevoked 
            && ExpiresAt > now 
            && InactivityTimeoutAt > now
            && State != SessionState.Abandoned;
    }

    /// <summary>
    /// Checks if session can transition to a new state.
    /// Enforces state machine rules.
    /// </summary>
    public bool CanTransitionTo(SessionState newState)
    {
        return (State, newState) switch
        {
            // Valid transitions
            (SessionState.Pending, SessionState.InProgress) => true,
            (SessionState.InProgress, SessionState.Submitted) => true,
            (SessionState.Submitted, SessionState.Completed) => true,
            (_, SessionState.Abandoned) => true, // Can abandon from any state
            
            // No-op (same state)
            var (current, next) when current == next => true,
            
            // All other transitions invalid
            _ => false
        };
    }

    /// <summary>
    /// Transitions session to a new state with validation.
    /// </summary>
    public void TransitionTo(SessionState newState)
    {
        if (!CanTransitionTo(newState))
        {
            throw new InvalidOperationException(
                $"Invalid state transition from {State} to {newState}");
        }

        State = newState;
        UpdatedAt = DateTime.UtcNow;
        Version++;
    }

    /// <summary>
    /// Resets the inactivity timeout (sliding window).
    /// Called on each user request to extend session.
    /// </summary>
    public void ResetInactivityTimeout(int timeoutMinutes)
    {
        InactivityTimeoutAt = DateTime.UtcNow.AddMinutes(timeoutMinutes);
        LastActivityAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
        Version++;
    }
}
