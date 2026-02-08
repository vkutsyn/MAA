namespace MAA.Application.Sessions.DTOs;

/// <summary>
/// Data Transfer Object for session answer storage and retrieval.
/// Does not expose raw encrypted values; handles encryption at service layer.
/// </summary>
public class SessionAnswerDto
{
    /// <summary>
    /// Unique answer identifier.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Session ID this answer belongs to.
    /// </summary>
    public Guid SessionId { get; set; }

    /// <summary>
    /// Field key (e.g., "income_annual_2025", "ssn", "household_size").
    /// Maps to question taxonomy.
    /// </summary>
    public string FieldKey { get; set; } = string.Empty;

    /// <summary>
    /// Field type (e.g., "currency", "integer", "string", "boolean").
    /// </summary>
    public string FieldType { get; set; } = string.Empty;

    /// <summary>
    /// Plain text answer value (for non-PII fields only).
    /// Null if field is PII (never expose encrypted values in DTOs).
    /// </summary>
    public string? AnswerPlain { get; set; }

    /// <summary>
    /// Is this field marked as containing PII?
    /// </summary>
    public bool IsPii { get; set; }

    /// <summary>
    /// Encryption key version used for this answer (if encrypted).
    /// </summary>
    public int KeyVersion { get; set; }

    /// <summary>
    /// Validation errors in JSONB format if any.
    /// </summary>
    public string? ValidationErrors { get; set; }

    /// <summary>
    /// Answer creation timestamp.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Last update timestamp.
    /// </summary>
    public DateTime UpdatedAt { get; set; }
}
