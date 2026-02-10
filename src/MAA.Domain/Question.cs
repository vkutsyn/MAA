using System.ComponentModel.DataAnnotations;

namespace MAA.Domain;

/// <summary>
/// Represents a single eligibility question in a state/program questionnaire.
/// </summary>
public class Question
{
    [Key]
    public Guid QuestionId { get; set; }

    [Required]
    [MaxLength(2)]
    public string StateCode { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string ProgramCode { get; set; } = string.Empty;

    [Required]
    public int DisplayOrder { get; set; }

    [Required]
    [MaxLength(1000)]
    public string QuestionText { get; set; } = string.Empty;

    [Required]
    public QuestionFieldType FieldType { get; set; }

    [Required]
    public bool IsRequired { get; set; }

    [MaxLength(2000)]
    public string? HelpText { get; set; }

    [MaxLength(500)]
    public string? ValidationRegex { get; set; }

    public Guid? ConditionalRuleId { get; set; }

    public ConditionalRule? ConditionalRule { get; set; }

    public ICollection<QuestionOption> Options { get; set; } = new List<QuestionOption>();

    [Required]
    public DateTime CreatedAt { get; set; }

    [Required]
    public DateTime UpdatedAt { get; set; }
}

public enum QuestionFieldType
{
    Text,
    Select,
    Checkbox,
    Radio,
    Date,
    Currency
}
