using MAA.Domain.Wizard;

namespace MAA.Application.Wizard.Repositories;

/// <summary>
/// Repository interface for wizard step answers.
/// </summary>
public interface IStepAnswerRepository
{
    /// <summary>
    /// Gets a step answer by session and step identifier.
    /// </summary>
    Task<StepAnswer?> GetBySessionAndStepIdAsync(Guid sessionId, string stepId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all step answers for a session.
    /// </summary>
    Task<IReadOnlyList<StepAnswer>> GetBySessionIdAsync(Guid sessionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates or updates a step answer.
    /// </summary>
    Task<StepAnswer> UpsertAsync(StepAnswer stepAnswer, CancellationToken cancellationToken = default);
}
