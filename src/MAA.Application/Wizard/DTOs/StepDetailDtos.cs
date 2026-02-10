namespace MAA.Application.Wizard.DTOs;

/// <summary>
/// Response payload for step detail lookup.
/// </summary>
public record StepDetailResponse
{
    public required StepDefinitionDto StepDefinition { get; init; }
    public required StepProgressDto StepProgress { get; init; }
    public StepAnswerDto? StepAnswer { get; init; }
}
