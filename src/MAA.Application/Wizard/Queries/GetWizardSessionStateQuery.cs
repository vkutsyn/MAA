using System.Text.Json;
using MAA.Application.Wizard.DTOs;
using MAA.Application.Wizard.Repositories;
using MAA.Domain.Repositories;

namespace MAA.Application.Wizard.Queries;

/// <summary>
/// Query to retrieve wizard session state by session ID.
/// </summary>
public record GetWizardSessionStateQuery
{
    public required Guid SessionId { get; init; }
}

/// <summary>
/// Handler for GetWizardSessionStateQuery.
/// </summary>
public class GetWizardSessionStateHandler
{
    private readonly IWizardSessionRepository _wizardSessionRepository;
    private readonly IStepAnswerRepository _answerRepository;
    private readonly ISessionRepository _sessionRepository;

    public GetWizardSessionStateHandler(
        IWizardSessionRepository wizardSessionRepository,
        IStepAnswerRepository answerRepository,
        ISessionRepository sessionRepository)
    {
        _wizardSessionRepository = wizardSessionRepository ?? throw new ArgumentNullException(nameof(wizardSessionRepository));
        _answerRepository = answerRepository ?? throw new ArgumentNullException(nameof(answerRepository));
        _sessionRepository = sessionRepository ?? throw new ArgumentNullException(nameof(sessionRepository));
    }

    public async Task<WizardSessionStateResponse?> HandleAsync(GetWizardSessionStateQuery query, CancellationToken cancellationToken = default)
    {
        var session = await _sessionRepository.GetByIdAsync(query.SessionId, cancellationToken);
        if (session == null)
            throw new InvalidOperationException($"Session {query.SessionId} not found.");

        if (!session.IsValid())
            throw new InvalidOperationException("Session has expired or is no longer valid.");

        var wizardSession = await _wizardSessionRepository.GetBySessionIdAsync(query.SessionId, cancellationToken);
        if (wizardSession == null)
            return null;

        var progress = await _wizardSessionRepository.GetProgressBySessionIdAsync(query.SessionId, cancellationToken);
        var answers = await _answerRepository.GetBySessionIdAsync(query.SessionId, cancellationToken);

        var progressDtos = progress.Select(p => new StepProgressDto
        {
            StepId = p.StepId,
            Status = MapStepStatus(p.Status),
            LastUpdatedAt = p.LastUpdatedAt,
            Version = (uint)p.Version
        }).ToList();

        var answerDtos = answers.Select(answer =>
        {
            using var document = JsonDocument.Parse(answer.AnswerData);
            return new StepAnswerDto
            {
                SessionId = answer.SessionId,
                StepId = answer.StepId,
                SchemaVersion = answer.SchemaVersion,
                Status = MapAnswerStatus(answer.Status),
                AnswerData = document.RootElement.Clone(),
                SubmittedAt = answer.SubmittedAt,
                UpdatedAt = answer.UpdatedAt,
                Version = (uint)answer.Version
            };
        }).ToList();

        return new WizardSessionStateResponse
        {
            WizardSession = new WizardSessionDto
            {
                SessionId = wizardSession.SessionId,
                CurrentStepId = wizardSession.CurrentStepId,
                LastActivityAt = wizardSession.LastActivityAt,
                Version = (uint)wizardSession.Version
            },
            StepProgress = progressDtos,
            StepAnswers = answerDtos
        };
    }

    private static string MapAnswerStatus(MAA.Domain.Wizard.AnswerStatus status)
    {
        return status switch
        {
            MAA.Domain.Wizard.AnswerStatus.Draft => "draft",
            MAA.Domain.Wizard.AnswerStatus.Submitted => "submitted",
            _ => "draft"
        };
    }

    private static string MapStepStatus(MAA.Domain.Wizard.StepStatus status)
    {
        return status switch
        {
            MAA.Domain.Wizard.StepStatus.NotStarted => "not_started",
            MAA.Domain.Wizard.StepStatus.InProgress => "in_progress",
            MAA.Domain.Wizard.StepStatus.Completed => "completed",
            MAA.Domain.Wizard.StepStatus.RequiresRevalidation => "requires_revalidation",
            _ => "not_started"
        };
    }
}
