using MAA.Application.DTOs;
using MAA.Application.Eligibility.DTOs;
using MAA.Application.Eligibility.Services;
using MAA.Application.Handlers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;

namespace MAA.API.Controllers;

/// <summary>
/// Questions API controller for eligibility wizard question taxonomy.
/// Provides state-specific question sets with conditional logic.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class QuestionsController : ControllerBase
{
    private readonly IQuestionTaxonomyService _questionService;
    private readonly GetQuestionDefinitionsHandler _definitionsHandler;
    private readonly ILogger<QuestionsController> _logger;

    public QuestionsController(
        IQuestionTaxonomyService questionService,
        GetQuestionDefinitionsHandler definitionsHandler,
        ILogger<QuestionsController> logger)
    {
        _questionService = questionService ?? throw new ArgumentNullException(nameof(questionService));
        _definitionsHandler = definitionsHandler ?? throw new ArgumentNullException(nameof(definitionsHandler));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// GET /api/questions?state={stateCode}
    /// Gets the question taxonomy for a specific state.
    /// Returns ordered list of questions with conditional display rules.
    /// </summary>
    /// <param name="state">Two-letter state code (e.g., "TX", "CA")</param>
    /// <returns>200 OK with question set, or 404 if state not found</returns>
    [HttpGet]
    [ProducesResponseType(typeof(QuestionSetDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<QuestionSetDto>> GetQuestionsByState([FromQuery] string state)
    {
        if (string.IsNullOrWhiteSpace(state))
        {
            _logger.LogWarning("Missing state parameter in question request");
            return BadRequest(new { error = "State code is required" });
        }

        if (state.Length != 2)
        {
            _logger.LogWarning("Invalid state code format: {State}", state);
            return BadRequest(new { error = "State code must be exactly 2 characters" });
        }

        try
        {
            _logger.LogInformation("Fetching questions for state: {State}", state);
            var questionSet = await _questionService.GetQuestionSetByStateAsync(state);

            if (questionSet == null)
            {
                _logger.LogInformation("No question set found for state: {State}", state);
                return NotFound(new { error = $"No question set available for state {state}" });
            }

            _logger.LogInformation("Returning {Count} questions for state: {State}",
                questionSet.Questions.Count, state);
            return Ok(questionSet);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching questions for state: {State}", state);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// GET /api/questions/{stateCode}/{programCode}
    /// Gets question definitions for a state and program.
    /// </summary>
    /// <param name="stateCode">Two-letter state code (e.g., "CA")</param>
    /// <param name="programCode">Program code (e.g., "MEDI-CAL")</param>
    /// <param name="cancellationToken">Cancellation token</param>
    [HttpGet("{stateCode}/{programCode}")]
    [ProducesResponseType(typeof(GetQuestionsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<GetQuestionsResponse>> GetQuestionDefinitions(
        string stateCode,
        string programCode,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Audit: Question definitions requested for {StateCode}/{ProgramCode} (TraceId: {TraceId})",
            stateCode,
            programCode,
            HttpContext.TraceIdentifier);

        var result = await _definitionsHandler.HandleAsync(new GetQuestionDefinitionsQuery
        {
            StateCode = stateCode,
            ProgramCode = programCode
        }, cancellationToken);

        Response.GetTypedHeaders().CacheControl = new CacheControlHeaderValue
        {
            Public = true,
            MaxAge = TimeSpan.FromHours(24)
        };

        return Ok(result);
    }
}
