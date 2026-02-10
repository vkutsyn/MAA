using MAA.Application.Interfaces;
using MAA.Domain;
using MAA.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace MAA.Infrastructure.Repositories;

/// <summary>
/// Repository implementation for eligibility question definitions.
/// </summary>
public class QuestionRepository : IQuestionRepository
{
    private readonly SessionContext _context;

    public QuestionRepository(SessionContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<IReadOnlyList<Question>> GetQuestionsAsync(
        string stateCode,
        string programCode,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(stateCode) || string.IsNullOrWhiteSpace(programCode))
            return Array.Empty<Question>();

        return await _context.Questions
            .AsNoTracking()
            .Where(q => q.StateCode == stateCode && q.ProgramCode == programCode)
            .Include(q => q.Options)
            .Include(q => q.ConditionalRule)
            .OrderBy(q => q.DisplayOrder)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ConditionalRule>> GetConditionalRulesAsync(
        IEnumerable<Guid> ruleIds,
        CancellationToken cancellationToken = default)
    {
        var ruleIdList = ruleIds?.Distinct().ToList() ?? new List<Guid>();
        if (ruleIdList.Count == 0)
            return Array.Empty<ConditionalRule>();

        return await _context.ConditionalRules
            .AsNoTracking()
            .Where(r => ruleIdList.Contains(r.ConditionalRuleId))
            .ToListAsync(cancellationToken);
    }
}
