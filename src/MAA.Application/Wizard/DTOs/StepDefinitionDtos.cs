using System.Text.Json;

namespace MAA.Application.Wizard.DTOs;

/// <summary>
/// DTO for a step definition returned by the API.
/// </summary>
public record StepDefinitionDto
{
    public required string StepId { get; init; }
    public required string Title { get; init; }
    public string? Description { get; init; }
    public required IReadOnlyList<StepFieldDto> Fields { get; init; }
    public IReadOnlyList<ValidationRuleDto>? ValidationRules { get; init; }
    public JsonElement? VisibilityRule { get; init; }
    public IReadOnlyList<NextStepRuleDto>? NextStepRules { get; init; }
    public JsonElement? DisplayMeta { get; init; }
}

/// <summary>
/// DTO for a step field definition.
/// </summary>
public record StepFieldDto
{
    public required string FieldId { get; init; }
    public required string Label { get; init; }
    public required string Type { get; init; }
    public bool Required { get; init; }
    public string? HelpText { get; init; }
    public IReadOnlyList<FieldOptionDto>? Options { get; init; }
    public decimal? Min { get; init; }
    public decimal? Max { get; init; }
    public string? Pattern { get; init; }
}

/// <summary>
/// DTO for select field options.
/// </summary>
public record FieldOptionDto
{
    public required string Value { get; init; }
    public required string Label { get; init; }
}

/// <summary>
/// DTO for validation rules.
/// </summary>
public record ValidationRuleDto
{
    public required string RuleId { get; init; }
    public required string RuleType { get; init; }
    public JsonElement? Parameters { get; init; }
}

/// <summary>
/// DTO for navigation rules.
/// </summary>
public record NextStepRuleDto
{
    public required JsonElement Condition { get; init; }
    public required string ToStepId { get; init; }
}
