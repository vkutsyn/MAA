using System.Text.Json;
using MAA.Application.Wizard.DTOs;
using MAA.Application.Wizard.Repositories;
using MAA.Domain.Repositories;
using MAA.Domain.Wizard;

namespace MAA.Application.Wizard.Queries;

/// <summary>
/// Query to retrieve the next wizard step definition.
/// </summary>
public record GetNextStepQuery
{
    public required Guid SessionId { get; init; }
    public required string CurrentStepId { get; init; }
}

/// <summary>
/// Handler for GetNextStepQuery.
/// </summary>
public class GetNextStepHandler
{
    private readonly IWizardSessionRepository _wizardSessionRepository;
    private readonly IStepAnswerRepository _answerRepository;
    private readonly ISessionRepository _sessionRepository;
    private readonly IStepDefinitionProvider _definitionProvider;
    private readonly StepNavigationEngine _navigationEngine;

    public GetNextStepHandler(
        IWizardSessionRepository wizardSessionRepository,
        IStepAnswerRepository answerRepository,
        ISessionRepository sessionRepository,
        IStepDefinitionProvider definitionProvider,
        StepNavigationEngine navigationEngine)
    {
        _wizardSessionRepository = wizardSessionRepository ?? throw new ArgumentNullException(nameof(wizardSessionRepository));
        _answerRepository = answerRepository ?? throw new ArgumentNullException(nameof(answerRepository));
        _sessionRepository = sessionRepository ?? throw new ArgumentNullException(nameof(sessionRepository));
        _definitionProvider = definitionProvider ?? throw new ArgumentNullException(nameof(definitionProvider));
        _navigationEngine = navigationEngine ?? throw new ArgumentNullException(nameof(navigationEngine));
    }

    public async Task<GetNextStepResponse?> HandleAsync(GetNextStepQuery query, CancellationToken cancellationToken = default)
    {
        var session = await _sessionRepository.GetByIdAsync(query.SessionId, cancellationToken);
        if (session == null)
            throw new InvalidOperationException($"Session {query.SessionId} not found.");

        if (!session.IsValid())
            throw new InvalidOperationException("Session has expired or is no longer valid.");

        var wizardSession = await _wizardSessionRepository.GetBySessionIdAsync(query.SessionId, cancellationToken);
        if (wizardSession == null)
            return null;

        var answers = await _answerRepository.GetBySessionIdAsync(query.SessionId, cancellationToken);
        var answerMap = BuildAnswerSnapshot(answers);

        var nextStep = _navigationEngine.GetNextStep(query.CurrentStepId, answerMap);
        if (nextStep == null)
            return null;

        var progress = await _wizardSessionRepository
            .GetProgressBySessionAndStepIdAsync(query.SessionId, nextStep.StepId, cancellationToken);

        var progressDto = progress == null
            ? new StepProgressDto
            {
                StepId = nextStep.StepId,
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

        return new GetNextStepResponse
        {
            WizardSession = new WizardSessionDto
            {
                SessionId = wizardSession.SessionId,
                CurrentStepId = wizardSession.CurrentStepId,
                LastActivityAt = wizardSession.LastActivityAt,
                Version = (uint)wizardSession.Version
            },
            StepDefinition = MapStepDefinition(nextStep),
            StepProgress = progressDto,
            IsFinal = nextStep.NextStepRules.Count == 0
        };
    }

    private static Dictionary<string, JsonElement> BuildAnswerSnapshot(IEnumerable<MAA.Domain.Wizard.StepAnswer> answers)
    {
        var snapshot = new Dictionary<string, JsonElement>(StringComparer.OrdinalIgnoreCase);

        foreach (var answer in answers)
        {
            using var document = JsonDocument.Parse(answer.AnswerData);
            snapshot[answer.StepId] = document.RootElement.Clone();
        }

        return snapshot;
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
