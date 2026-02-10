namespace MAA.Application.Services;

/// <summary>
/// Service interface for JWT token operations (Phase 5 feature).
/// Stub implementation for Phase 1 - full implementation in Phase 5.
/// </summary>
public interface ITokenProvider
{
    /// <summary>
    /// Generates an access token (1 hour expiration) with user ID and roles.
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="roles">User roles</param>
    /// <param name="sessionId">Session ID to include as claim (optional)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>JWT access token</returns>
    Task<string> GenerateAccessTokenAsync(
        Guid userId,
        IEnumerable<string> roles,
        Guid? sessionId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates a refresh token (7 days expiration).
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>JWT refresh token</returns>
    Task<string> GenerateRefreshTokenAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates a JWT token and returns claims.
    /// </summary>
    /// <param name="token">JWT token to validate</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if valid</returns>
    Task<bool> ValidateTokenAsync(string token, CancellationToken cancellationToken = default);

    /// <summary>
    /// Extracts user ID from a JWT token.
    /// </summary>
    /// <param name="token">JWT token</param>
    /// <returns>User ID</returns>
    Guid GetUserIdFromToken(string token);

    /// <summary>
    /// Extracts roles from a JWT token.
    /// </summary>
    /// <param name="token">JWT token</param>
    /// <returns>User roles</returns>
    IEnumerable<string> GetRolesFromToken(string token);

    /// <summary>
    /// Checks if access token needs automatic refresh (expires soon).
    /// </summary>
    /// <param name="token">JWT token</param>
    /// <returns>True if token expires in less than RefreshThresholdMinutes</returns>
    bool NeedsRefresh(string token);

    /// <summary>
    /// Gets token expiration timestamp.
    /// </summary>
    /// <param name="token">JWT token</param>
    /// <returns>DateTime when token expires (UTC)</returns>
    DateTime GetTokenExpiration(string token);
}
