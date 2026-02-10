using System.ComponentModel.DataAnnotations;

namespace MAA.Domain;

/// <summary>
/// Represents a visibility condition for a question.
/// </summary>
public class ConditionalRule
{
    [Key]
    public Guid ConditionalRuleId { get; set; }

    [Required]
    [MaxLength(5000)]
    public string RuleExpression { get; set; } = string.Empty;

    [MaxLength(200)]
    public string? Description { get; set; }

    public ICollection<Question> Questions { get; set; } = new List<Question>();

    [Required]
    public DateTime CreatedAt { get; set; }

    [Required]
    public DateTime UpdatedAt { get; set; }
}
