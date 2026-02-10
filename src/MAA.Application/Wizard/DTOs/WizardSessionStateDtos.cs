namespace MAA.Application.Wizard.DTOs;

/// <summary>
/// Response payload for wizard session state.
/// </summary>
public record WizardSessionStateResponse
{
    public required WizardSessionDto WizardSession { get; init; }
    public required IReadOnlyList<StepProgressDto> StepProgress { get; init; }
    public required IReadOnlyList<StepAnswerDto> StepAnswers { get; init; }
}

/// <summary>
/// Response payload for step answers retrieval.
/// </summary>
public record StepAnswersResponse
{
    public required IReadOnlyList<StepAnswerDto> StepAnswers { get; init; }
}
