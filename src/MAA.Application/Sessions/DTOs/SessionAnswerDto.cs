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
    /// <remarks>
    /// Format: UUID v4
    /// Constraints: Non-empty, immutable
    /// Example: "a1b2c3d4-e5f6-7g8h-9i0j-k1l2m3n4o5p6"
    /// </remarks>
    public Guid Id { get; set; }

    /// <summary>
    /// Session ID this answer belongs to.
    /// </summary>
    /// <remarks>
    /// Format: UUID v4
    /// Constraints: Non-empty, must reference an existing Session
    /// Foreign key to Session entity
    /// Example: "550e8400-e29b-41d4-a716-446655440000"
    /// </remarks>
    public Guid SessionId { get; set; }

    /// <summary>
    /// Field key (e.g., "income_annual_2025", "ssn", "household_size").
    /// Maps to question taxonomy.
    /// </summary>
    /// <remarks>
    /// Format: Kebab-case string identifier
    /// Constraints: Required, max 200 characters
    /// Validation: Must match known question keys in taxonomy
    /// Examples: "income_annual", "ssn", "household_size", "age", "citizenship"
    /// Used to link answer to specific question
    /// </remarks>
    public string FieldKey { get; set; } = string.Empty;

    /// <summary>
    /// Field type (e.g., "currency", "integer", "string", "boolean").
    /// </summary>
    /// <remarks>
    /// Allowed values: "currency", "integer", "string", "boolean", "date", "text"
    /// Constraints: Required, case-insensitive
    /// Validation: Must match one of allowed types
    /// Determines: Input validation rules and schema constraints
    /// Example: "currency"
    /// Type-specific constraints applied:
    ///   - currency: Must be non-negative decimal
    ///   - integer: Must be valid integer
    ///   - boolean: Must be "true" or "false"
    ///   - date: Must be valid DateTime
    ///   - string: Max 5000 characters
    ///   - text: Max 10000 characters
    /// </remarks>
    public string FieldType { get; set; } = string.Empty;

    /// <summary>
    /// Decrypted answer value (for display/use in application logic).
    /// For PII fields, this is the decrypted value; for non-PII, it's the plain value.
    /// Never exposes raw encrypted ciphertext.
    /// </summary>
    /// <remarks>
    /// Format: String representation of value (type-specific formatting)
    /// Constraints: Max 10000 characters, can be null
    /// Validation: Type-specific validation applied per FieldType
    /// Examples:
    ///   - currency: "45000.00"
    ///   - integer: "128"
    ///   - boolean: "true"
    ///   - date: "2026-02-10"
    ///   - string: "John Doe" or similar
    /// Important: Already decrypted at API response time; no additional decryption needed by client
    /// </remarks>
    public string? AnswerValue { get; set; }

    /// <summary>
    /// Is this field marked as containing PII?
    /// </summary>
    /// <remarks>
    /// Format: Boolean flag
    /// Constraints: Required, defaults to false
    /// Encryption: PII fields are encrypted at rest
    /// Audit: PII access is logged for compliance
    /// Examples of PII fields: ssn, email, phone, drivers_license
    /// Examples of non-PII: household_size, income_level_category
    /// </remarks>
    public bool IsPii { get; set; }

    /// <summary>
    /// Encryption key version used for this answer (if encrypted).
    /// </summary>
    /// <remarks>
    /// Format: Integer version number
    /// Constraints: Non-negative integer, required
    /// Used for: Proper decryption when key version is rotated
    /// Example: 3
    /// Important: PII fields always have a version; non-PII may have version 0
    /// </remarks>
    public int KeyVersion { get; set; }

    /// <summary>
    /// Validation errors in JSONB format if any.
    /// </summary>
    /// <remarks>
    /// Format: JSON string array of error messages
    /// Constraints: Can be null for valid answers
    /// Example: "[\"Value must be a positive number\", \"Cannot exceed 999999999\"]"
    /// Set by: Backend validation rules
    /// Cleared: When answer is re-validated and passes
    /// </remarks>
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
