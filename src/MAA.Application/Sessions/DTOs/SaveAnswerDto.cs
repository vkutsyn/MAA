namespace MAA.Application.Sessions.DTOs;

/// <summary>
/// Data Transfer Object for saving/updating session answers.
/// Used in POST /api/sessions/{id}/answers requests.
/// </summary>
public class SaveAnswerDto
{
    /// <summary>
    /// Field key (e.g., "income_annual_2025", "ssn", "household_size").
    /// Maps to question taxonomy.
    /// </summary>
    /// <remarks>
    /// Format: Kebab-case string identifier
    /// Constraints: Required, max 200 characters, non-empty
    /// Validation: Must match known question keys in taxonomy
    /// Examples: "income_annual", "ssn", "household_size", "age", "citizenship"
    /// Used to identify which question is being answered
    /// </remarks>
    public string FieldKey { get; set; } = string.Empty;

    /// <summary>
    /// Field type (e.g., "currency", "integer", "string", "boolean").
    /// Used for validation and formatting.
    /// </summary>
    /// <remarks>
    /// Allowed values: "currency", "integer", "string", "boolean", "date", "text"
    /// Constraints: Required, case-insensitive, max 50 characters
    /// Validation: Must match one of allowed types
    /// Determines: Input validation rules and storage format
    /// Example: "currency"
    /// Type-specific validation applied:
    ///   - currency: Must be non-negative decimal
    ///   - integer: Must be valid integer
    ///   - boolean: Must be "true" or "false"
    ///   - date: Must be valid DateTime (ISO 8601)
    ///   - string: Max 5000 characters
    ///   - text: Max 10000 characters
    /// </remarks>
    public string FieldType { get; set; } = string.Empty;

    /// <summary>
    /// Answer value (plain text from user input).
    /// Will be encrypted before storage if IsPii=true.
    /// </summary>
    /// <remarks>
    /// Format: String representation of value (type-specific)
    /// Constraints: Required, max 10000 characters, non-empty
    /// Validation: Type-specific validation applied per FieldType
    /// Examples:
    ///   - currency: "45000.00"
    ///   - integer: "128"
    ///   - boolean: "true" or "false"
    ///   - date: "2026-02-10"
    ///   - string: "John Doe" or similar
    /// Encryption: If IsPii=true, this value is encrypted at service layer
    /// Important: Submit as plain text; encryption is transparent to client
    /// </remarks>
    public string AnswerValue { get; set; } = string.Empty;

    /// <summary>
    /// Flag indicating if this field contains PII requiring encryption.
    /// </summary>
    /// <remarks>
    /// Format: Boolean flag
    /// Constraints: Required, defaults to false
    /// Encryption: If true, AnswerValue is AES-256-GCM encrypted before storage
    /// Audit: Access to PII fields is logged for HIPAA compliance
    /// Examples of PII fields: ssn, email, phone, drivers_license, income
    /// Examples of non-PII: household_size, income_level_category, age_group
    /// Compliance: Required for handling sensitive patient health information
    /// </remarks>
    public bool IsPii { get; set; }
}
