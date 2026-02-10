namespace MAA.Application.Sessions.DTOs;

/// <summary>
/// Data Transfer Object for user information in API responses.
/// Represents authenticated users for session ownership and audit trails.
/// </summary>
public class UserDto
{
    /// <summary>
    /// Unique user identifier.
    /// </summary>
    /// <remarks>
    /// Format: UUID v4
    /// Constraints: Non-empty, immutable
    /// Example: "f47ac10b-58cc-4372-a567-0e02b2c3d479"
    /// </remarks>
    public Guid UserId { get; set; }

    /// <summary>
    /// User email address.
    /// </summary>
    /// <remarks>
    /// Format: Email address
    /// Constraints: Required, unique, max 320 characters
    /// Example: "user@example.com"
    /// </remarks>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// User role for authorization.
    /// </summary>
    /// <remarks>
    /// Allowed values: "Admin", "Reviewer", "Analyst", "Applicant"
    /// Constraints: Required
    /// Example: "Applicant"
    /// </remarks>
    public string Role { get; set; } = string.Empty;

    /// <summary>
    /// Account creation timestamp.
    /// </summary>
    /// <remarks>
    /// Format: DateTime (ISO 8601, UTC)
    /// Constraints: Required, immutable
    /// Example: "2026-02-10T09:30:00Z"
    /// </remarks>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Last successful sign-in timestamp.
    /// </summary>
    /// <remarks>
    /// Format: DateTime (ISO 8601, UTC)
    /// Constraints: Nullable when user has never signed in
    /// Example: "2026-02-10T10:45:00Z"
    /// </remarks>
    public DateTime? LastSignedInAt { get; set; }
}
