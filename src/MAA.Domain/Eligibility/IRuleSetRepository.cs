namespace MAA.Domain.Eligibility;

public interface IRuleSetRepository
{
    Task<RuleSetVersion?> GetActiveRuleSetAsync(
        string stateCode,
        DateTime effectiveDate,
        CancellationToken cancellationToken = default);

    Task<RuleSetVersion?> GetRuleSetVersionAsync(
        Guid ruleSetVersionId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<EligibilityRule>> GetRulesForRuleSetAsync(
        Guid ruleSetVersionId,
        CancellationToken cancellationToken = default);
}
