namespace MAA.Application.Sessions.DTOs;

/// <summary>
/// Data Transfer Object for validation result responses.
/// Used to communicate validation errors or success status in API responses.
/// Part of Phase 4 implementation: T025 (User Story 2 - Developer Understands Schemas)
/// </summary>
public class ValidationResultDto
{
    /// <summary>
    /// Whether validation passed (true) or failed (false).
    /// </summary>
    /// <remarks>
    /// Format: Boolean flag
    /// Constraints: Required
    /// Usage: Read this first to determine success/failure
    /// Example: false (when validation fails)
    /// </remarks>
    public bool IsValid { get; set; }

    /// <summary>
    /// Machine-readable error or success code.
    /// </summary>
    /// <remarks>
    /// Format: Uppercase alphanumeric code
    /// Constraints: Required, max 100 characters
    /// Examples:
    ///   - Success: "VALIDATION_SUCCESS", "ANSWER_SAVED"
    ///   - Error: "VALIDATION_ERROR", "INVALID_FIELD_TYPE", "VALUE_OUT_OF_RANGE"
    /// Usage: Use for logging, monitoring, or client-side logic
    /// </remarks>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// Human-readable explanation of validation result.
    /// </summary>
    /// <remarks>
    /// Format: Plain English text, suitable for end-user display
    /// Constraints: Required, max 1000 characters
    /// Localization: English (US locale)
    /// Examples:
    ///   - Success: "Answer saved successfully"
    ///   - Error: "Validation failed. Please check the provided data."
    /// Important: Does not expose sensitive implementation details
    /// </remarks>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Detailed validation errors, if any.
    /// </summary>
    /// <remarks>
    /// Format: Array of ValidationErrorDto objects
    /// Constraints: Empty array if IsValid=true; populated if IsValid=false
    /// Structure: Each error includes field name and message
    /// Example:
    ///   errors: [
    ///     { "field": "income", "message": "Income must be a positive number" },
    ///     { "field": "ssn", "message": "SSN must be 9 digits" }
    ///   ]
    /// Usage: Display errors next to form fields on client
    /// </remarks>
    public List<ValidationErrorDto> Errors { get; set; } = new();

    /// <summary>
    /// Optional success data (e.g., saved entity ID).
    /// </summary>
    /// <remarks>
    /// Format: Object structure depends on operation
    /// Constraints: Can be null
    /// Usage: Populated only on success responses
    /// Examples:
    ///   - Answer save: { "answerId": "a1b2c3d4-..." }
    ///   - Eligibility check: { "eligible": true, "programs": [...] }
    /// </remarks>
    public object? Data { get; set; }
}

/// <summary>
/// Data Transfer Object for individual validation errors.
/// Nested within ValidationResultDto to provide field-specific error details.
/// </summary>
public class ValidationErrorDto
{
    /// <summary>
    /// Field name that failed validation.
    /// </summary>
    /// <remarks>
    /// Format: Kebab-case or camelCase identifier
    /// Constraints: Required, identifies which form field has error
    /// Examples: "income", "ssn", "household_size", "email", "phone"
    /// Usage: Client maps to form field to show error message next to input
    /// </remarks>
    public string Field { get; set; } = string.Empty;

    /// <summary>
    /// Detailed error message specific to this field.
    /// </summary>
    /// <remarks>
    /// Format: Plain English text
    /// Constraints: Required, max 500 characters
    /// Content: Specific about what's wrong and how to fix it
    /// Examples:
    ///   - "Income must be a positive number"
    ///   - "SSN must be 9 digits (format: 123-45-6789)"
    ///   - "Phone must be in format (123) 456-7890"
    ///   - "Household size must be between 1 and 20"
    /// Tone: Helpful, not accusatory
    /// </remarks>
    public string Message { get; set; } = string.Empty;
}
