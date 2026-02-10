using MAA.Application.Services;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace MAA.Infrastructure.Security;

/// <summary>
/// JWT token provider for user authentication (Phase 5 feature).
/// Generates, validates, and extracts claims from JWT tokens.
/// Uses HMAC-SHA256 signing with configurable secret key from Azure Key Vault.
/// </summary>
public class JwtTokenProvider : ITokenProvider
{
    private readonly JwtSettings _settings;
    private readonly ILogger<JwtTokenProvider> _logger;
    private readonly JwtSecurityTokenHandler _tokenHandler;

    public JwtTokenProvider(ILogger<JwtTokenProvider> logger)
    {
        _logger = logger;
        // This will be overridden when DI is configured
        _settings = new JwtSettings();
        _tokenHandler = new JwtSecurityTokenHandler();
    }

    /// <summary>
    /// Initializes JwtTokenProvider with settings (called during DI setup).
    /// </summary>
    public JwtTokenProvider(JwtSettings settings, ILogger<JwtTokenProvider> logger)
    {
        _settings = settings;
        _logger = logger;
        _tokenHandler = new JwtSecurityTokenHandler();

        _settings.Validate();
    }

    /// <summary>
    /// Generates an access token (1 hour expiration) with user ID and roles.
    /// </summary>
    /// <param name="userId">User ID (subject claim)</param>
    /// <param name="roles">User roles to include in token</param>
    /// <param name="cancellationToken">Cancellation token (unused for synchronous operation)</param>
    /// <returns>JWT access token string</returns>
    /// <remarks>
    /// Token structure:
    /// - sub: User ID (GUID)
    /// - role: User roles (multiple role claims if applicable)
    /// - iss: Issuer (e.g., "maa-api")
    /// - aud: Audience (e.g., "maa-client")
    /// - exp: Expiration time (1 hour from now)
    /// - iat: Issued at time (now)
    /// - nbf: Not before time (now)
    /// </remarks>
    public async Task<string> GenerateAccessTokenAsync(
        Guid userId,
        IEnumerable<string> roles,
        Guid? sessionId = null,
        CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            try
            {
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                    new Claim("sub", userId.ToString()),
                };

                if (sessionId.HasValue && sessionId.Value != Guid.Empty)
                {
                    claims.Add(new Claim("sessionId", sessionId.Value.ToString()));
                }

                // Add role claims
                foreach (var role in roles)
                {
                    claims.Add(new Claim(ClaimTypes.Role, role));
                }

                var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.SecretKey));
                var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

                var token = new JwtSecurityToken(
                    issuer: _settings.Issuer,
                    audience: _settings.Audience,
                    claims: claims,
                    expires: DateTime.UtcNow.AddMinutes(_settings.AccessTokenExpirationMinutes),
                    signingCredentials: creds
                );

                var tokenString = _tokenHandler.WriteToken(token);
                _logger.LogInformation("Generated access token for user {UserId} with {RoleCount} roles",
                    userId, roles.Count());

                return tokenString;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating access token for user {UserId}", userId);
                throw;
            }
        }, cancellationToken);
    }

    /// <summary>
    /// Generates a refresh token (7 days expiration).
    /// </summary>
    /// <param name="userId">User ID (subject claim)</param>
    /// <param name="cancellationToken">Cancellation token (unused for synchronous operation)</param>
    /// <returns>JWT refresh token string</returns>
    /// <remarks>
    /// Refresh tokens have minimal claims:
    /// - sub: User ID (GUID)
    /// - exp: Expiration time (7 days from now)
    /// 
    /// Refresh tokens are stored in HttpOnly, Secure, SameSite=Strict cookies.
    /// </remarks>
    public async Task<string> GenerateRefreshTokenAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            try
            {
                var claims = new List<Claim>
                {
                    new Claim("sub", userId.ToString()),
                    new Claim("type", "refresh"),  // Distinguish from access tokens
                };

                var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.SecretKey));
                var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

                var token = new JwtSecurityToken(
                    issuer: _settings.Issuer,
                    audience: _settings.Audience,
                    claims: claims,
                    expires: DateTime.UtcNow.AddDays(_settings.RefreshTokenExpirationDays),
                    signingCredentials: creds
                );

                var tokenString = _tokenHandler.WriteToken(token);
                _logger.LogInformation("Generated refresh token for user {UserId}", userId);

                return tokenString;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating refresh token for user {UserId}", userId);
                throw;
            }
        }, cancellationToken);
    }

    /// <summary>
    /// Validates a JWT token and returns whether it's valid.
    /// </summary>
    /// <param name="token">JWT token to validate</param>
    /// <param name="cancellationToken">Cancellation token (unused for synchronous operation)</param>
    /// <returns>True if token is valid, false otherwise</returns>
    /// <remarks>
    /// Validation checks:
    /// - Token format is valid JWT
    /// - Signature is correct
    /// - Token is not expired
    /// - Issuer matches expected value
    /// - Audience matches expected value
    /// </remarks>
    public async Task<bool> ValidateTokenAsync(string token, CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            try
            {
                if (string.IsNullOrWhiteSpace(token))
                {
                    _logger.LogWarning("Token validation failed: token is null or empty");
                    return false;
                }

                var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.SecretKey));

                var validationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = key,
                    ValidateIssuer = true,
                    ValidIssuer = _settings.Issuer,
                    ValidateAudience = true,
                    ValidAudience = _settings.Audience,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromMinutes(0),
                };

                _tokenHandler.ValidateToken(token, validationParameters, out var validatedToken);

                _logger.LogDebug("Token validation successful");
                return true;
            }
            catch (SecurityTokenExpiredException ex)
            {
                _logger.LogWarning(ex, "Token validation failed: token expired");
                return false;
            }
            catch (SecurityTokenInvalidSignatureException ex)
            {
                _logger.LogWarning(ex, "Token validation failed: invalid signature");
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Token validation failed with exception");
                return false;
            }
        }, cancellationToken);
    }

    /// <summary>
    /// Extracts user ID from a JWT token without validation.
    /// </summary>
    /// <param name="token">JWT token</param>
    /// <returns>User ID extracted from "sub" claim</returns>
    /// <remarks>
    /// This method does NOT validate the token. Use ValidateTokenAsync before calling if validation is needed.
    /// Useful for extracting user ID from malformed tokens in error handling.
    /// </remarks>
    public Guid GetUserIdFromToken(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            throw new ArgumentException("Token cannot be null or empty", nameof(token));
        }

        try
        {
            var jwtToken = _tokenHandler.ReadJwtToken(token);
            var userIdClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == "sub");

            if (userIdClaim == null)
            {
                throw new InvalidOperationException("Token does not contain 'sub' (user ID) claim");
            }

            if (Guid.TryParse(userIdClaim.Value, out var userId))
            {
                return userId;
            }

            throw new InvalidOperationException($"User ID claim value '{userIdClaim.Value}' is not a valid GUID");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error extracting user ID from token");
            throw;
        }
    }

    /// <summary>
    /// Extracts roles from a JWT token without validation.
    /// </summary>
    /// <param name="token">JWT token</param>
    /// <returns>Enumerable of role strings (or empty if no roles)</returns>
    /// <remarks>
    /// Returns all claims with type "role". Returns empty enumerable if token has no roles.
    /// Does NOT validate the token.
    /// </remarks>
    public IEnumerable<string> GetRolesFromToken(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            _logger.LogWarning("GetRolesFromToken called with null or empty token");
            return Enumerable.Empty<string>();
        }

        try
        {
            var jwtToken = _tokenHandler.ReadJwtToken(token);
            var roleClaims = jwtToken.Claims
                .Where(c => c.Type == ClaimTypes.Role)
                .Select(c => c.Value);

            return roleClaims;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error extracting roles from token");
            return Enumerable.Empty<string>();
        }
    }

    /// <summary>
    /// Checks if access token needs automatic refresh (expires soon).
    /// </summary>
    /// <param name="token">JWT token</param>
    /// <returns>True if token expires in less than RefreshThresholdMinutes</returns>
    /// <remarks>
    /// Used by middleware to trigger auto-refresh before token expires.
    /// Default threshold is 5 minutes.
    /// </remarks>
    public bool NeedsRefresh(string token)
    {
        try
        {
            var jwtToken = _tokenHandler.ReadJwtToken(token);
            var expirationTime = jwtToken.ValidTo;
            var timeUntilExpiry = expirationTime - DateTime.UtcNow;

            return timeUntilExpiry.TotalMinutes < _settings.RefreshThresholdMinutes;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error checking if token needs refresh");
            return false;
        }
    }

    /// <summary>
    /// Gets token expiration timestamp.
    /// </summary>
    /// <param name="token">JWT token</param>
    /// <returns>DateTime when token expires (UTC)</returns>
    public DateTime GetTokenExpiration(string token)
    {
        try
        {
            var jwtToken = _tokenHandler.ReadJwtToken(token);
            return jwtToken.ValidTo;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error getting token expiration");
            throw;
        }
    }
}
