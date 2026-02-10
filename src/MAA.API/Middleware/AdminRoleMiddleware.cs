using MAA.Domain.Sessions;
using System.Text.Json;

namespace MAA.API.Middleware;

/// <summary>
/// Middleware to enforce role-based access control for admin endpoints.
/// US3: Admin endpoints only accessible to Admin, Reviewer, or Analyst roles.
/// Phase 1 Implementation: Checks X-User-Role header (stub for Phase 5 JWT claims).
/// Phase 5: Replace header check with JWT token validation and claims extraction.
/// </summary>
public class AdminRoleMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<AdminRoleMiddleware> _logger;

    public AdminRoleMiddleware(RequestDelegate next, ILogger<AdminRoleMiddleware> logger)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Invokes middleware to check role authorization for admin paths.
    /// </summary>
    /// <param name="context">HTTP context</param>
    public async Task InvokeAsync(HttpContext context)
    {
        // Only apply to /api/admin/* paths
        if (!context.Request.Path.StartsWithSegments("/api/admin"))
        {
            await _next(context);
            return;
        }

        // Phase 1: Extract role from X-User-Role header (stub for JWT claims)
        // TODO Phase 5 (T038-T042): Replace with JWT token validation
        var roleHeader = context.Request.Headers["X-User-Role"].ToString();

        if (string.IsNullOrWhiteSpace(roleHeader))
        {
            // US3 Acceptance Scenario 1: No authorization → 403 Forbidden
            _logger.LogWarning("Admin endpoint access denied: No authorization header. Path: {Path}",
                context.Request.Path);

            await WriteForbiddenResponse(context, "Missing authorization. Admin access required.");
            return;
        }

        // Parse role
        var role = UserRoleExtensions.ParseRole(roleHeader);

        // Check if role has admin privileges (Analyst, Reviewer, or Admin)
        if (!role.IsAdminRole())
        {
            // US3 Acceptance Scenario 2: Insufficient role → 403 Forbidden
            _logger.LogWarning("Admin endpoint access denied: Insufficient permissions. Role: {Role}, Path: {Path}",
                role, context.Request.Path);

            await WriteForbiddenResponse(context, "Insufficient permissions. Admin, Reviewer, or Analyst role required.");
            return;
        }

        // US3 Acceptance Scenario 3: Valid admin role → proceed
        _logger.LogInformation("Admin endpoint access granted. Role: {Role}, Path: {Path}",
            role, context.Request.Path);

        await _next(context);
    }

    /// <summary>
    /// Writes 403 Forbidden response with JSON problem details.
    /// </summary>
    private static async Task WriteForbiddenResponse(HttpContext context, string detail)
    {
        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        context.Response.ContentType = "application/problem+json";

        var problemDetails = new
        {
            type = "https://tools.ietf.org/html/rfc7231#section-6.5.3",
            title = "Forbidden",
            status = 403,
            detail = detail,
            instance = context.Request.Path.ToString()
        };

        var json = JsonSerializer.Serialize(problemDetails, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        await context.Response.WriteAsync(json);
    }
}
