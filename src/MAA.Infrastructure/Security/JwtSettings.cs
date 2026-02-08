namespace MAA.Infrastructure.Security;

/// <summary>
/// JWT settings configuration (Phase 5 feature).
/// Loaded from appsettings.json under "Jwt" section.
/// </summary>
public class JwtSettings
{
    /// <summary>
    /// JWT issuer claim value (e.g., "maa-api").
    /// </summary>
    public string Issuer { get; set; } = "maa-api";

    /// <summary>
    /// JWT audience claim value (e.g., "maa-client").
    /// </summary>
    public string Audience { get; set; } = "maa-client";

    /// <summary>
    /// Secret key for signing tokens (from Azure Key Vault or config).
    /// NEVER expose this in logs or error messages.
    /// </summary>
    public string SecretKey { get; set; } = string.Empty;

    /// <summary>
    /// Access token expiration in minutes (typically 60 for 1 hour).
    /// </summary>
    public int AccessTokenExpirationMinutes { get; set; } = 60;

    /// <summary>
    /// Refresh token expiration in days (typically 7 for 1 week).
    /// </summary>
    public int RefreshTokenExpirationDays { get; set; } = 7;

    /// <summary>
    /// Maximum concurrent sessions per user (Phase 5).
    /// Default: 3 devices max.
    /// </summary>
    public int MaxConcurrentSessions { get; set; } = 3;

    /// <summary>
    /// Refresh token auto-refresh threshold in minutes.
    /// If access token expires in less than this, auto-refresh is triggered.
    /// Default: 5 minutes.
    /// </summary>
    public int RefreshThresholdMinutes { get; set; } = 5;

    /// <summary>
    /// Validates settings are properly configured.
    /// </summary>
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(Issuer))
            throw new InvalidOperationException("JWT Issuer must be configured");

        if (string.IsNullOrWhiteSpace(Audience))
            throw new InvalidOperationException("JWT Audience must be configured");

        if (string.IsNullOrWhiteSpace(SecretKey))
            throw new InvalidOperationException("JWT SecretKey must be configured (from Key Vault)");

        if (AccessTokenExpirationMinutes <= 0)
            throw new InvalidOperationException("AccessTokenExpirationMinutes must be positive");

        if (RefreshTokenExpirationDays <= 0)
            throw new InvalidOperationException("RefreshTokenExpirationDays must be positive");

        if (MaxConcurrentSessions <= 0)
            throw new InvalidOperationException("MaxConcurrentSessions must be positive");
    }
}
