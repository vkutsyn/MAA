using AutoMapper;
using MAA.Application.Services;
using MAA.Application.Sessions.DTOs;
using MAA.Domain.Sessions;
using MAA.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace MAA.API.Controllers;

/// <summary>
/// Authentication API controller for user registration, login, token refresh, and logout.
/// Phase 5 feature: Registered user sessions with JWT tokens and max 3 concurrent sessions per user.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly ISessionService _sessionService;
    private readonly ITokenProvider _tokenProvider;
    private readonly IMapper _mapper;
    private readonly ILogger<AuthController> _logger;
    private readonly SessionContext _dbContext;
    private readonly IWebHostEnvironment _environment;
    
    private const int MaxConcurrentSessions = 3;
    private const string RefreshTokenCookieName = "refreshToken";

    public AuthController(
        ISessionService sessionService,
        ITokenProvider tokenProvider,
        IMapper mapper,
        SessionContext dbContext,
        ILogger<AuthController> logger,
        IWebHostEnvironment environment)
    {
        _sessionService = sessionService ?? throw new ArgumentNullException(nameof(sessionService));
        _tokenProvider = tokenProvider ?? throw new ArgumentNullException(nameof(tokenProvider));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _environment = environment ?? throw new ArgumentNullException(nameof(environment));
    }

    /// <summary>
    /// POST /api/auth/register
    /// Registers a new user account.
    /// Returns: 201 Created with user ID
    ///          400 Bad Request if validation fails
    ///          409 Conflict if email already registered
    /// </summary>
    [HttpPost("register")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult> Register(
        [FromBody] RegisterRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            _logger.LogInformation("User registration request for email {Email}", 
                request.Email);

            var normalizedEmail = request.Email?.Trim().ToLowerInvariant();
            if (string.IsNullOrWhiteSpace(normalizedEmail))
            {
                return BadRequest(new { error = "Email is required" });
            }

            // Validate password strength
            if (string.IsNullOrWhiteSpace(request.Password) || request.Password.Length < 8)
            {
                return BadRequest(new { error = "Password must be at least 8 characters" });
            }

            var existingUser = await _dbContext.Users
                .AsNoTracking()
                .AnyAsync(u => u.Email == normalizedEmail, cancellationToken);
            if (existingUser)
            {
                return Conflict(new { error = "Email is already registered" });
            }

            var userId = Guid.NewGuid();
            var user = new User
            {
                Id = userId,
                Email = normalizedEmail,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                Role = UserRole.User,
                EmailVerified = false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                Version = 1
            };

            _dbContext.Users.Add(user);
            await _dbContext.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("User registered successfully: {UserId}", userId);

            return Created($"/api/auth/users/{userId}", new { userId = userId });
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("already exists"))
        {
            return Conflict(new { error = "Email is already registered" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error registering user");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// POST /api/auth/login
    /// Authenticates user and returns JWT tokens.
    /// 
    /// Returns: 200 Ok with access token and refresh token in cookie
    ///          400 Bad Request if validation fails
    ///          401 Unauthorized if credentials invalid
    ///          409 Conflict if user has max 3 concurrent sessions (with active session list)
    /// </summary>
    [HttpPost("login")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult> Login(
        [FromBody] LoginRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            _logger.LogInformation("Login attempt for email {Email}", request.Email);

            var normalizedEmail = request.Email?.Trim().ToLowerInvariant();
            if (string.IsNullOrWhiteSpace(normalizedEmail))
            {
                return Unauthorized(new { error = "Invalid email or password" });
            }

            var user = await _dbContext.Users.FirstOrDefaultAsync(
                u => u.Email == normalizedEmail,
                cancellationToken);

            if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            {
                return Unauthorized(new { error = "Invalid email or password" });
            }

            var userId = user.Id;
            var userRoles = new[] { user.Role.ToString() };

            // Check max concurrent sessions
            var activeSessions = await _sessionService.GetActiveSessionsAsync(userId, cancellationToken);
            if (activeSessions.Count >= MaxConcurrentSessions)
            {
                return Conflict(new
                {
                    error = "You're logged in on 3 devices. End another session to continue?",
                    activeSessions = activeSessions.Select(s => new
                    {
                        sessionId = s.Id,
                        device = "Unknown",
                        ipAddress = s.IpAddress,
                        loginTime = s.CreatedAt
                    }).ToList()
                });
            }

            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "0.0.0.0";
            var userAgent = HttpContext.Request.Headers["User-Agent"].ToString();
            if (string.IsNullOrWhiteSpace(userAgent))
            {
                userAgent = "Unknown";
            }

            var session = await _sessionService.CreateAuthenticatedSessionAsync(
                userId, ipAddress, userAgent, cancellationToken);

            // Generate tokens
            var accessToken = await _tokenProvider.GenerateAccessTokenAsync(
                userId,
                userRoles,
                session.Id,
                cancellationToken);
            var refreshToken = await _tokenProvider.GenerateRefreshTokenAsync(userId, cancellationToken);

            // Set refresh token in HttpOnly secure cookie
            var cookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Secure = !_environment.IsDevelopment(),  // HTTPS only in production
                SameSite = SameSiteMode.Strict,
                Expires = DateTimeOffset.UtcNow.AddDays(7),
                IsEssential = true
            };

            Response.Cookies.Append(RefreshTokenCookieName, refreshToken, cookieOptions);

            _logger.LogInformation("User logged in successfully: {UserId}", userId);

            return Ok(new
            {
                accessToken = accessToken,
                tokenType = "Bearer",
                expiresIn = 3600,  // 1 hour in seconds
                refreshToken = refreshToken  // Also in response for some clients
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during login");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// POST /api/auth/refresh
    /// Refreshes access token using refresh token.
    /// 
    /// Returns: 200 Ok with new access token
    ///          401 Unauthorized if refresh token invalid/expired
    ///          400 Bad Request if refresh token not provided
    /// </summary>
    [HttpPost("refresh")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult> Refresh(
        [FromBody] RefreshTokenRequest? request,
        CancellationToken cancellationToken)
    {
        try
        {
            // Get refresh token from cookie (preferred) or body
            var refreshToken = request?.RefreshToken ?? 
                               HttpContext.Request.Cookies[RefreshTokenCookieName];

            if (string.IsNullOrWhiteSpace(refreshToken))
            {
                return BadRequest(new { error = "Refresh token is required" });
            }

            // Validate refresh token
            var isValid = await _tokenProvider.ValidateTokenAsync(refreshToken, cancellationToken);
            if (!isValid)
            {
                return Unauthorized(new { error = "Invalid or expired refresh token" });
            }

            // Extract user ID from refresh token
            var userId = _tokenProvider.GetUserIdFromToken(refreshToken);

            // Get user roles from database
            // var user = await _userService.GetUserAsync(userId, cancellationToken);
            // var roles = new[] { user.Role.ToString() };
            var roles = new[] { UserRole.User.ToString() };  // Demo

            // Generate new access token
            var newAccessToken = await _tokenProvider.GenerateAccessTokenAsync(userId, roles, null, cancellationToken);

            _logger.LogInformation("Token refreshed for user {UserId}", userId);

            return Ok(new
            {
                accessToken = newAccessToken,
                tokenType = "Bearer",
                expiresIn = 3600  // 1 hour in seconds
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error refreshing token");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// POST /api/auth/logout
    /// Logs out user by revoking current session and clearing refresh token.
    /// 
    /// Requires: Authorization header with Bearer token
    /// Returns: 200 Ok with refresh token cleared
    ///          401 Unauthorized if not authenticated
    /// </summary>
    [Authorize]
    [HttpPost("logout")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult> Logout(CancellationToken cancellationToken)
    {
        try
        {
            // Get access token from headers
            var authHeader = HttpContext.Request.Headers["Authorization"].ToString();
            if (string.IsNullOrWhiteSpace(authHeader) || !authHeader.StartsWith("Bearer "))
            {
                return Unauthorized(new { error = "Missing authorization token" });
            }

            var accessToken = authHeader.Replace("Bearer ", string.Empty);

            Guid userId;
            try
            {
                userId = _tokenProvider.GetUserIdFromToken(accessToken);
            }
            catch (Exception)
            {
                return Unauthorized(new { error = "Invalid user context" });
            }

            var handler = new JwtSecurityTokenHandler();
            var jwt = handler.ReadJwtToken(accessToken);
            var sessionIdClaim = jwt.Claims.FirstOrDefault(c => c.Type == "sessionId")?.Value;

            if (string.IsNullOrWhiteSpace(sessionIdClaim) || !Guid.TryParse(sessionIdClaim, out var sessionId))
            {
                return Unauthorized(new { error = "Invalid session context" });
            }

            await _sessionService.RevokeSessionAsync(sessionId, cancellationToken);

            // Clear refresh token cookie
            Response.Cookies.Delete(RefreshTokenCookieName);

            _logger.LogInformation("User logged out: {UserId}", userId);

            return Ok(new { message = "Logged out successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during logout");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// GET /api/auth/sessions
    /// Lists all active sessions for current user.
    /// 
    /// Requires: Authorization header with Bearer token
    /// Returns: 200 Ok with list of active sessions
    ///          401 Unauthorized if not authenticated
    /// </summary>
    [Authorize]
    [HttpGet("sessions")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult> ListSessions(CancellationToken cancellationToken)
    {
        try
        {
            var authHeader = HttpContext.Request.Headers["Authorization"].ToString();
            if (string.IsNullOrWhiteSpace(authHeader) || !authHeader.StartsWith("Bearer "))
            {
                return Unauthorized(new { error = "Missing authorization token" });
            }

            var accessToken = authHeader.Replace("Bearer ", string.Empty);
            Guid userId;
            try
            {
                userId = _tokenProvider.GetUserIdFromToken(accessToken);
            }
            catch (Exception)
            {
                return Unauthorized(new { error = "Invalid user context" });
            }

            var sessions = await _sessionService.GetActiveSessionsAsync(userId, cancellationToken);
            var sessionDtos = sessions.Select(s => new SessionInfo
            {
                SessionId = s.Id,
                Device = "Unknown",
                IpAddress = s.IpAddress,
                LoginTime = s.CreatedAt
            }).ToList();

            return Ok(new { sessions = sessionDtos });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error listing sessions");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// DELETE /api/auth/sessions/{sessionId}
    /// Revokes a specific session remotely.
    /// 
    /// Requires: Authorization header with Bearer token
    /// Returns: 200 Ok if session revoked
    ///          401 Unauthorized if not authenticated
    ///          404 Not Found if session doesn't exist or doesn't belong to user
    /// </summary>
    [Authorize]
    [HttpDelete("sessions/{sessionId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> RevokeSession(
        Guid sessionId,
        CancellationToken cancellationToken)
    {
        try
        {
            var authHeader = HttpContext.Request.Headers["Authorization"].ToString();
            if (string.IsNullOrWhiteSpace(authHeader) || !authHeader.StartsWith("Bearer "))
            {
                return Unauthorized(new { error = "Missing authorization token" });
            }

            var accessToken = authHeader.Replace("Bearer ", string.Empty);
            Guid userId;
            try
            {
                userId = _tokenProvider.GetUserIdFromToken(accessToken);
            }
            catch (Exception)
            {
                return Unauthorized(new { error = "Invalid user context" });
            }

            var session = await _dbContext.Sessions
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.Id == sessionId, cancellationToken);

            if (session == null || session.UserId != userId)
            {
                return NotFound(new { error = "Session not found" });
            }

            await _sessionService.RevokeSessionAsync(sessionId, cancellationToken);

            _logger.LogInformation("Session revoked for user {UserId}: {SessionId}", userId, sessionId);

            return Ok(new { message = "Session revoked" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error revoking session");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }
}

/// <summary>
/// Request models for auth endpoints.
/// </summary>
public class RegisterRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
}

public class LoginRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class RefreshTokenRequest
{
    public string RefreshToken { get; set; } = string.Empty;
}

public class SessionInfo
{
    public Guid SessionId { get; set; }
    public string Device { get; set; } = string.Empty;
    public string IpAddress { get; set; } = string.Empty;
    public DateTime LoginTime { get; set; }
}
