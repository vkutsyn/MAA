using System.Text.Json;
using MAA.Application.Wizard.DTOs;
using MAA.Application.Wizard.Repositories;
using MAA.Domain.Repositories;

namespace MAA.Application.Wizard.Queries;

/// <summary>
/// Query to retrieve step answers for a session, optionally filtered by step ID.
/// </summary>
public record GetStepAnswersQuery
{
    public required Guid SessionId { get; init; }
    public string? StepId { get; init; }
}

/// <summary>
/// Handler for GetStepAnswersQuery.
/// </summary>
public class GetStepAnswersHandler
{
    private readonly IStepAnswerRepository _answerRepository;
    private readonly ISessionRepository _sessionRepository;

    public GetStepAnswersHandler(
        IStepAnswerRepository answerRepository,
        ISessionRepository sessionRepository)
    {
        _answerRepository = answerRepository ?? throw new ArgumentNullException(nameof(answerRepository));
        _sessionRepository = sessionRepository ?? throw new ArgumentNullException(nameof(sessionRepository));
    }

    public async Task<StepAnswersResponse> HandleAsync(GetStepAnswersQuery query, CancellationToken cancellationToken = default)
    {
        var session = await _sessionRepository.GetByIdAsync(query.SessionId, cancellationToken);
        if (session == null)
            throw new InvalidOperationException($"Session {query.SessionId} not found.");

        if (!session.IsValid())
            throw new InvalidOperationException("Session has expired or is no longer valid.");

        var answers = await _answerRepository.GetBySessionIdAsync(query.SessionId, cancellationToken);
        if (!string.IsNullOrWhiteSpace(query.StepId))
        {
            answers = answers.Where(a => a.StepId == query.StepId).ToList();
        }

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

        return new StepAnswersResponse
        {
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
}
