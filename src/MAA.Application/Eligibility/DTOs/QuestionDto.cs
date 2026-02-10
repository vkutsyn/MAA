namespace MAA.Application.Eligibility.DTOs;

/// <summary>
/// DTO representing a single question in the eligibility wizard.
/// Matches the OpenAPI QuestionDto schema.
/// </summary>
public class QuestionDto
{
    /// <summary>
    /// Unique question key (e.g., "household_size", "annual_income").
    /// </summary>
    public required string Key { get; set; }

    /// <summary>
    /// Human-readable question label.
    /// </summary>
    public required string Label { get; set; }

    /// <summary>
    /// Question input type.
    /// Valid values: currency, integer, string, boolean, date, text, select, multiselect.
    /// </summary>
    public required string Type { get; set; }

    /// <summary>
    /// Whether this question requires an answer before proceeding.
    /// </summary>
    public bool Required { get; set; }

    /// <summary>
    /// Optional help text to display for the question.
    /// </summary>
    public string? HelpText { get; set; }

    /// <summary>
    /// Options for select/multiselect questions.
    /// </summary>
    public List<QuestionOption>? Options { get; set; }

    /// <summary>
    /// Conditional display rules for this question.
    /// </summary>
    public List<QuestionCondition>? Conditions { get; set; }
}

/// <summary>
/// Option for select/multiselect questions.
/// </summary>
public class QuestionOption
{
    /// <summary>
    /// Option value (stored in answer).
    /// </summary>
    public required string Value { get; set; }

    /// <summary>
    /// Display label for the option.
    /// </summary>
    public required string Label { get; set; }
}

/// <summary>
/// Conditional display rule for a question.
/// Question is only shown if all conditions evaluate to true.
/// </summary>
public class QuestionCondition
{
    /// <summary>
    /// Field key to evaluate (refers to another question).
    /// </summary>
    public required string FieldKey { get; set; }

    /// <summary>
    /// Comparison operator.
    /// Valid values: equals, not_equals, gt, gte, lt, lte, includes.
    /// </summary>
    public required string Operator { get; set; }

    /// <summary>
    /// Comparison value.
    /// </summary>
    public required string Value { get; set; }
}
