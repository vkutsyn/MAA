using AutoMapper;
using MAA.Application.Services;
using MAA.Application.Sessions.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MAA.API.Controllers;

/// <summary>
/// Sessions API controller for anonymous user session management.
/// Handles session creation, retrieval, and validation.
/// User Story 1: Anonymous User Session with 30-minute timeout.
/// </summary>
/// <remarks>
/// Authentication: All endpoints require JWT bearer token authentication.
/// Include token in Authorization header: Authorization: Bearer {token}
/// </remarks>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SessionsController : ControllerBase
{
    private readonly ISessionService _sessionService;
    private readonly IMapper _mapper;
    private readonly ILogger<SessionsController> _logger;

    // CONST-III: Exact error message for session expiration
    private const string SessionExpiredMessage = "Your session expired after 30 minutes. Start a new eligibility check.";

    public SessionsController(
        ISessionService sessionService,
        IMapper mapper,
        ILogger<SessionsController> logger)
    {
        _sessionService = sessionService ?? throw new ArgumentNullException(nameof(sessionService));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// POST /api/sessions
    /// Creates a new anonymous session.
    /// Returns: 201 Created with SessionDto
    /// </summary>
    /// <param name="request">Session creation request with IP and user agent</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>201 Created with SessionDto</returns>
    [HttpPost]
    [ProducesResponseType(typeof(SessionDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<SessionDto>> CreateSession(
        [FromBody] CreateSessionDto request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            // Get IP address from request if not provided
            var ipAddress = request.IpAddress ?? HttpContext.Connection.RemoteIpAddress?.ToString() ?? "0.0.0.0";
            var userAgent = request.UserAgent ?? HttpContext.Request.Headers["User-Agent"].ToString() ?? "Unknown";

            _logger.LogInformation(
                "Creating session for IP {IpAddress}",
                ipAddress);

            var session = await _sessionService.CreateSessionAsync(ipAddress, userAgent, cancellationToken);

            var sessionDto = _mapper.Map<SessionDto>(session);

            return CreatedAtAction(
                nameof(GetSessionById),
                new { id = session.Id },
                sessionDto);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning($"Invalid session creation request: {ex.Message}");
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error creating session: {ex.Message}");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// GET /api/sessions/{id}
    /// Retrieves a session by ID.
    /// Returns: 200 Ok with SessionDto for valid session
    ///          401 Unauthorized if session expired/inactive with CONST-III message
    ///          404 Not Found if session doesn't exist
    /// </summary>
    /// <param name="id">Session ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>SessionDto or error response</returns>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(SessionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<SessionDto>> GetSessionById(
        Guid id,
        CancellationToken cancellationToken)
    {
        try
        {
            // First check if session is valid
            var isValid = await _sessionService.ValidateSessionAsync(id, cancellationToken);

            if (!isValid)
            {
                // Session is expired, inactive, revoked, or not found
                // Return 401 with CONST-III compliant error message
                _logger.LogWarning(
                    "Session {SessionId} is not valid (expired, inactive, revoked, or not found)",
                    id);

                return Unauthorized(new
                {
                    type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
                    title = "Session Expired",
                    status = 401,
                    detail = SessionExpiredMessage,
                    traceId = HttpContext.TraceIdentifier
                });
            }

            // Session is valid - retrieve and return it
            var session = await _sessionService.GetSessionAsync(id, cancellationToken);
            var sessionDto = _mapper.Map<SessionDto>(session);

            _logger.LogInformation("Retrieved session {SessionId}", id);

            return Ok(sessionDto);
        }
        catch (InvalidOperationException)
        {
            // Session not found or not valid
            _logger.LogWarning("Session {SessionId} not found or invalid", id);

            return NotFound(new
            {
                type = "https://tools.ietf.org/html/rfc7231#section-6.5.4",
                title = "Session Not Found",
                status = 404,
                detail = $"Session with ID {id} not found",
                traceId = HttpContext.TraceIdentifier
            });
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error retrieving session: {ex.Message}");

            return StatusCode(500, new
            {
                type = "https://tools.ietf.org/html/rfc7231#section-6.6.1",
                title = "Internal Server Error",
                status = 500,
                detail = "An error occurred while processing the request",
                traceId = HttpContext.TraceIdentifier
            });
        }
    }

    /// <summary>
    /// GET /api/sessions/{id}/status
    /// Gets the current status of a session (valid, expired, etc).
    /// Returns: 200 Ok with status information
    ///          401 Unauthorized if expired
    ///          404 Not Found if not found
    /// </summary>
    /// <param name="id">Session ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Status information</returns>
    [HttpGet("{id:guid}/status")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<object>> GetSessionStatus(
        Guid id,
        CancellationToken cancellationToken)
    {
        try
        {
            var isValid = await _sessionService.ValidateSessionAsync(id, cancellationToken);

            if (!isValid)
            {
                return Unauthorized(new { message = SessionExpiredMessage });
            }

            var session = await _sessionService.GetSessionAsync(id, cancellationToken);
            var sessionDto = _mapper.Map<SessionDto>(session);

            return Ok(new
            {
                sessionId = session.Id,
                isValid = sessionDto.IsValid,
                minutesUntilExpiry = sessionDto.MinutesUntilExpiry,
                minutesUntilInactivity = sessionDto.MinutesUntilInactivityTimeout,
                expiresAt = sessionDto.ExpiresAt,
                inactivityTimeoutAt = sessionDto.InactivityTimeoutAt
            });
        }
        catch (InvalidOperationException)
        {
            return NotFound(new { message = "Session not found" });
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error getting session status: {ex.Message}");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }
}
