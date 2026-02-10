using System.ComponentModel.DataAnnotations;

namespace MAA.Domain;

/// <summary>
/// Represents a selectable option for a question.
/// </summary>
public class QuestionOption
{
    [Key]
    public Guid OptionId { get; set; }

    [Required]
    public Guid QuestionId { get; set; }

    [Required]
    [MaxLength(200)]
    public string OptionLabel { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string OptionValue { get; set; } = string.Empty;

    [Required]
    public int DisplayOrder { get; set; }

    [Required]
    public DateTime CreatedAt { get; set; }

    [Required]
    public DateTime UpdatedAt { get; set; }

    public Question Question { get; set; } = null!;
}
