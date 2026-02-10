using System.ComponentModel.DataAnnotations;

namespace MAA.Domain.Wizard;

/// <summary>
/// Tracks completion status for a wizard step.
/// </summary>
public class StepProgress
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    public Guid SessionId { get; set; }

    [Required]
    [MaxLength(100)]
    public string StepId { get; set; } = string.Empty;

    [Required]
    public StepStatus Status { get; set; }

    [Required]
    public DateTime LastUpdatedAt { get; set; }

    [ConcurrencyCheck]
    public int Version { get; set; }

    public void Validate()
    {
        if (Id == Guid.Empty)
            throw new InvalidOperationException("StepProgress ID cannot be empty");

        if (SessionId == Guid.Empty)
            throw new InvalidOperationException("SessionId cannot be empty");

        if (string.IsNullOrWhiteSpace(StepId))
            throw new InvalidOperationException("StepId is required");

        if (Version < 0)
            throw new InvalidOperationException("Version cannot be negative");
    }
}
