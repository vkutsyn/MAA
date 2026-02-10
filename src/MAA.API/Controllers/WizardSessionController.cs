using MAA.Application.Wizard.Commands;
using MAA.Application.Wizard.DTOs;
using MAA.Application.Wizard.Queries;
using MAA.Application.Wizard.Validators;
using Microsoft.AspNetCore.Mvc;

namespace MAA.API.Controllers;

/// <summary>
/// Controller for wizard session management.
/// </summary>
[ApiController]
[Route("api/wizard-session")]
public class WizardSessionController : ControllerBase
{
    private readonly SaveStepAnswerHandler _saveStepAnswerHandler;
    private readonly GetWizardSessionStateHandler _getWizardSessionStateHandler;
    private readonly GetStepAnswersHandler _getStepAnswersHandler;
    private readonly GetNextStepHandler _getNextStepHandler;
    private readonly GetStepDetailHandler _getStepDetailHandler;
    private readonly SaveStepAnswerRequestValidator _saveValidator;
    private readonly GetWizardSessionStateValidator _stateValidator;
    private readonly ILogger<WizardSessionController> _logger;

    public WizardSessionController(
        SaveStepAnswerHandler saveStepAnswerHandler,
        GetWizardSessionStateHandler getWizardSessionStateHandler,
        GetStepAnswersHandler getStepAnswersHandler,
        GetNextStepHandler getNextStepHandler,
        GetStepDetailHandler getStepDetailHandler,
        SaveStepAnswerRequestValidator saveValidator,
        GetWizardSessionStateValidator stateValidator,
        ILogger<WizardSessionController> logger)
    {
        _saveStepAnswerHandler = saveStepAnswerHandler;
        _getWizardSessionStateHandler = getWizardSessionStateHandler;
        _getStepAnswersHandler = getStepAnswersHandler;
        _getNextStepHandler = getNextStepHandler;
        _getStepDetailHandler = getStepDetailHandler;
        _saveValidator = saveValidator;
        _stateValidator = stateValidator;
        _logger = logger;
    }

    /// <summary>
    /// Save or update a step answer.
    /// </summary>
    [HttpPost("answers")]
    [ProducesResponseType(typeof(SaveStepAnswerResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> SaveStepAnswer([FromBody] SaveStepAnswerRequest request)
    {
        var validationResult = await _saveValidator.ValidateAsync(request);
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
            var command = new SaveStepAnswerCommand { Request = request };
            var response = await _saveStepAnswerHandler.HandleAsync(command);
            return Ok(response);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("expired") || ex.Message.Contains("no longer valid"))
        {
            _logger.LogWarning(ex, "Session invalid while saving step answer");
            return Unauthorized(new { error = "Unauthorized", message = ex.Message });
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("not found"))
        {
            _logger.LogWarning(ex, "Session not found while saving step answer");
            return NotFound(new { error = "NotFound", message = ex.Message });
        }
    }

    /// <summary>
    /// Restore wizard session state.
    /// </summary>
    [HttpGet("{sessionId:guid}")]
    [ProducesResponseType(typeof(WizardSessionStateResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetWizardSessionState([FromRoute] Guid sessionId)
    {
        var query = new GetWizardSessionStateQuery { SessionId = sessionId };
        var validationResult = await _stateValidator.ValidateAsync(query);
        if (!validationResult.IsValid)
        {
            return BadRequest(new
            {
                error = "ValidationError",
                message = "SessionId is required"
            });
        }

        try
        {
            var response = await _getWizardSessionStateHandler.HandleAsync(query);
            if (response == null)
            {
                return NotFound(new { error = "NotFound", message = "Wizard session not found" });
            }

            return Ok(response);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("expired") || ex.Message.Contains("no longer valid"))
        {
            _logger.LogWarning(ex, "Session invalid while restoring wizard state");
            return Unauthorized(new { error = "Unauthorized", message = ex.Message });
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("not found"))
        {
            _logger.LogWarning(ex, "Session not found while restoring wizard state");
            return NotFound(new { error = "NotFound", message = ex.Message });
        }
    }

    /// <summary>
    /// Retrieve step answers for a session.
    /// </summary>
    [HttpGet("{sessionId:guid}/answers")]
    [ProducesResponseType(typeof(StepAnswersResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetStepAnswers([FromRoute] Guid sessionId, [FromQuery] string? stepId)
    {
        if (sessionId == Guid.Empty)
        {
            return BadRequest(new { error = "ValidationError", message = "SessionId is required" });
        }

        try
        {
            var query = new GetStepAnswersQuery { SessionId = sessionId, StepId = stepId };
            var response = await _getStepAnswersHandler.HandleAsync(query);
            return Ok(response);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("expired") || ex.Message.Contains("no longer valid"))
        {
            _logger.LogWarning(ex, "Session invalid while retrieving step answers");
            return Unauthorized(new { error = "Unauthorized", message = ex.Message });
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("not found"))
        {
            _logger.LogWarning(ex, "Session not found while retrieving step answers");
            return NotFound(new { error = "NotFound", message = ex.Message });
        }
    }

    /// <summary>
    /// Get the next step definition based on current answers.
    /// </summary>
    [HttpPost("next-step")]
    [ProducesResponseType(typeof(GetNextStepResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetNextStep([FromBody] GetNextStepRequest request)
    {
        if (request.SessionId == Guid.Empty || string.IsNullOrWhiteSpace(request.CurrentStepId))
        {
            return BadRequest(new { error = "ValidationError", message = "SessionId and CurrentStepId are required" });
        }

        try
        {
            var query = new GetNextStepQuery
            {
                SessionId = request.SessionId,
                CurrentStepId = request.CurrentStepId
            };

            var response = await _getNextStepHandler.HandleAsync(query);
            if (response == null)
            {
                return NotFound(new { error = "NotFound", message = "Step definition not found" });
            }

            return Ok(response);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("expired") || ex.Message.Contains("no longer valid"))
        {
            _logger.LogWarning(ex, "Session invalid while resolving next step");
            return Unauthorized(new { error = "Unauthorized", message = ex.Message });
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("not found"))
        {
            _logger.LogWarning(ex, "Session not found while resolving next step");
            return NotFound(new { error = "NotFound", message = ex.Message });
        }
    }

    /// <summary>
    /// Get step definition and existing answer details.
    /// </summary>
    [HttpGet("{sessionId:guid}/steps/{stepId}")]
    [ProducesResponseType(typeof(StepDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetStepDetail([FromRoute] Guid sessionId, [FromRoute] string stepId)
    {
        if (sessionId == Guid.Empty || string.IsNullOrWhiteSpace(stepId))
        {
            return BadRequest(new { error = "ValidationError", message = "SessionId and StepId are required" });
        }

        try
        {
            var query = new GetStepDetailQuery
            {
                SessionId = sessionId,
                StepId = stepId
            };

            var response = await _getStepDetailHandler.HandleAsync(query);
            if (response == null)
            {
                return NotFound(new { error = "NotFound", message = "Step detail not found" });
            }

            return Ok(response);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("expired") || ex.Message.Contains("no longer valid"))
        {
            _logger.LogWarning(ex, "Session invalid while retrieving step detail");
            return Unauthorized(new { error = "Unauthorized", message = ex.Message });
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("not found"))
        {
            _logger.LogWarning(ex, "Session not found while retrieving step detail");
            return NotFound(new { error = "NotFound", message = ex.Message });
        }
    }
}
