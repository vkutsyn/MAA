namespace MAA.Application.DTOs;

/// <summary>
/// DTO for a question option.
/// </summary>
public record QuestionOptionDto
{
    public required Guid OptionId { get; init; }
    public required string OptionLabel { get; init; }
    public required string OptionValue { get; init; }
    public required int DisplayOrder { get; init; }
}

/// <summary>
/// DTO for a conditional rule definition.
/// </summary>
public record ConditionalRuleDto
{
    public required Guid ConditionalRuleId { get; init; }
    public required string RuleExpression { get; init; }
    public string? Description { get; init; }
}

/// <summary>
/// DTO for a question definition.
/// </summary>
public record QuestionDto
{
    public required Guid QuestionId { get; init; }
    public required int DisplayOrder { get; init; }
    public required string QuestionText { get; init; }
    public required string FieldType { get; init; }
    public required bool IsRequired { get; init; }
    public string? HelpText { get; init; }
    public string? ValidationRegex { get; init; }
    public Guid? ConditionalRuleId { get; init; }
    public IReadOnlyList<QuestionOptionDto>? Options { get; init; }
}

/// <summary>
/// DTO for question definitions response.
/// </summary>
public record GetQuestionsResponse
{
    public required string StateCode { get; init; }
    public required string ProgramCode { get; init; }
    public required IReadOnlyList<QuestionDto> Questions { get; init; }
    public required IReadOnlyList<ConditionalRuleDto> ConditionalRules { get; init; }
}
