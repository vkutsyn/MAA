using Microsoft.AspNetCore.Mvc;

namespace MAA.API.Controllers;

/// <summary>
/// Rules Engine API Controller
/// 
/// Endpoints for eligibility evaluation, rule management, and federal poverty level lookups
/// Phase 1 Placeholder: Implementation in Phase 3+
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class RulesController : ControllerBase
{
    private readonly ILogger<RulesController> _logger;

    public RulesController(ILogger<RulesController> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// POST /api/rules/evaluate
    /// Evaluate user eligibility based on household data and state
    /// Implementation: Phase 3 (US1 - Basic Eligibility Evaluation)
    /// </summary>
    [HttpPost("evaluate")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> EvaluateEligibility()
    {
        return StatusCode(StatusCodes.Status501NotImplemented, "Eligibility evaluation not yet implemented. Phase 3 implementation pending.");
    }
}
