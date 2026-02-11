using MAA.Application.Eligibility.DTOs;
using MAA.Domain.Eligibility;

namespace MAA.Application.Eligibility.Queries;

public record EvaluateEligibilityQuery
{
    public required EligibilityEvaluateRequestDto Request { get; init; }
}

public class EvaluateEligibilityQueryHandler
{
    private readonly IRuleSetRepository _ruleSetRepository;
    private readonly IEligibilityCacheService _cacheService;
    private readonly EligibilityEvaluator _evaluator;

    public EvaluateEligibilityQueryHandler(
        IRuleSetRepository ruleSetRepository,
        IEligibilityCacheService cacheService,
        EligibilityEvaluator evaluator)
    {
        _ruleSetRepository = ruleSetRepository ?? throw new ArgumentNullException(nameof(ruleSetRepository));
        _cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));
        _evaluator = evaluator ?? throw new ArgumentNullException(nameof(evaluator));
    }

    public async Task<EligibilityEvaluateResponseDto> HandleAsync(
        EvaluateEligibilityQuery query,
        CancellationToken cancellationToken = default)
    {
        if (query == null)
            throw new ArgumentNullException(nameof(query));
        if (query.Request == null)
            throw new ArgumentNullException(nameof(query.Request));

        var stateCode = query.Request.StateCode.Trim().ToUpperInvariant();
        var effectiveDate = query.Request.EffectiveDate;

        var ruleSet = _cacheService.GetRuleSetVersion(stateCode, effectiveDate);
        if (ruleSet == null)
        {
            ruleSet = await _ruleSetRepository.GetActiveRuleSetAsync(stateCode, effectiveDate, cancellationToken);
            if (ruleSet == null)
                throw new KeyNotFoundException("Rules not found for state or effective date.");

            _cacheService.SetRuleSetVersion(stateCode, effectiveDate, ruleSet);
        }

        var rules = _cacheService.GetRules(ruleSet.RuleSetVersionId);
        if (rules == null)
        {
            var loadedRules = await _ruleSetRepository.GetRulesForRuleSetAsync(ruleSet.RuleSetVersionId, cancellationToken);
            if (loadedRules == null || loadedRules.Count == 0)
                throw new KeyNotFoundException("Rules not found for state or effective date.");

            _cacheService.SetRules(ruleSet.RuleSetVersionId, loadedRules);
            rules = loadedRules;
        }

        var domainRequest = new EligibilityRequest
        {
            StateCode = stateCode,
            EffectiveDate = effectiveDate,
            Answers = query.Request.Answers
        };

        var result = _evaluator.Evaluate(domainRequest, ruleSet, rules);

        return new EligibilityEvaluateResponseDto
        {
            Status = result.Status.ToString(),
            MatchedPrograms = result.MatchedPrograms.Select(match => new EligibilityEvaluateProgramMatchDto
            {
                ProgramCode = match.ProgramCode,
                ProgramName = match.ProgramName,
                ConfidenceScore = match.ConfidenceScore,
                Explanation = match.Explanation
            }).ToList(),
            ConfidenceScore = result.ConfidenceScore,
            Explanation = result.Explanation,
            ExplanationItems = result.ExplanationItems.Select(item => new EligibilityExplanationItemDto
            {
                CriterionId = item.CriterionId,
                Message = item.Message,
                Status = item.Status.ToString(),
                GlossaryReference = item.GlossaryReference
            }).ToList(),
            RuleVersionUsed = result.RuleVersionUsed,
            EvaluatedAt = result.EvaluatedAt
        };
    }
}
