using System.Text.Json;

namespace MAA.Domain.Wizard;

/// <summary>
/// Definition of a wizard step and its schema metadata.
/// </summary>
public record StepDefinition
{
    public required string StepId { get; init; }
    public required string Title { get; init; }
    public string? Description { get; init; }
    public required IReadOnlyList<StepFieldDefinition> Fields { get; init; } = Array.Empty<StepFieldDefinition>();
    public IReadOnlyList<ValidationRuleDefinition> ValidationRules { get; init; } = Array.Empty<ValidationRuleDefinition>();
    public JsonElement? VisibilityRule { get; init; }
    public IReadOnlyList<StepNavigationRule> NextStepRules { get; init; } = Array.Empty<StepNavigationRule>();
    public Dictionary<string, JsonElement>? DisplayMeta { get; init; }
    public int? Sequence { get; init; }
}

/// <summary>
/// Field metadata for a step definition.
/// </summary>
public record StepFieldDefinition
{
    public required string FieldId { get; init; }
    public required string Label { get; init; }
    public required string Type { get; init; }
    public bool Required { get; init; }
    public string? HelpText { get; init; }
    public IReadOnlyList<FieldOptionDefinition>? Options { get; init; }
    public decimal? Min { get; init; }
    public decimal? Max { get; init; }
    public string? Pattern { get; init; }
}

/// <summary>
/// Field option metadata for select inputs.
/// </summary>
public record FieldOptionDefinition
{
    public required string Value { get; init; }
    public required string Label { get; init; }
}

/// <summary>
/// Validation rule metadata for a step.
/// </summary>
public record ValidationRuleDefinition
{
    public required string RuleId { get; init; }
    public required string RuleType { get; init; }
    public JsonElement? Parameters { get; init; }
}
