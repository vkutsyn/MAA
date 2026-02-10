namespace MAA.Tests.Integration.DTOs;

/// <summary>
/// DTO for login/refresh response
/// </summary>
public class LoginResponse
{
    public string AccessToken { get; set; } = string.Empty;
    public string TokenType { get; set; } = string.Empty;
    public int ExpiresIn { get; set; }
    public string RefreshToken { get; set; } = string.Empty;
}

/// <summary>
/// DTO for 409 Conflict response when max sessions reached
/// </summary>
public class ConflictResponse
{
    public string Error { get; set; } = string.Empty;
    public List<ActiveSessionDto> ActiveSessions { get; set; } = new();
}

/// <summary>
/// DTO for active session info in conflict response
/// </summary>
public class ActiveSessionDto
{
    public Guid SessionId { get; set; }
    public string Device { get; set; } = string.Empty;
    public string IpAddress { get; set; } = string.Empty;
    public DateTime LoginTime { get; set; }
}
