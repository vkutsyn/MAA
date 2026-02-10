namespace MAA.Application.Sessions.DTOs;

/// <summary>
/// Data Transfer Object for session creation requests.
/// Used to accept client input without exposing domain entities directly.
/// </summary>
public class CreateSessionDto
{
    /// <summary>
    /// Client IP address (required).
    /// </summary>
    /// <remarks>
    /// Format: IPv4 or IPv6 string
    /// Constraints: Required, max 45 characters
    /// Validation: Must be valid IP format
    /// If not provided, server will extract from request context
    /// Example: "192.168.1.100"
    /// </remarks>
    public string IpAddress { get; set; } = string.Empty;

    /// <summary>
    /// Browser user agent string (required).
    /// </summary>
    /// <remarks>
    /// Format: HTTP User-Agent header value
    /// Constraints: Required, max 500 characters
    /// If not provided, server will extract from request headers
    /// Example: "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36"
    /// Used for browser compatibility tracking and audit
    /// </remarks>
    public string UserAgent { get; set; } = string.Empty;

    /// <summary>
    /// Session timeout in minutes (optional, default 30).
    /// </summary>
    /// <remarks>
    /// Format: Positive integer minutes
    /// Constraints: Optional, typically 15-480 minutes
    /// Default: 30 minutes per Constitution III requirements
    /// Range: 15 (minimum) to 480 (8 hours maximum recommended)
    /// Example: 30
    /// Purpose: Absolute session expiration time
    /// </remarks>
    public int? TimeoutMinutes { get; set; }

    /// <summary>
    /// Inactivity timeout in minutes (optional, default 15).
    /// </summary>
    /// <remarks>
    /// Format: Positive integer minutes
    /// Constraints: Optional, typically 5-60 minutes
    /// Default: 15 minutes (Constitution III)
    /// Range: 5 (minimum for usability) to 60 (maximum for security)
    /// Example: 15
    /// Purpose: Sliding-window timeout - session expires if no activity
    /// Reset on: Each API call updates inactivity timeout
    /// </remarks>
    public int? InactivityTimeoutMinutes { get; set; }
}
