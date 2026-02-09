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
    public string FieldKey { get; set; } = string.Empty;

    /// <summary>
    /// Field type (e.g., "currency", "integer", "string", "boolean").
    /// Used for validation and formatting.
    /// </summary>
    public string FieldType { get; set; } = string.Empty;

    /// <summary>
    /// Answer value (plain text from user input).
    /// Will be encrypted before storage if IsPii=true.
    /// </summary>
    public string AnswerValue { get; set; } = string.Empty;

    /// <summary>
    /// Flag indicating if this field contains PII requiring encryption.
    /// </summary>
    public bool IsPii { get; set; }
}
