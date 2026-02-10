using System.Text.Json;

namespace MAA.Application.Wizard.DTOs;

/// <summary>
/// Request payload for saving a wizard step answer.
/// </summary>
public record SaveStepAnswerRequest
{
    public required Guid SessionId { get; init; }
    public required string StepId { get; init; }
    public required string SchemaVersion { get; init; }
    public required string Status { get; init; }
    public required JsonElement AnswerData { get; init; }
    public int? ExpectedAnswerVersion { get; init; }
    public int? ExpectedSessionVersion { get; init; }
}

/// <summary>
/// Response payload after saving a wizard step answer.
/// </summary>
public record SaveStepAnswerResponse
{
    public required WizardSessionDto WizardSession { get; init; }
    public required StepAnswerDto StepAnswer { get; init; }
    public required StepProgressDto StepProgress { get; init; }
    public IReadOnlyList<string> InvalidatedSteps { get; init; } = Array.Empty<string>();
}
