using MAA.Domain;

namespace MAA.Application.Interfaces;

/// <summary>
/// Repository interface for eligibility question definitions.
/// </summary>
public interface IQuestionRepository
{
    /// <summary>
    /// Gets all questions for a state/program combination.
    /// </summary>
    Task<IReadOnlyList<Question>> GetQuestionsAsync(
        string stateCode,
        string programCode,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets conditional rules by their identifiers.
    /// </summary>
    Task<IReadOnlyList<ConditionalRule>> GetConditionalRulesAsync(
        IEnumerable<Guid> ruleIds,
        CancellationToken cancellationToken = default);
}
