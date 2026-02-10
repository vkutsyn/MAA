using MAA.Domain;

namespace MAA.Application.Interfaces;

/// <summary>
/// Cache abstraction for question definitions.
/// </summary>
public interface IQuestionDefinitionsCache
{
    Task<QuestionDefinitionsCacheEntry?> GetAsync(
        string stateCode,
        string programCode,
        CancellationToken cancellationToken = default);

    Task SetAsync(
        string stateCode,
        string programCode,
        QuestionDefinitionsCacheEntry entry,
        CancellationToken cancellationToken = default);

    Task InvalidateAsync(
        string stateCode,
        string programCode,
        CancellationToken cancellationToken = default);
}

public record QuestionDefinitionsCacheEntry(
    IReadOnlyList<Question> Questions,
    IReadOnlyList<ConditionalRule> ConditionalRules,
    DateTime CachedAt);
