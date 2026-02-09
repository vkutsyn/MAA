using MAA.Application.Services;
using MAA.Domain.Exceptions;
using Microsoft.AspNetCore.Http;
using System.Security.Cryptography;

namespace MAA.API.Middleware;

/// <summary>
/// Session validation middleware for anonymous user sessions.
/// Validates session cookies, checks timeouts (30-minute absolute + sliding inactivity),
/// and returns HTTP 401 with CONST-III message if expired.
/// Applied to all requests except /health/* and /api/auth/* (Phase 5).
/// </summary>
public class SessionMiddleware
{
    // CONST-III: Exact error message for session expiration per Constitution compliance
    private const string SessionExpiredMessage = "Your session expired after 30 minutes. Start a new eligibility check.";
    private const string SessionCookieName = "MAA_SessionId";

    private readonly RequestDelegate _next;
    private readonly ILogger<SessionMiddleware> _logger;

    // Paths that bypass session validation
    private static readonly string[] BypassPaths = new[]
    {
        "/health",
        "/api/auth",
        "/api-docs",
        "/swagger"
    };

    public SessionMiddleware(RequestDelegate next, ILogger<SessionMiddleware> logger)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Validates session cookie on each incoming HTTP request.
    /// Sliding window: updates inactivity timeout on successful validation.
    /// </summary>
    public async Task InvokeAsync(HttpContext context, ISessionService sessionService)
    {
        // Skip validation for bypass paths (health checks, auth, docs)
        if (ShouldBypassSessionValidation(context.Request.Path))
        {
            await _next(context);
            return;
        }

        // Skip GET /api/sessions/* (session creation is public)
        if (context.Request.Method == HttpMethods.Post && context.Request.Path.StartsWithSegments("/api/sessions"))
        {
            await _next(context);
            return;
        }

        try
        {
            // Extract session ID from cookie
            if (!context.Request.Cookies.TryGetValue(SessionCookieName, out var sessionIdString))
            {
                _logger.LogInformation("Request missing session cookie: {Path}", context.Request.Path);
                ReturnSessionExpired(context);
                return;
            }

            if (!Guid.TryParse(sessionIdString, out var sessionId))
            {
                _logger.LogWarning("Invalid session ID format in cookie: {SessionId}", sessionIdString);
                ReturnSessionExpired(context);
                return;
            }

            // Validate session (checks expiry, inactivity, revoked status)
            var isValid = await sessionService.ValidateSessionAsync(sessionId);
            if (!isValid)
            {
                _logger.LogWarning(
                    "Session validation failed for {SessionId}: Session expired or invalid",
                    sessionId);
                ReturnSessionExpired(context);
                return;
            }

            // Update inactivity timeout (sliding window)
            await sessionService.ResetInactivityTimeoutAsync(sessionId);
            _logger.LogDebug("Session {SessionId} inactivity timeout reset", sessionId);

            // Store session ID in context for downstream use
            context.Items["SessionId"] = sessionId;

            // Proceed to next middleware
            await _next(context);
        }
        catch (ValidationException ex)
        {
            _logger.LogWarning("Validation error in SessionMiddleware: {Message}", ex.Message);
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsJsonAsync(new { error = "Invalid request", details = ex.Errors });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error in SessionMiddleware");
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsJsonAsync(new { error = "Session validation error" });
        }
    }

    /// <summary>
    /// Checks if the request path should bypass session validation.
    /// </summary>
    private static bool ShouldBypassSessionValidation(PathString path)
    {
        var pathValue = path.Value ?? string.Empty;
        return BypassPaths.Any(bypassPath => pathValue.StartsWith(bypassPath, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Returns HTTP 401 with CONST-III compliant error message for expired sessions.
    /// </summary>
    private static void ReturnSessionExpired(HttpContext context)
    {
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        context.Response.ContentType = "application/json";

        // CONST-III: Actionable, clear error message
        var errorResponse = new
        {
            error = "Unauthorized",
            message = SessionExpiredMessage,
            timestamp = DateTime.UtcNow
        };

        context.Response.WriteAsJsonAsync(errorResponse).ConfigureAwait(false).GetAwaiter().GetResult();
    }
}

/// <summary>
/// Extension methods for adding SessionMiddleware to the request pipeline.
/// </summary>
public static class SessionMiddlewareExtensions
{
    /// <summary>
    /// Adds session validation middleware to the request pipeline.
    /// Must be added before routing middleware.
    /// </summary>
    public static IApplicationBuilder UseSessionMiddleware(this IApplicationBuilder app)
    {
        return app.UseMiddleware<SessionMiddleware>();
    }
}
