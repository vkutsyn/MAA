using MAA.Domain.Wizard;

namespace MAA.Application.Wizard.Repositories;

/// <summary>
/// Repository interface for wizard session state and progress.
/// </summary>
public interface IWizardSessionRepository
{
    /// <summary>
    /// Gets a wizard session by the auth session ID.
    /// </summary>
    Task<WizardSession?> GetBySessionIdAsync(Guid sessionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new wizard session.
    /// </summary>
    Task<WizardSession> AddAsync(WizardSession wizardSession, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing wizard session.
    /// </summary>
    Task<WizardSession> UpdateAsync(WizardSession wizardSession, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a wizard session exists for the auth session.
    /// </summary>
    Task<bool> ExistsBySessionIdAsync(Guid sessionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all step progress records for a session.
    /// </summary>
    Task<IReadOnlyList<StepProgress>> GetProgressBySessionIdAsync(Guid sessionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a step progress record by session and step identifier.
    /// </summary>
    Task<StepProgress?> GetProgressBySessionAndStepIdAsync(Guid sessionId, string stepId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Upserts a step progress record.
    /// </summary>
    Task<StepProgress> UpsertProgressAsync(StepProgress progress, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates statuses for multiple steps within a session.
    /// </summary>
    Task<IReadOnlyList<StepProgress>> UpdateProgressStatusesAsync(
        Guid sessionId,
        IReadOnlyCollection<string> stepIds,
        StepStatus status,
        CancellationToken cancellationToken = default);
}
