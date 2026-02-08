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
    public Guid Id { get; set; }

    /// <summary>
    /// Current session state.
    /// </summary>
    public string State { get; set; } = string.Empty;

    /// <summary>
    /// User ID (nullable for anonymous sessions).
    /// </summary>
    public Guid? UserId { get; set; }

    /// <summary>
    /// Client IP address.
    /// </summary>
    public string IpAddress { get; set; } = string.Empty;

    /// <summary>
    /// Browser user agent string.
    /// </summary>
    public string UserAgent { get; set; } = string.Empty;

    /// <summary>
    /// Session type: 'anonymous' or 'authenticated'.
    /// </summary>
    public string SessionType { get; set; } = "anonymous";

    /// <summary>
    /// Encryption key version used for this session.
    /// </summary>
    public int EncryptionKeyVersion { get; set; }

    /// <summary>
    /// Absolute expiry time.
    /// </summary>
    public DateTime ExpiresAt { get; set; }

    /// <summary>
    /// Sliding window inactivity timeout.
    /// </summary>
    public DateTime InactivityTimeoutAt { get; set; }

    /// <summary>
    /// Last recorded user activity timestamp.
    /// </summary>
    public DateTime LastActivityAt { get; set; }

    /// <summary>
    /// Is this session currently revoked?
    /// </summary>
    public bool IsRevoked { get; set; }

    /// <summary>
    /// Session creation timestamp.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Last update timestamp.
    /// </summary>
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
