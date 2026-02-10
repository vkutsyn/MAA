using MAA.Application.DTOs;
using MAA.Application.Interfaces;
using MAA.Application.Validation;
using Microsoft.Extensions.Logging;

namespace MAA.Application.Handlers;

/// <summary>
/// Query to retrieve question definitions by state and program.
/// </summary>
public record GetQuestionDefinitionsQuery
{
    public required string StateCode { get; init; }
    public required string ProgramCode { get; init; }
}

/// <summary>
/// Handler for retrieving question definitions.
/// </summary>
public class GetQuestionDefinitionsHandler
{
    private readonly IQuestionRepository _questionRepository;
    private readonly IQuestionDefinitionsCache _cache;
    private readonly StateProgramValidator _validator;
    private readonly ConditionalRuleValidator _ruleValidator;
    private readonly ILogger<GetQuestionDefinitionsHandler> _logger;

    public GetQuestionDefinitionsHandler(
        IQuestionRepository questionRepository,
        IQuestionDefinitionsCache cache,
        StateProgramValidator validator,
        ConditionalRuleValidator ruleValidator,
        ILogger<GetQuestionDefinitionsHandler> logger)
    {
        _questionRepository = questionRepository ?? throw new ArgumentNullException(nameof(questionRepository));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _validator = validator ?? throw new ArgumentNullException(nameof(validator));
        _ruleValidator = ruleValidator ?? throw new ArgumentNullException(nameof(ruleValidator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<GetQuestionsResponse> HandleAsync(
        GetQuestionDefinitionsQuery query,
        CancellationToken cancellationToken = default)
    {
        var (stateCode, programCode) = await _validator.ValidateAsync(
            query.StateCode,
            query.ProgramCode,
            cancellationToken);

        var cached = await _cache.GetAsync(stateCode, programCode, cancellationToken);
        if (cached != null)
        {
            _logger.LogDebug("Question definitions cache hit for {StateCode}/{ProgramCode}", stateCode, programCode);
            return MapResponse(stateCode, programCode, cached.Questions, cached.ConditionalRules);
        }

        _logger.LogDebug("Question definitions cache miss for {StateCode}/{ProgramCode}", stateCode, programCode);

        var questions = await _questionRepository.GetQuestionsAsync(stateCode, programCode, cancellationToken);
        var ruleIds = questions
            .Where(q => q.ConditionalRuleId.HasValue)
            .Select(q => q.ConditionalRuleId!.Value)
            .Distinct()
            .ToList();

        var rules = await _questionRepository.GetConditionalRulesAsync(ruleIds, cancellationToken);

        _ruleValidator.Validate(questions.ToList(), rules.ToList());

        var cacheEntry = new QuestionDefinitionsCacheEntry(questions, rules, DateTime.UtcNow);
        await _cache.SetAsync(stateCode, programCode, cacheEntry, cancellationToken);

        return MapResponse(stateCode, programCode, questions, rules);
    }

    private static GetQuestionsResponse MapResponse(
        string stateCode,
        string programCode,
        IReadOnlyList<MAA.Domain.Question> questions,
        IReadOnlyList<MAA.Domain.ConditionalRule> rules)
    {
        var questionDtos = questions
            .OrderBy(q => q.DisplayOrder)
            .Select(q =>
            {
                IReadOnlyList<QuestionOptionDto>? options = null;
                if (q.Options != null && q.Options.Count > 0)
                {
                    options = q.Options
                        .OrderBy(o => o.DisplayOrder)
                        .Select(o => new QuestionOptionDto
                        {
                            OptionId = o.OptionId,
                            OptionLabel = o.OptionLabel,
                            OptionValue = o.OptionValue,
                            DisplayOrder = o.DisplayOrder
                        })
                        .ToList();
                }

                return new QuestionDto
                {
                    QuestionId = q.QuestionId,
                    DisplayOrder = q.DisplayOrder,
                    QuestionText = q.QuestionText,
                    FieldType = q.FieldType.ToString().ToLowerInvariant(),
                    IsRequired = q.IsRequired,
                    HelpText = q.HelpText,
                    ValidationRegex = q.ValidationRegex,
                    ConditionalRuleId = q.ConditionalRuleId,
                    Options = options
                };
            })
            .ToList();

        var ruleDtos = rules
            .Select(r => new ConditionalRuleDto
            {
                ConditionalRuleId = r.ConditionalRuleId,
                RuleExpression = r.RuleExpression,
                Description = r.Description
            })
            .ToList();

        return new GetQuestionsResponse
        {
            StateCode = stateCode,
            ProgramCode = programCode,
            Questions = questionDtos,
            ConditionalRules = ruleDtos
        };
    }
}
