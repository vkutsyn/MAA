namespace MAA.Application.DTOs;

/// <summary>
/// Standard error response DTO for API endpoints
/// Feature: 006-state-context-init - User Story 3: Error Handling
/// </summary>
public record ErrorResponse
{
    /// <summary>
    /// Error code or category (e.g., "ValidationError", "NotFound", "InternalServerError")
    /// </summary>
    public required string Error { get; init; }

    /// <summary>
    /// Human-readable error message
    /// </summary>
    public required string Message { get; init; }

    /// <summary>
    /// Optional array of detailed error information (e.g., field validation errors)
    /// </summary>
    public IEnumerable<ErrorDetail>? Details { get; init; }
}

/// <summary>
/// Detailed error information for a specific field or aspect
/// </summary>
public record ErrorDetail
{
    /// <summary>
    /// Field name or context for the error
    /// </summary>
    public required string Field { get; init; }

    /// <summary>
    /// Specific error message for this field
    /// </summary>
    public required string Message { get; init; }
}
