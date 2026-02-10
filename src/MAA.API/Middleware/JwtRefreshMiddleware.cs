using MAA.Application.Services;
using Microsoft.AspNetCore.Http;

namespace MAA.API.Middleware;

/// <summary>
/// JWT token auto-refresh middleware for authenticated sessions (Phase 5).
/// Checks if access token is near expiration (<5 minutes) and automatically refreshes it.
/// Uses refresh token stored in HttpOnly cookie.
/// Transparent to client - new access token automatically returned in response header.
/// Applied to all requests for authenticated users (paths with Authorization: Bearer header).
/// </summary>
public class JwtRefreshMiddleware
{
    private const string RefreshTokenCookieName = "refreshToken";
    private const string AuthorizationHeader = "Authorization";
    private const string BearerScheme = "Bearer ";

    private readonly RequestDelegate _next;
    private readonly ILogger<JwtRefreshMiddleware> _logger;

    // Paths that bypass token refresh
    private static readonly string[] BypassPaths = new[]
    {
        "/health",
        "/api/auth/login",
        "/api/auth/register",
        "/api/auth/refresh",
        "/api/auth/logout",
        "/api-docs",
        "/swagger"
    };

    public JwtRefreshMiddleware(RequestDelegate next, ILogger<JwtRefreshMiddleware> logger)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Checks access token expiration and auto-refreshes if needed.
    /// </summary>
    public async Task InvokeAsync(HttpContext context, ITokenProvider tokenProvider)
    {
        // Skip certain paths
        if (ShouldBypassRefresh(context.Request.Path))
        {
            await _next(context);
            return;
        }

        try
        {
            // Extract access token from Authorization header
            var authHeader = context.Request.Headers[AuthorizationHeader].ToString();

            if (string.IsNullOrWhiteSpace(authHeader) || !authHeader.StartsWith(BearerScheme))
            {
                // No access token, proceed normally (could be anonymous session or public endpoint)
                await _next(context);
                return;
            }

            var accessToken = authHeader.Substring(BearerScheme.Length);

            // Check if token needs refresh
            if (!tokenProvider.NeedsRefresh(accessToken))
            {
                // Token is still valid, proceed normally
                await _next(context);
                return;
            }

            _logger.LogInformation("Access token needs refresh (expires soon)");

            // Get refresh token from cookie
            if (!context.Request.Cookies.TryGetValue(RefreshTokenCookieName, out var refreshToken))
            {
                _logger.LogWarning("Access token expired but no refresh token found in cookies");
                // Let other middleware handle 401 response
                await _next(context);
                return;
            }

            // Validate refresh token
            var isRefreshTokenValid = await tokenProvider.ValidateTokenAsync(refreshToken);
            if (!isRefreshTokenValid)
            {
                _logger.LogWarning("Refresh token is invalid or expired");
                // Let other middleware handle 401 response
                await _next(context);
                return;
            }

            // Extract user ID and roles from refresh token
            var userId = tokenProvider.GetUserIdFromToken(refreshToken);
            var roles = tokenProvider.GetRolesFromToken(refreshToken).ToList();

            // Generate new access token
            var newAccessToken = await tokenProvider.GenerateAccessTokenAsync(userId, roles);

            _logger.LogInformation("Access token auto-refreshed for user {UserId}", userId);

            // Store new token in context for response header injection
            context.Items["NewAccessToken"] = newAccessToken;
            context.Items["AccessTokenRefreshed"] = true;

            // Update Authorization header for downstream handlers
            context.Request.Headers[AuthorizationHeader] = $"{BearerScheme}{newAccessToken}";

            // Capture original response stream
            var originalBody = context.Response.Body;
            using (var memoryStream = new MemoryStream())
            {
                context.Response.Body = memoryStream;

                try
                {
                    // Proceed to next middleware
                    await _next(context);

                    // Add new access token to response header
                    if (context.Items.ContainsKey("AccessTokenRefreshed") &&
                        context.Items["AccessTokenRefreshed"] is true)
                    {
                        context.Response.Headers["X-New-Access-Token"] = newAccessToken;
                        context.Response.Headers["X-Token-Refreshed"] = "true";

                        _logger.LogDebug("Added X-New-Access-Token header to response");
                    }

                    // Copy response stream back
                    await memoryStream.CopyToAsync(originalBody);
                }
                finally
                {
                    context.Response.Body = originalBody;
                }
            }

            return;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in JwtRefreshMiddleware");
            // Continue normally, let error handling middleware deal with it
            await _next(context);
        }
    }

    /// <summary>
    /// Determines if request should bypass token refresh.
    /// </summary>
    private bool ShouldBypassRefresh(PathString path)
    {
        return BypassPaths.Any(bypassPath =>
            path.StartsWithSegments(bypassPath, StringComparison.OrdinalIgnoreCase));
    }
}

/// <summary>
/// Extension methods for registering JwtRefreshMiddleware.
/// </summary>
public static class JwtRefreshMiddlewareExtensions
{
    /// <summary>
    /// Registers JwtRefreshMiddleware in the request pipeline.
    /// Should be called AFTER authentication but BEFORE endpoint routing.
    /// </summary>
    /// <param name="app">Application builder</param>
    /// <returns>Application builder for chaining</returns>
    public static IApplicationBuilder UseJwtRefreshMiddleware(this IApplicationBuilder app)
    {
        return app.UseMiddleware<JwtRefreshMiddleware>();
    }
}
