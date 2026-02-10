using MAA.Application.Eligibility.DTOs;
using MAA.Application.Eligibility.Services;
using Microsoft.AspNetCore.Mvc;

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
    private readonly ILogger<QuestionsController> _logger;

    public QuestionsController(
        IQuestionTaxonomyService questionService,
        ILogger<QuestionsController> logger)
    {
        _questionService = questionService ?? throw new ArgumentNullException(nameof(questionService));
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
}
