using System.ComponentModel.DataAnnotations;

namespace MAA.Domain.Wizard;

/// <summary>
/// Represents wizard session state for an auth session.
/// </summary>
public class WizardSession
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    public Guid SessionId { get; set; }

    [Required]
    [MaxLength(100)]
    public string CurrentStepId { get; set; } = string.Empty;

    [Required]
    public DateTime LastActivityAt { get; set; }

    [Required]
    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    [ConcurrencyCheck]
    public int Version { get; set; }

    public void Validate()
    {
        if (Id == Guid.Empty)
            throw new InvalidOperationException("WizardSession ID cannot be empty");

        if (SessionId == Guid.Empty)
            throw new InvalidOperationException("SessionId cannot be empty");

        if (string.IsNullOrWhiteSpace(CurrentStepId))
            throw new InvalidOperationException("CurrentStepId is required");

        if (Version < 0)
            throw new InvalidOperationException("Version cannot be negative");
    }
}
