using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MAA.Application.Eligibility.Handlers;
using MAA.Application.Eligibility.DTOs;
using MAA.Application.Eligibility.Validators;
using FluentValidation;

namespace MAA.API.Controllers;

/// <summary>
/// Rules Engine API Controller
/// 
/// Endpoints for eligibility evaluation, rule management, and federal poverty level lookups
/// Phase 3 Implementation: T021,T029,T030
/// </summary>
/// <remarks>
/// Authentication: All endpoints require JWT bearer token authentication.
/// Include token in Authorization header: Authorization: Bearer {token}
/// </remarks>
[ApiController]
[Route("api/[controller]")]
public class RulesController : ControllerBase
{
    private readonly ILogger<RulesController> _logger;
    private readonly IEvaluateEligibilityHandler _evaluateHandler;
    private readonly EligibilityInputValidator _validator;

    public RulesController(
        ILogger<RulesController> logger,
        IEvaluateEligibilityHandler evaluateHandler,
        EligibilityInputValidator validator)
    {
        _logger = logger;
        _evaluateHandler = evaluateHandler;
        _validator = validator;
    }

    /// <summary>
    /// POST /api/rules/evaluate
    /// Evaluate user eligibility based on household data and state
    /// Implementation: Phase 3 (US1 - Basic Eligibility Evaluation)
    /// </summary>
    [HttpPost("evaluate")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(EligibilityResultDto))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> EvaluateEligibility([FromBody] UserEligibilityInputDto input)
    {
        try
        {
            // Validate input
            var validationResult = await _validator.ValidateAsync(input);
            if (!validationResult.IsValid)
            {
                _logger.LogWarning("Invalid eligibility input: {Errors}",
                    string.Join(",", validationResult.Errors.Select(e => e.ErrorMessage)));
                return BadRequest(new
                {
                    errors = validationResult.Errors.Select(e => new
                    {
                        field = e.PropertyName,
                        message = e.ErrorMessage
                    })
                });
            }

            // Perform eligibility evaluation
            var result = await _evaluateHandler.EvaluateAsync(input);

            _logger.LogInformation("Eligibility evaluation completed for state {State}, status: {Status}",
                input.StateCode, result.OverallStatus);

            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid argument in eligibility evaluation");
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error evaluating eligibility");
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { error = "An error occurred during eligibility evaluation" });
        }
    }


    /// <summary>
    /// GET /api/rules/health
    /// Health check endpoint for rules service
    /// </summary>
    [HttpGet("health")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult HealthCheck()
    {
        return Ok(new
        {
            status = "healthy",
            timestamp = DateTime.UtcNow,
            service = "Rules Engine"
        });
    }
}
