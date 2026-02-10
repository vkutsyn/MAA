using System.ComponentModel.DataAnnotations;

namespace MAA.Domain.Sessions;

/// <summary>
/// Represents a single answer to a wizard question, with encryption support.
/// Stores both encrypted and hashed values for different use cases.
/// </summary>
public class SessionAnswer
{
    /// <summary>
    /// Unique answer identifier (GUID).
    /// </summary>
    [Key]
    public Guid Id { get; set; }

    /// <summary>
    /// Foreign key to Session.
    /// </summary>
    [Required]
    public Guid SessionId { get; set; }

    /// <summary>
    /// Field key (e.g., "income_annual_2025", "ssn", "household_size").
    /// Maps to question taxonomy.
    /// </summary>
    [Required]
    [MaxLength(200)]
    public string FieldKey { get; set; } = string.Empty;

    /// <summary>
    /// Field type (e.g., "currency", "integer", "string", "boolean").
    /// Used for validation and display formatting.
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string FieldType { get; set; } = string.Empty;

    /// <summary>
    /// Plain text answer (for non-PII fields only).
    /// Null if field is PII (use AnswerEncrypted instead).
    /// </summary>
    [MaxLength(1000)]
    public string? AnswerPlain { get; set; }

    /// <summary>
    /// Encrypted answer (randomized encryption for PII fields).
    /// Stored as Base64-encoded ciphertext.
    /// </summary>
    public string? AnswerEncrypted { get; set; }

    /// <summary>
    /// Deterministic hash of answer (for exact-match queries, e.g., SSN lookup).
    /// Only populated for fields requiring searchability.
    /// </summary>
    [MaxLength(256)]
    public string? AnswerHash { get; set; }

    /// <summary>
    /// Which encryption key version was used to encrypt/hash this answer.
    /// References EncryptionKey.KeyVersion for key rotation support.
    /// Null for non-PII fields that don't require encryption.
    /// </summary>
    public int? KeyVersion { get; set; }

    /// <summary>
    /// Flag indicating if field contains PII requiring encryption.
    /// </summary>
    public bool IsPii { get; set; }

    /// <summary>
    /// JSONB storage for validation errors (if any).
    /// Structure: { "errors": ["error1", "error2"], "warnings": [...] }
    /// </summary>
    public string? ValidationErrors { get; set; }

    /// <summary>
    /// Answer creation timestamp.
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
    /// Navigation property to Session.
    /// </summary>
    // public Session Session { get; set; } = null!;

    /// <summary>
    /// Domain validation: Ensures answer configuration is valid.
    /// </summary>
    public void Validate()
    {
        if (Id == Guid.Empty)
            throw new InvalidOperationException("SessionAnswer ID cannot be empty");

        if (SessionId == Guid.Empty)
            throw new InvalidOperationException("SessionId cannot be empty");

        if (string.IsNullOrWhiteSpace(FieldKey))
            throw new InvalidOperationException("FieldKey is required");

        if (string.IsNullOrWhiteSpace(FieldType))
            throw new InvalidOperationException("FieldType is required");

        // PII fields must have encrypted value, non-PII must have plain value
        if (IsPii && string.IsNullOrWhiteSpace(AnswerEncrypted))
            throw new InvalidOperationException("PII fields must have AnswerEncrypted");

        if (!IsPii && string.IsNullOrWhiteSpace(AnswerPlain))
            throw new InvalidOperationException("Non-PII fields must have AnswerPlain");

        if (Version < 0)
            throw new InvalidOperationException("Version cannot be negative");
    }
}
