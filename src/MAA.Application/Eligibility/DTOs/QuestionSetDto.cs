namespace MAA.Application.Eligibility.DTOs;

/// <summary>
/// DTO representing a complete question set for a state.
/// Used in GET /api/questions?state={state} endpoint.
/// </summary>
public class QuestionSetDto
{
    /// <summary>
    /// Two-letter state code this question set applies to.
    /// </summary>
    public required string State { get; set; }

    /// <summary>
    /// Version identifier for the question taxonomy (e.g., "1.0", "2024-Q1").
    /// </summary>
    public required string Version { get; set; }

    /// <summary>
    /// Ordered list of questions for the wizard.
    /// </summary>
    public required List<QuestionDto> Questions { get; set; }
}
