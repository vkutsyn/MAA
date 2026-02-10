using System.ComponentModel.DataAnnotations;

namespace MAA.Domain.Wizard;

/// <summary>
/// Represents a saved answer for a wizard step.
/// </summary>
public class StepAnswer
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    public Guid SessionId { get; set; }

    [Required]
    [MaxLength(100)]
    public string StepId { get; set; } = string.Empty;

    [Required]
    public string AnswerData { get; set; } = "{}";

    [Required]
    [MaxLength(50)]
    public string SchemaVersion { get; set; } = "v1";

    [Required]
    public AnswerStatus Status { get; set; }

    [Required]
    public DateTime SubmittedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    [ConcurrencyCheck]
    public int Version { get; set; }

    public void Validate()
    {
        if (Id == Guid.Empty)
            throw new InvalidOperationException("StepAnswer ID cannot be empty");

        if (SessionId == Guid.Empty)
            throw new InvalidOperationException("SessionId cannot be empty");

        if (string.IsNullOrWhiteSpace(StepId))
            throw new InvalidOperationException("StepId is required");

        if (string.IsNullOrWhiteSpace(SchemaVersion))
            throw new InvalidOperationException("SchemaVersion is required");

        if (Version < 0)
            throw new InvalidOperationException("Version cannot be negative");
    }
}
