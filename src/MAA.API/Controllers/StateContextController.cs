using MAA.Application.StateContext;
using MAA.Application.StateContext.Commands;
using MAA.Application.StateContext.DTOs;
using MAA.Application.StateContext.Queries;
using MAA.Application.StateContext.Validators;
using Microsoft.AspNetCore.Mvc;

namespace MAA.API.Controllers;

/// <summary>
/// Controller for state context initialization and management
/// </summary>
[ApiController]
[Route("api/state-context")]
public class StateContextController : ControllerBase
{
    private readonly InitializeStateContextHandler _initializeHandler;
    private readonly UpdateStateContextHandler _updateHandler;
    private readonly GetStateContextHandler _getHandler;
    private readonly InitializeStateContextRequestValidator _initValidator;
    private readonly UpdateStateContextRequestValidator _updateValidator;
    private readonly ILogger<StateContextController> _logger;

    public StateContextController(
        InitializeStateContextHandler initializeHandler,
        UpdateStateContextHandler updateHandler,
        GetStateContextHandler getHandler,
        InitializeStateContextRequestValidator initValidator,
        UpdateStateContextRequestValidator updateValidator,
        ILogger<StateContextController> logger)
    {
        _initializeHandler = initializeHandler;
        _updateHandler = updateHandler;
        _getHandler = getHandler;
        _initValidator = initValidator;
        _updateValidator = updateValidator;
        _logger = logger;
    }

    /// <summary>
    /// Initialize state context from ZIP code
    /// </summary>
    /// <param name="request">Request containing session ID and ZIP code</param>
    /// <returns>State context and configuration</returns>
    /// <response code="201">State context created successfully</response>
    /// <response code="400">Invalid request (validation error)</response>
    /// <response code="404">Session not found</response>
    /// <response code="409">State context already exists for this session</response>
    [HttpPost]
    [ProducesResponseType(typeof(StateContextResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> InitializeStateContext([FromBody] InitializeStateContextRequest request)
    {
        // Validate request
        var validationResult = await _initValidator.ValidateAsync(request);
        if (!validationResult.IsValid)
        {
            return BadRequest(new
            {
                error = "ValidationError",
                message = "Request validation failed",
                details = validationResult.Errors.Select(e => new
                {
                    field = e.PropertyName,
                    message = e.ErrorMessage
                })
            });
        }

        try
        {
            var command = new InitializeStateContextCommand
            {
                SessionId = request.SessionId,
                ZipCode = request.ZipCode,
                StateCodeOverride = request.StateCodeOverride
            };

            var result = await _initializeHandler.HandleAsync(command);

            return CreatedAtAction(
                nameof(GetStateContext),
                new { sessionId = result.StateContextResponse.StateContext.SessionId },
                result.StateContextResponse);
        }
        catch (Domain.Exceptions.ValidationException ex)
        {
            _logger.LogWarning(ex, "Validation error initializing state context");
            return BadRequest(new
            {
                error = "ValidationError",
                message = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initializing state context");
            return StatusCode(500, new
            {
                error = "InternalServerError",
                message = "An error occurred while initializing state context"
            });
        }
    }

    /// <summary>
    /// Update state context with a new state code
    /// </summary>
    /// <param name="request">Request containing session ID and new state code</param>
    /// <returns>Updated state context and configuration</returns>
    /// <response code="200">State context updated successfully</response>
    /// <response code="400">Invalid request (validation error)</response>
    /// <response code="404">State context not found for session</response>
    [HttpPut]
    [ProducesResponseType(typeof(StateContextResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateStateContext([FromBody] UpdateStateContextRequest request)
    {
        // Validate request
        var validationResult = await _updateValidator.ValidateAsync(request);
        if (!validationResult.IsValid)
        {
            return BadRequest(new
            {
                error = "ValidationError",
                message = "Request validation failed",
                details = validationResult.Errors.Select(e => new
                {
                    field = e.PropertyName,
                    message = e.ErrorMessage
                })
            });
        }

        try
        {
            var command = new UpdateStateContextCommand
            {
                SessionId = request.SessionId,
                StateCode = request.StateCode,
                ZipCode = request.ZipCode,
                IsManualOverride = request.IsManualOverride
            };

            var result = await _updateHandler.HandleAsync(command);

            return Ok(result.StateContextResponse);
        }
        catch (Domain.Exceptions.ValidationException ex)
        {
            _logger.LogWarning(ex, "Validation error updating state context");
            return BadRequest(new
            {
                error = "ValidationError",
                message = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating state context");
            return StatusCode(500, new
            {
                error = "InternalServerError",
                message = "An error occurred while updating state context"
            });
        }
    }

    /// <summary>
    /// Get state context by session ID
    /// </summary>
    /// <param name="sessionId">Session ID</param>
    /// <returns>State context and configuration</returns>
    /// <response code="200">State context retrieved successfully</response>
    /// <response code="404">State context not found for session</response>
    [HttpGet]
    [ProducesResponseType(typeof(StateContextResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetStateContext([FromQuery] Guid sessionId)
    {
        if (sessionId == Guid.Empty)
        {
            return BadRequest(new
            {
                error = "ValidationError",
                message = "SessionId is required"
            });
        }

        try
        {
            var query = new GetStateContextQuery { SessionId = sessionId };
            var result = await _getHandler.HandleAsync(query);

            if (result == null)
            {
                return NotFound(new
                {
                    error = "NotFound",
                    message = "State context not found for this session"
                });
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting state context for session {SessionId}", sessionId);
            return StatusCode(500, new
            {
                error = "InternalServerError",
                message = "An error occurred while retrieving state context"
            });
        }
    }
}
