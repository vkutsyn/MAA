using System.ComponentModel.DataAnnotations;

namespace MAA.Domain.Sessions;

/// <summary>
/// Represents a registered user account (Phase 5 feature).
/// Stub implementation for Phase 1 - full implementation in Phase 5.
/// </summary>
public class User
{
    /// <summary>
    /// Unique user identifier (GUID).
    /// </summary>
    [Key]
    public Guid Id { get; set; }

    /// <summary>
    /// User email address (encrypted in database).
    /// </summary>
    [Required]
    [MaxLength(500)] // Encrypted value may be longer than plain email
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Bcrypt password hash.
    /// </summary>
    [Required]
    [MaxLength(255)]
    public string PasswordHash { get; set; } = string.Empty;

    /// <summary>
    /// User role for authorization (Admin, Reviewer, Analyst, User).
    /// </summary>
    [Required]
    public UserRole Role { get; set; } = UserRole.User;

    /// <summary>
    /// Whether email has been verified (Phase 5).
    /// </summary>
    public bool EmailVerified { get; set; }

    /// <summary>
    /// User creation timestamp.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Last update timestamp (for optimistic locking).
    /// </summary>
    public DateTime UpdatedAt { get; set; }

    /// <summary>
    /// Version number for optimistic concurrency control.
    /// </summary>
    [ConcurrencyCheck]
    public int Version { get; set; }

    /// <summary>
    /// Navigation property to Sessions.
    /// </summary>
    // public ICollection<Session> Sessions { get; set; } = new List<Session>();
}
