using EligibilityDomain = MAA.Domain.Eligibility;
using MAA.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace MAA.Infrastructure.Eligibility;

public class RuleSetRepository : EligibilityDomain.IRuleSetRepository
{
    private readonly SessionContext _context;

    public RuleSetRepository(SessionContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<EligibilityDomain.RuleSetVersion?> GetActiveRuleSetAsync(
        string stateCode,
        DateTime effectiveDate,
        CancellationToken cancellationToken = default)
    {
        return await _context.EligibilityRuleSetVersions
            .AsNoTracking()
            .Where(ruleSet => ruleSet.StateCode == stateCode)
            .Where(ruleSet => ruleSet.EffectiveDate <= effectiveDate)
            .Where(ruleSet => ruleSet.EndDate == null || ruleSet.EndDate >= effectiveDate)
            .Where(ruleSet => ruleSet.Status == EligibilityDomain.RuleSetStatus.Active)
            .OrderByDescending(ruleSet => ruleSet.EffectiveDate)
            .ThenByDescending(ruleSet => ruleSet.Version)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<EligibilityDomain.RuleSetVersion?> GetRuleSetVersionAsync(
        Guid ruleSetVersionId,
        CancellationToken cancellationToken = default)
    {
        return await _context.EligibilityRuleSetVersions
            .AsNoTracking()
            .FirstOrDefaultAsync(ruleSet => ruleSet.RuleSetVersionId == ruleSetVersionId, cancellationToken);
    }

    public async Task<IReadOnlyList<EligibilityDomain.EligibilityRule>> GetRulesForRuleSetAsync(
        Guid ruleSetVersionId,
        CancellationToken cancellationToken = default)
    {
        return await _context.EligibilityRulesV2
            .AsNoTracking()
            .Include(rule => rule.Program)
            .Where(rule => rule.RuleSetVersionId == ruleSetVersionId)
            .OrderBy(rule => rule.Priority)
            .ToListAsync(cancellationToken);
    }
}
