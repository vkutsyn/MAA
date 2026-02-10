using MAA.Application.Eligibility.DTOs;

namespace MAA.Application.Eligibility.Services;

/// <summary>
/// Service for question taxonomy operations for the eligibility wizard.
/// Provides state-specific question sets with conditional logic.
/// </summary>
public interface IQuestionTaxonomyService
{
    /// <summary>
    /// Gets the question set for a specific state.
    /// Returns ordered list of questions with conditional display rules.
    /// </summary>
    /// <param name="stateCode">Two-letter state code (e.g., "TX", "CA")</param>
    /// <returns>Question set DTO or null if state not found</returns>
    Task<QuestionSetDto?> GetQuestionSetByStateAsync(string stateCode);
}
