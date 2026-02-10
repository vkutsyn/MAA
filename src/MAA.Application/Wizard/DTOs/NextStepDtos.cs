namespace MAA.Application.Wizard.DTOs;

/// <summary>
/// Request payload for next-step navigation.
/// </summary>
public record GetNextStepRequest
{
    public required Guid SessionId { get; init; }
    public required string CurrentStepId { get; init; }
}

/// <summary>
/// Response payload for next-step navigation.
/// </summary>
public record GetNextStepResponse
{
    public required WizardSessionDto WizardSession { get; init; }
    public required StepDefinitionDto StepDefinition { get; init; }
    public required StepProgressDto StepProgress { get; init; }
    public required bool IsFinal { get; init; }
}
