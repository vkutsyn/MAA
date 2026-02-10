using MAA.Application.Sessions.Commands;
using MAA.Application.Sessions.DTOs;
using MAA.Application.Sessions.Queries;
using MAA.Application.Sessions.Validators;
using Microsoft.AspNetCore.Mvc;

namespace MAA.API.Controllers;

/// <summary>
/// Controller for session answer operations (save/retrieve wizard answers).
/// Implements US2: Session Data Persistence.
/// Handles encryption transparently via command/query handlers.
/// </summary>
[ApiController]
[Route("api/sessions/{sessionId:guid}/answers")]
[Produces("application/json")]
public class SessionAnswersController : ControllerBase
{
    private readonly SaveAnswerCommandHandler _saveAnswerHandler;
    private readonly GetAnswersQueryHandler _getAnswersHandler;
    private readonly SaveAnswerCommandValidator _validator;
    private readonly ILogger<SessionAnswersController> _logger;

    public SessionAnswersController(
        SaveAnswerCommandHandler saveAnswerHandler,
        GetAnswersQueryHandler getAnswersHandler,
        SaveAnswerCommandValidator validator,
        ILogger<SessionAnswersController> logger)
    {
        _saveAnswerHandler = saveAnswerHandler ?? throw new ArgumentNullException(nameof(saveAnswerHandler));
        _getAnswersHandler = getAnswersHandler ?? throw new ArgumentNullException(nameof(getAnswersHandler));
        _validator = validator ?? throw new ArgumentNullException(nameof(validator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Saves or updates an answer for a session.
    /// POST /api/sessions/{sessionId}/answers
    /// US2 Acceptance Scenario 1: Income=$2100 encrypted before DB insert.
    /// </summary>
    /// <param name="sessionId">Session ID (UUID format, required)</param>
    /// <param name="dto">Answer data including field key, type, value, and PII flag</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Saved answer DTO with ID and encryption metadata</returns>
    /// <remarks>
    /// Request Body (SaveAnswerDto):
    /// - fieldKey: Required, max 200 characters, must match question taxonomy
    ///   Example: "income_annual", "ssn", "household_size"
    /// - fieldType: Required, must be one of: "currency", "integer", "string", "boolean", "date", "text"
    ///   Determines validation rules applied to answerValue
    /// - answerValue: Required, max 10000 characters
    ///   Type-specific validation:
    ///   * currency: Must be non-negative decimal (e.g., "45000.00")
    ///   * integer: Must be valid integer (e.g., "128")
    ///   * boolean: Must be "true" or "false"
    ///   * date: Must be valid DateTime in ISO 8601 format (e.g., "2026-02-10")
    ///   * string: Max 5000 characters
    ///   * text: Max 10000 characters
    /// - isPii: Boolean flag. If true, answerValue is encrypted at rest
    ///   PII examples: ssn, email, phone, income, driver_license
    /// 
    /// Response (SessionAnswerDto):
    /// - id: Unique answer identifier (UUID)
    /// - sessionId: Reference to parent session
    /// - fieldKey, fieldType, answerValue: Echo of input (value is decrypted)
    /// - isPii, keyVersion, validationErrors: Metadata
    /// 
    /// Status Codes:
    /// - 201 Created: Answer saved successfully
    /// - 400 Bad Request: Validation failed (see validation rules above)
    /// - 401 Unauthorized: Session expired or revoked (exceeds 30-min timeout)
    /// - 404 Not Found: Session does not exist
    /// - 500 Internal Server Error: Server error (unexpected)
    /// </remarks>
    /// <response code="201">Answer saved successfully</response>
    /// <response code="400">Validation failed</response>
    /// <response code="401">Session expired or invalid</response>
    /// <response code="404">Session not found</response>
    [HttpPost]
    [ProducesResponseType(typeof(SessionAnswerDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SaveAnswer(
        [FromRoute] Guid sessionId,
        [FromBody] SaveAnswerDto dto,
        CancellationToken cancellationToken)
    {
        try
        {
            var command = new SaveAnswerCommand
            {
                SessionId = sessionId,
                FieldKey = dto.FieldKey,
                FieldType = dto.FieldType,
                AnswerValue = dto.AnswerValue,
                IsPii = dto.IsPii
            };

            // Validate command
            var validationResult = _validator.Validate(command);
            if (!validationResult.IsValid)
            {
                _logger.LogWarning("SaveAnswer validation failed for session {SessionId}: {Errors}",
                    sessionId, validationResult.GetErrorMessage());
                return BadRequest(new ProblemDetails
                {
                    Title = "Validation failed",
                    Detail = validationResult.GetErrorMessage(),
                    Status = StatusCodes.Status400BadRequest
                });
            }

            // Execute command
            var answer = await _saveAnswerHandler.HandleAsync(command, cancellationToken);

            _logger.LogInformation("Answer saved for session {SessionId}, field {FieldKey}, PII={IsPii}",
                sessionId, dto.FieldKey, dto.IsPii);

            return CreatedAtAction(
                nameof(GetAnswers),
                new { sessionId },
                answer);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("not found"))
        {
            _logger.LogWarning("SaveAnswer failed - session not found: {SessionId}", sessionId);
            return NotFound(new ProblemDetails
            {
                Title = "Session not found",
                Detail = ex.Message,
                Status = StatusCodes.Status404NotFound
            });
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("expired") || ex.Message.Contains("no longer valid"))
        {
            _logger.LogWarning("SaveAnswer failed - session invalid: {SessionId}, {Message}", sessionId, ex.Message);
            return Unauthorized(new ProblemDetails
            {
                Title = "Session expired",
                Detail = "Your session expired after 30 minutes. Start a new eligibility check.", // CONST-III
                Status = StatusCodes.Status401Unauthorized
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SaveAnswer failed for session {SessionId}", sessionId);
            throw; // Global exception handler will process
        }
    }

    /// <summary>
    /// Retrieves all answers for a session.
    /// GET /api/sessions/{sessionId}/answers
    /// US2: Wizard answers saved → page refresh → answers restored.
    /// </summary>
    /// <param name="sessionId">Session ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of answers with decrypted values</returns>
    /// <response code="200">Answers retrieved successfully</response>
    /// <response code="401">Session expired or invalid</response>
    /// <response code="404">Session not found</response>
    [HttpGet]
    [ProducesResponseType(typeof(List<SessionAnswerDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetAnswers(
        [FromRoute] Guid sessionId,
        CancellationToken cancellationToken)
    {
        try
        {
            var query = new GetAnswersQuery { SessionId = sessionId };
            var answers = await _getAnswersHandler.HandleAsync(query, cancellationToken);

            _logger.LogInformation("Retrieved {Count} answers for session {SessionId}", answers.Count, sessionId);

            return Ok(answers);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("not found"))
        {
            _logger.LogWarning("GetAnswers failed - session not found: {SessionId}", sessionId);
            return NotFound(new ProblemDetails
            {
                Title = "Session not found",
                Detail = ex.Message,
                Status = StatusCodes.Status404NotFound
            });
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("expired") || ex.Message.Contains("no longer valid"))
        {
            _logger.LogWarning("GetAnswers failed - session invalid: {SessionId}, {Message}", sessionId, ex.Message);
            return Unauthorized(new ProblemDetails
            {
                Title = "Session expired",
                Detail = "Your session expired after 30 minutes. Start a new eligibility check.", // CONST-III
                Status = StatusCodes.Status401Unauthorized
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetAnswers failed for session {SessionId}", sessionId);
            throw; // Global exception handler will process
        }
    }
}
