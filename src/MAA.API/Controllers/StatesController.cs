using MAA.Application.Eligibility.DTOs;
using MAA.Application.Eligibility.Services;
using Microsoft.AspNetCore.Mvc;

namespace MAA.API.Controllers;

/// <summary>
/// States API controller for eligibility wizard state selection.
/// Provides state listings and ZIP code lookups for anonymous users.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class StatesController : ControllerBase
{
    private readonly IStateMetadataService _stateService;
    private readonly ILogger<StatesController> _logger;

    public StatesController(
        IStateMetadataService stateService,
        ILogger<StatesController> logger)
    {
        _stateService = stateService ?? throw new ArgumentNullException(nameof(stateService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// GET /api/states
    /// Gets all available states for the eligibility wizard.
    /// Returns pilot states with full question taxonomy support.
    /// </summary>
    /// <returns>200 OK with list of state info</returns>
    [HttpGet]
    [ProducesResponseType(typeof(List<StateInfoDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<List<StateInfoDto>>> GetAllStates()
    {
        try
        {
            _logger.LogInformation("Fetching all available states");
            var states = await _stateService.GetAllStatesAsync();
            return Ok(states);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching states");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// GET /api/states/lookup?zip={zip}
    /// Looks up state by 5-digit ZIP code.
    /// Returns state information if match found.
    /// </summary>
    /// <param name="zip">5-digit ZIP code</param>
    /// <returns>200 OK with state info, or 404 Not Found</returns>
    [HttpGet("lookup")]
    [ProducesResponseType(typeof(StateLookupResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<StateLookupResponse>> LookupStateByZip([FromQuery] string zip)
    {
        if (string.IsNullOrWhiteSpace(zip) || zip.Length != 5 || !zip.All(char.IsDigit))
        {
            _logger.LogWarning("Invalid ZIP code format: {Zip}", zip);
            return BadRequest(new { error = "ZIP code must be exactly 5 digits" });
        }

        try
        {
            _logger.LogInformation("Looking up state for ZIP: {Zip}", zip);
            var result = await _stateService.LookupStateByZipAsync(zip);

            if (result == null)
            {
                _logger.LogInformation("No state found for ZIP: {Zip}", zip);
                return NotFound(new { error = $"No state found for ZIP code {zip}" });
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error looking up state by ZIP: {Zip}", zip);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }
}
