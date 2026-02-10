namespace MAA.Application.Sessions.DTOs;

/// <summary>
/// Data Transfer Object for session retrieval and responses.
/// Contains serializable session information for API responses.
/// </summary>
public class SessionDto
{
    /// <summary>
    /// Unique session identifier.
    /// </summary>
    /// <remarks>
    /// Format: UUID v4
    /// Constraints: Non-empty, immutable
    /// Example: "550e8400-e29b-41d4-a716-446655440000"
    /// </remarks>
    public Guid Id { get; set; }

    /// <summary>
    /// Current session state.
    /// </summary>
    /// <remarks>
    /// Allowed values: "draft", "submitted", "approved", "rejected"
    /// Constraints: Required, case-sensitive
    /// Used to track application progress through the workflow
    /// </remarks>
    public string State { get; set; } = string.Empty;

    /// <summary>
    /// User ID (nullable for anonymous sessions).
    /// </summary>
    /// <remarks>
    /// Format: UUID v4 or null
    /// Constraints: Non-empty if authenticated; null if anonymous
    /// References User entity
    /// Example: "f47ac10b-58cc-4372-a567-0e02b2c3d479"
    /// </remarks>
    public Guid? UserId { get; set; }

    /// <summary>
    /// Client IP address.
    /// </summary>
    /// <remarks>
    /// Format: IPv4 or IPv6 string
    /// Constraints: Non-empty string, max 45 characters
    /// Example: "192.168.1.100"
    /// Used for audit logging and fraud detection
    /// </remarks>
    public string IpAddress { get; set; } = string.Empty;

    /// <summary>
    /// Browser user agent string.
    /// </summary>
    /// <remarks>
    /// Format: HTTP User-Agent header value
    /// Constraints: Non-empty string, max 500 characters
    /// Example: "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36"
    /// Used for browser compatibility tracking
    /// </remarks>
    public string UserAgent { get; set; } = string.Empty;

    /// <summary>
    /// Session type: 'anonymous' or 'authenticated'.
    /// </summary>
    /// <remarks>
    /// Allowed values: "anonymous", "authenticated"
    /// Constraints: Required, defaults to "anonymous"
    /// Determines if session is linked to a user account
    /// </remarks>
    public string SessionType { get; set; } = "anonymous";

    /// <summary>
    /// Encryption key version used for this session.
    /// </summary>
    /// <remarks>
    /// Format: Integer version number
    /// Constraints: Positive integer, required
    /// Used for proper decryption of session data
    /// Reference: Current encryption key version
    /// Example: 3 (indicating key rotation has occurred)
    /// </remarks>
    public int EncryptionKeyVersion { get; set; }

    /// <summary>
    /// Absolute expiry time.
    /// </summary>
    /// <remarks>
    /// Format: DateTime (ISO 8601, UTC)
    /// Constraints: Required, must be in future at creation
    /// Type-specific: Session is invalid if DateTime.UtcNow > ExpiresAt
    /// Example: "2026-02-11T10:30:00Z"
    /// Typical TTL: 8 hours from creation
    /// </remarks>
    public DateTime ExpiresAt { get; set; }

    /// <summary>
    /// Sliding window inactivity timeout.
    /// </summary>
    /// <remarks>
    /// Format: DateTime (ISO 8601, UTC)
    /// Constraints: Required, must be in future at creation
    /// Updated on each user activity (answer submission)
    /// Type-specific: Session is invalid if DateTime.UtcNow > InactivityTimeoutAt
    /// Example: "2026-02-10T18:00:00Z"
    /// Typical inactivity: 30 minutes without activity
    /// </remarks>
    public DateTime InactivityTimeoutAt { get; set; }

    /// <summary>
    /// Last recorded user activity timestamp.
    /// </summary>
    /// <remarks>
    /// Format: DateTime (ISO 8601, UTC)
    /// Constraints: Required, updated on each API call
    /// Used to calculate remaining inactivity timeout
    /// Example: "2026-02-10T17:45:00Z"
    /// </remarks>
    public DateTime LastActivityAt { get; set; }

    /// <summary>
    /// Is this session currently revoked?
    /// </summary>
    /// <remarks>
    /// Format: Boolean flag
    /// Constraints: Required, defaults to false
    /// When true: Session cannot be used despite absolute expiry
    /// Used for: User logout, security events, admin actions
    /// Type-specific: IsValid returns false if IsRevoked == true
    /// </remarks>
    public bool IsRevoked { get; set; }

    /// <summary>
    /// Session creation timestamp.
    /// </summary>
    /// <remarks>
    /// Format: DateTime (ISO 8601, UTC)
    /// Constraints: Required, immutable
    /// Example: "2026-02-10T10:00:00Z"
    /// Used for audit trail
    /// </remarks>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Last update timestamp.
    /// </summary>
    /// <remarks>
    /// Format: DateTime (ISO 8601, UTC)
    /// Constraints: Required, updated on any modification
    /// Example: "2026-02-10T17:45:00Z"
    /// Used for optimistic concurrency control
    /// </remarks>
    public DateTime UpdatedAt { get; set; }

    /// <summary>
    /// Computed property: Is session currently valid?
    /// </summary>
    public bool IsValid =>
        !IsRevoked
        && ExpiresAt > DateTime.UtcNow
        && InactivityTimeoutAt > DateTime.UtcNow;

    /// <summary>
    /// Computed property: Minutes until expiry.
    /// </summary>
    public int MinutesUntilExpiry =>
        (int)Math.Max(0, (ExpiresAt - DateTime.UtcNow).TotalMinutes);

    /// <summary>
    /// Computed property: Minutes until inactivity timeout.
    /// </summary>
    public int MinutesUntilInactivityTimeout =>
        (int)Math.Max(0, (InactivityTimeoutAt - DateTime.UtcNow).TotalMinutes);
}
