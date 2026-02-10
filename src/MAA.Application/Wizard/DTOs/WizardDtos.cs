using System.Text.Json;

namespace MAA.Application.Wizard.DTOs;

/// <summary>
/// DTO for wizard session state.
/// </summary>
public record WizardSessionDto
{
    public required Guid SessionId { get; init; }
    public required string CurrentStepId { get; init; }
    public required DateTime LastActivityAt { get; init; }
    public required uint Version { get; init; }
}

/// <summary>
/// DTO for a saved wizard step answer.
/// </summary>
public record StepAnswerDto
{
    public required Guid SessionId { get; init; }
    public required string StepId { get; init; }
    public required string SchemaVersion { get; init; }
    public required string Status { get; init; }
    public required JsonElement AnswerData { get; init; }
    public required DateTime SubmittedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
    public required uint Version { get; init; }
}

/// <summary>
/// DTO for wizard step progress.
/// </summary>
public record StepProgressDto
{
    public required string StepId { get; init; }
    public required string Status { get; init; }
    public required DateTime LastUpdatedAt { get; init; }
    public required uint Version { get; init; }
}
