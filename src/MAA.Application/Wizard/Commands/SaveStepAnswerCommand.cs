using MAA.Application.Wizard.DTOs;
using MAA.Application.Wizard.Repositories;
using MAA.Domain.Repositories;
using MAA.Domain.Wizard;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace MAA.Application.Wizard.Commands;

/// <summary>
/// Command to save or update a wizard step answer.
/// </summary>
public record SaveStepAnswerCommand
{
    public required SaveStepAnswerRequest Request { get; init; }
}

/// <summary>
/// Handler for SaveStepAnswerCommand.
/// </summary>
public class SaveStepAnswerHandler
{
    private readonly IStepAnswerRepository _answerRepository;
    private readonly IWizardSessionRepository _wizardSessionRepository;
    private readonly ISessionRepository _sessionRepository;
    private readonly StepInvalidationService _invalidationService;

    public SaveStepAnswerHandler(
        IStepAnswerRepository answerRepository,
        IWizardSessionRepository wizardSessionRepository,
        ISessionRepository sessionRepository,
        StepInvalidationService invalidationService)
    {
        _answerRepository = answerRepository ?? throw new ArgumentNullException(nameof(answerRepository));
        _wizardSessionRepository = wizardSessionRepository ?? throw new ArgumentNullException(nameof(wizardSessionRepository));
        _sessionRepository = sessionRepository ?? throw new ArgumentNullException(nameof(sessionRepository));
        _invalidationService = invalidationService ?? throw new ArgumentNullException(nameof(invalidationService));
    }

    public async Task<SaveStepAnswerResponse> HandleAsync(SaveStepAnswerCommand command, CancellationToken cancellationToken = default)
    {
        var request = command.Request;

        var session = await _sessionRepository.GetByIdAsync(request.SessionId, cancellationToken);
        if (session == null)
            throw new InvalidOperationException($"Session {request.SessionId} not found.");

        if (!session.IsValid())
            throw new InvalidOperationException("Session has expired or is no longer valid.");

        var wizardSession = await _wizardSessionRepository.GetBySessionIdAsync(request.SessionId, cancellationToken);
        if (wizardSession == null)
        {
            wizardSession = new WizardSession
            {
                Id = Guid.NewGuid(),
                SessionId = request.SessionId,
                CurrentStepId = request.StepId,
                LastActivityAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
                Version = 1
            };

            wizardSession = await _wizardSessionRepository.AddAsync(wizardSession, cancellationToken);
        }
        else
        {
            if (request.ExpectedSessionVersion.HasValue && wizardSession.Version != request.ExpectedSessionVersion.Value)
            {
                throw new DbUpdateConcurrencyException("Wizard session version mismatch.");
            }

            wizardSession.CurrentStepId = request.StepId;
            wizardSession.LastActivityAt = DateTime.UtcNow;
            wizardSession = await _wizardSessionRepository.UpdateAsync(wizardSession, cancellationToken);
        }

        var existingAnswer = await _answerRepository.GetBySessionAndStepIdAsync(
            request.SessionId,
            request.StepId,
            cancellationToken);

        if (existingAnswer != null && request.ExpectedAnswerVersion.HasValue
            && existingAnswer.Version != request.ExpectedAnswerVersion.Value)
        {
            throw new DbUpdateConcurrencyException("Step answer version mismatch.");
        }

        var answerDataJson = JsonSerializer.Serialize(request.AnswerData);
        var now = DateTime.UtcNow;

        var answerStatus = ParseAnswerStatus(request.Status);
        var stepAnswer = new StepAnswer
        {
            Id = existingAnswer?.Id ?? Guid.NewGuid(),
            SessionId = request.SessionId,
            StepId = request.StepId,
            AnswerData = answerDataJson,
            SchemaVersion = request.SchemaVersion,
            Status = answerStatus,
            SubmittedAt = now,
            UpdatedAt = now,
            Version = existingAnswer?.Version ?? 1
        };

        if (existingAnswer != null)
        {
            stepAnswer.Version = existingAnswer.Version;
        }

        stepAnswer = await _answerRepository.UpsertAsync(stepAnswer, cancellationToken);

        var progressStatus = answerStatus == AnswerStatus.Submitted
            ? StepStatus.Completed
            : StepStatus.InProgress;

        var stepProgress = new StepProgress
        {
            Id = Guid.NewGuid(),
            SessionId = request.SessionId,
            StepId = request.StepId,
            Status = progressStatus,
            LastUpdatedAt = now,
            Version = 1
        };

        stepProgress = await _wizardSessionRepository.UpsertProgressAsync(stepProgress, cancellationToken);

        var invalidatedSteps = await InvalidateDownstreamStepsAsync(request.StepId, existingAnswer, request.AnswerData, request.SessionId, cancellationToken);

        return new SaveStepAnswerResponse
        {
            WizardSession = new WizardSessionDto
            {
                SessionId = wizardSession.SessionId,
                CurrentStepId = wizardSession.CurrentStepId,
                LastActivityAt = wizardSession.LastActivityAt,
                Version = (uint)wizardSession.Version
            },
            StepAnswer = MapStepAnswer(stepAnswer),
            StepProgress = new StepProgressDto
            {
                StepId = stepProgress.StepId,
                Status = MapStepStatus(stepProgress.Status),
                LastUpdatedAt = stepProgress.LastUpdatedAt,
                Version = (uint)stepProgress.Version
            },
            InvalidatedSteps = invalidatedSteps
        };
    }

    private async Task<IReadOnlyList<string>> InvalidateDownstreamStepsAsync(
        string currentStepId,
        StepAnswer? existingAnswer,
        JsonElement newAnswerData,
        Guid sessionId,
        CancellationToken cancellationToken)
    {
        if (existingAnswer == null)
            return Array.Empty<string>();

        using var existingDocument = JsonDocument.Parse(existingAnswer.AnswerData);
        if (JsonElement.DeepEquals(existingDocument.RootElement, newAnswerData))
            return Array.Empty<string>();

        var downstreamSteps = _invalidationService.GetDownstreamStepIds(currentStepId);
        if (downstreamSteps.Count == 0)
            return Array.Empty<string>();

        var updated = await _wizardSessionRepository.UpdateProgressStatusesAsync(
            sessionId,
            downstreamSteps,
            StepStatus.RequiresRevalidation,
            cancellationToken);

        return updated.Select(progress => progress.StepId).ToList();
    }

    private static StepAnswerDto MapStepAnswer(StepAnswer answer)
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
    }

    private static AnswerStatus ParseAnswerStatus(string status)
    {
        return status.ToLowerInvariant() switch
        {
            "draft" => AnswerStatus.Draft,
            "submitted" => AnswerStatus.Submitted,
            _ => throw new InvalidOperationException("Unsupported answer status.")
        };
    }

    private static string MapAnswerStatus(AnswerStatus status)
    {
        return status switch
        {
            AnswerStatus.Draft => "draft",
            AnswerStatus.Submitted => "submitted",
            _ => "draft"
        };
    }

    private static string MapStepStatus(StepStatus status)
    {
        return status switch
        {
            StepStatus.NotStarted => "not_started",
            StepStatus.InProgress => "in_progress",
            StepStatus.Completed => "completed",
            StepStatus.RequiresRevalidation => "requires_revalidation",
            _ => "not_started"
        };
    }
}
