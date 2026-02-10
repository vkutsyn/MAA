using System.Text.Json;
using MAA.Application.Wizard.DTOs;
using MAA.Application.Wizard.Repositories;
using MAA.Domain.Repositories;
using MAA.Domain.Wizard;

namespace MAA.Application.Wizard.Queries;

/// <summary>
/// Query to retrieve step detail for a session.
/// </summary>
public record GetStepDetailQuery
{
    public required Guid SessionId { get; init; }
    public required string StepId { get; init; }
}

/// <summary>
/// Handler for GetStepDetailQuery.
/// </summary>
public class GetStepDetailHandler
{
    private readonly IWizardSessionRepository _wizardSessionRepository;
    private readonly IStepAnswerRepository _answerRepository;
    private readonly ISessionRepository _sessionRepository;
    private readonly IStepDefinitionProvider _definitionProvider;

    public GetStepDetailHandler(
        IWizardSessionRepository wizardSessionRepository,
        IStepAnswerRepository answerRepository,
        ISessionRepository sessionRepository,
        IStepDefinitionProvider definitionProvider)
    {
        _wizardSessionRepository = wizardSessionRepository ?? throw new ArgumentNullException(nameof(wizardSessionRepository));
        _answerRepository = answerRepository ?? throw new ArgumentNullException(nameof(answerRepository));
        _sessionRepository = sessionRepository ?? throw new ArgumentNullException(nameof(sessionRepository));
        _definitionProvider = definitionProvider ?? throw new ArgumentNullException(nameof(definitionProvider));
    }

    public async Task<StepDetailResponse?> HandleAsync(GetStepDetailQuery query, CancellationToken cancellationToken = default)
    {
        var session = await _sessionRepository.GetByIdAsync(query.SessionId, cancellationToken);
        if (session == null)
            throw new InvalidOperationException($"Session {query.SessionId} not found.");

        if (!session.IsValid())
            throw new InvalidOperationException("Session has expired or is no longer valid.");

        var wizardSession = await _wizardSessionRepository.GetBySessionIdAsync(query.SessionId, cancellationToken);
        if (wizardSession == null)
            return null;

        var definition = _definitionProvider.GetById(query.StepId);
        if (definition == null)
            return null;

        var progress = await _wizardSessionRepository
            .GetProgressBySessionAndStepIdAsync(query.SessionId, query.StepId, cancellationToken);

        var progressDto = progress == null
            ? new StepProgressDto
            {
                StepId = query.StepId,
                Status = "not_started",
                LastUpdatedAt = DateTime.UtcNow,
                Version = 0
            }
            : new StepProgressDto
            {
                StepId = progress.StepId,
                Status = MapStepStatus(progress.Status),
                LastUpdatedAt = progress.LastUpdatedAt,
                Version = (uint)progress.Version
            };

        var answer = await _answerRepository.GetBySessionAndStepIdAsync(query.SessionId, query.StepId, cancellationToken);
        StepAnswerDto? answerDto = null;
        if (answer != null)
        {
            using var document = JsonDocument.Parse(answer.AnswerData);
            answerDto = new StepAnswerDto
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

        return new StepDetailResponse
        {
            StepDefinition = MapStepDefinition(definition),
            StepProgress = progressDto,
            StepAnswer = answerDto
        };
    }

    private static StepDefinitionDto MapStepDefinition(StepDefinition definition)
    {
        return new StepDefinitionDto
        {
            StepId = definition.StepId,
            Title = definition.Title,
            Description = definition.Description,
            Fields = definition.Fields.Select(field => new StepFieldDto
            {
                FieldId = field.FieldId,
                Label = field.Label,
                Type = field.Type,
                Required = field.Required,
                HelpText = field.HelpText,
                Options = field.Options?.Select(option => new FieldOptionDto
                {
                    Value = option.Value,
                    Label = option.Label
                }).ToList(),
                Min = field.Min,
                Max = field.Max,
                Pattern = field.Pattern
            }).ToList(),
            ValidationRules = definition.ValidationRules.Select(rule => new ValidationRuleDto
            {
                RuleId = rule.RuleId,
                RuleType = rule.RuleType,
                Parameters = rule.Parameters
            }).ToList(),
            VisibilityRule = definition.VisibilityRule,
            NextStepRules = definition.NextStepRules.Select(rule => new NextStepRuleDto
            {
                Condition = rule.Condition,
                ToStepId = rule.ToStepId
            }).ToList(),
            DisplayMeta = definition.DisplayMeta == null
                ? null
                : JsonSerializer.SerializeToElement(definition.DisplayMeta)
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
