using FluentAssertions;
using MAA.API.Middleware;
using MAA.Domain.Sessions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace MAA.Tests.Unit.Middleware;

/// <summary>
/// Unit tests for AdminRoleMiddleware.
/// Tests US3: Role-Based Access Control requirements.
/// Validates that admin endpoints are protected and only accessible to authorized roles.
/// CONST-I: Testable in isolation without database calls.
/// </summary>
[Trait("Category", "Unit")]
public class AdminRoleMiddlewareTests
{
    private readonly Mock<ILogger<AdminRoleMiddleware>> _loggerMock;
    private readonly Mock<RequestDelegate> _nextMock;

    public AdminRoleMiddlewareTests()
    {
        _loggerMock = new Mock<ILogger<AdminRoleMiddleware>>();
        _nextMock = new Mock<RequestDelegate>();
    }

    /// <summary>
    /// US3 Acceptance Scenario 1: No authorization header → 403 Forbidden.
    /// </summary>
    [Fact]
    public async Task InvokeAsync_NoAuthorizationHeader_Returns403Forbidden()
    {
        // Arrange
        var middleware = new AdminRoleMiddleware(_nextMock.Object, _loggerMock.Object);
        var context = new DefaultHttpContext();
        context.Request.Path = "/api/admin/rules";
        context.Request.Method = "POST";
        // No authorization header set

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        context.Response.StatusCode.Should().Be(StatusCodes.Status403Forbidden,
            "requests without authorization should be forbidden");

        _nextMock.Verify(next => next(It.IsAny<HttpContext>()), Times.Never,
            "next middleware should not be invoked for unauthorized requests");
    }

    /// <summary>
    /// US3 Acceptance Scenario 2: Valid token but role="User" → 403 Forbidden.
    /// </summary>
    [Fact]
    public async Task InvokeAsync_UserRole_Returns403Forbidden()
    {
        // Arrange
        var middleware = new AdminRoleMiddleware(_nextMock.Object, _loggerMock.Object);
        var context = new DefaultHttpContext();
        context.Request.Path = "/api/admin/rules";
        context.Request.Method = "GET";
        context.Request.Headers["X-User-Role"] = "User"; // Stub for JWT claim in Phase 1

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        context.Response.StatusCode.Should().Be(StatusCodes.Status403Forbidden,
            "regular users should not access admin endpoints");

        _nextMock.Verify(next => next(It.IsAny<HttpContext>()), Times.Never,
            "next middleware should not be invoked for insufficient permissions");
    }

    /// <summary>
    /// US3 Acceptance Scenario 2: Anonymous role → 403 Forbidden.
    /// </summary>
    [Fact]
    public async Task InvokeAsync_AnonymousRole_Returns403Forbidden()
    {
        // Arrange
        var middleware = new AdminRoleMiddleware(_nextMock.Object, _loggerMock.Object);
        var context = new DefaultHttpContext();
        context.Request.Path = "/api/admin/rules";
        context.Request.Method = "GET";
        context.Request.Headers["X-User-Role"] = "Anonymous";

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        context.Response.StatusCode.Should().Be(StatusCodes.Status403Forbidden,
            "anonymous users should not access admin endpoints");
    }

    /// <summary>
    /// US3 Acceptance Scenario 3: Valid token with role="Admin" → 200 OK (proceeds to next).
    /// </summary>
    [Fact]
    public async Task InvokeAsync_AdminRole_ProceedsToNextMiddleware()
    {
        // Arrange
        var middleware = new AdminRoleMiddleware(_nextMock.Object, _loggerMock.Object);
        var context = new DefaultHttpContext();
        context.Request.Path = "/api/admin/rules";
        context.Request.Method = "POST";
        context.Request.Headers["X-User-Role"] = "Admin";

        _nextMock.Setup(next => next(It.IsAny<HttpContext>()))
            .Returns(Task.CompletedTask);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        _nextMock.Verify(next => next(context), Times.Once,
            "Admin role should allow access to admin endpoints");

        // Status code should not be set by middleware (controller sets it)
        context.Response.StatusCode.Should().Be(200, "default status code when middleware passes through");
    }

    /// <summary>
    /// US3 Acceptance Scenario 4: Reviewer role can access approval queue.
    /// </summary>
    [Fact]
    public async Task InvokeAsync_ReviewerRole_ProceedsToNextMiddleware()
    {
        // Arrange
        var middleware = new AdminRoleMiddleware(_nextMock.Object, _loggerMock.Object);
        var context = new DefaultHttpContext();
        context.Request.Path = "/api/admin/approval-queue";
        context.Request.Method = "GET";
        context.Request.Headers["X-User-Role"] = "Reviewer";

        _nextMock.Setup(next => next(It.IsAny<HttpContext>()))
            .Returns(Task.CompletedTask);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        _nextMock.Verify(next => next(context), Times.Once,
            "Reviewer role should allow access to admin endpoints");
    }

    /// <summary>
    /// US3 Acceptance Scenario 4: Analyst role can access analytics endpoints.
    /// </summary>
    [Fact]
    public async Task InvokeAsync_AnalystRole_ProceedsToNextMiddleware()
    {
        // Arrange
        var middleware = new AdminRoleMiddleware(_nextMock.Object, _loggerMock.Object);
        var context = new DefaultHttpContext();
        context.Request.Path = "/api/admin/analytics";
        context.Request.Method = "GET";
        context.Request.Headers["X-User-Role"] = "Analyst";

        _nextMock.Setup(next => next(It.IsAny<HttpContext>()))
            .Returns(Task.CompletedTask);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        _nextMock.Verify(next => next(context), Times.Once,
            "Analyst role should allow access to admin endpoints");
    }

    /// <summary>
    /// Non-admin paths should bypass middleware (public endpoints).
    /// </summary>
    [Fact]
    public async Task InvokeAsync_NonAdminPath_ProceedsWithoutCheck()
    {
        // Arrange
        var middleware = new AdminRoleMiddleware(_nextMock.Object, _loggerMock.Object);
        var context = new DefaultHttpContext();
        context.Request.Path = "/api/sessions"; // Public endpoint
        context.Request.Method = "POST";
        // No role header needed for public endpoints

        _nextMock.Setup(next => next(It.IsAny<HttpContext>()))
            .Returns(Task.CompletedTask);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        _nextMock.Verify(next => next(context), Times.Once,
            "non-admin paths should not require authorization");
    }

    /// <summary>
    /// Invalid role string → 403 Forbidden.
    /// </summary>
    [Fact]
    public async Task InvokeAsync_InvalidRoleString_Returns403Forbidden()
    {
        // Arrange
        var middleware = new AdminRoleMiddleware(_nextMock.Object, _loggerMock.Object);
        var context = new DefaultHttpContext();
        context.Request.Path = "/api/admin/rules";
        context.Request.Method = "GET";
        context.Request.Headers["X-User-Role"] = "InvalidRole";

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        context.Response.StatusCode.Should().Be(StatusCodes.Status403Forbidden,
            "invalid roles should be rejected");
    }

    /// <summary>
    /// Case-insensitive role matching.
    /// </summary>
    [Fact]
    public async Task InvokeAsync_RoleCaseInsensitive_ProceedsCorrectly()
    {
        // Arrange
        var middleware = new AdminRoleMiddleware(_nextMock.Object, _loggerMock.Object);
        var context = new DefaultHttpContext();
        context.Request.Path = "/api/admin/rules";
        context.Request.Method = "GET";
        context.Request.Headers["X-User-Role"] = "admin"; // lowercase

        _nextMock.Setup(next => next(It.IsAny<HttpContext>()))
            .Returns(Task.CompletedTask);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        _nextMock.Verify(next => next(context), Times.Once,
            "role matching should be case-insensitive");
    }
}
