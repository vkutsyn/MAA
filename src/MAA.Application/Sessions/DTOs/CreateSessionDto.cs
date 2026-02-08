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
    public string IpAddress { get; set; } = string.Empty;

    /// <summary>
    /// Browser user agent string (required).
    /// </summary>
    public string UserAgent { get; set; } = string.Empty;

    /// <summary>
    /// Session timeout in minutes (optional, default 30).
    /// </summary>
    public int? TimeoutMinutes { get; set; }

    /// <summary>
    /// Inactivity timeout in minutes (optional, default 15).
    /// </summary>
    public int? InactivityTimeoutMinutes { get; set; }
}
