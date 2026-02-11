using MAA.Application.Eligibility.DTOs;
using MAA.Application.Eligibility.Queries;
using MAA.Domain.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace MAA.API.Controllers;

/// <summary>
/// Eligibility evaluation API controller.
/// Contract reference: specs/010-eligibility-evaluation-engine/contracts/eligibility-evaluation-api.yaml
/// </summary>
[ApiController]
[Route("api/eligibility")]
public class EligibilityController : ControllerBase
{
	private readonly EvaluateEligibilityQueryHandler _handler;

	public EligibilityController(EvaluateEligibilityQueryHandler handler)
	{
		_handler = handler ?? throw new ArgumentNullException(nameof(handler));
	}

	[HttpPost("evaluate")]
	public async Task<ActionResult<EligibilityEvaluateResponseDto>> EvaluateEligibility(
		[FromBody] EligibilityEvaluateRequestDto request,
		CancellationToken cancellationToken)
	{
		ValidateRequest(request);
		request.StateCode = request.StateCode.Trim().ToUpperInvariant();

		var response = await _handler.HandleAsync(new EvaluateEligibilityQuery
		{
			Request = request
		}, cancellationToken);

		return Ok(response);
	}

	private static void ValidateRequest(EligibilityEvaluateRequestDto request)
	{
		if (request == null)
			throw new ValidationException("Request body is required.");

		var errors = new Dictionary<string, string[]>();

		if (string.IsNullOrWhiteSpace(request.StateCode))
			errors["stateCode"] = new[] { "State code is required." };
		else if (request.StateCode.Trim().Length != 2)
			errors["stateCode"] = new[] { "State code must be exactly 2 characters." };

		if (request.EffectiveDate == default)
			errors["effectiveDate"] = new[] { "Effective date is required." };

		if (request.Answers == null)
			errors["answers"] = new[] { "Answers payload is required." };

		if (errors.Count > 0)
			throw new ValidationException(errors);
	}
}
