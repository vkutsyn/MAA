using System.Text.Json;

namespace MAA.Domain.Wizard;

/// <summary>
/// Conditional navigation rule between steps.
/// </summary>
public record StepNavigationRule
{
    public required JsonElement Condition { get; init; }
    public required string ToStepId { get; init; }
}
