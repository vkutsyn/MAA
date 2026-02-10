using MAA.Domain.Rules;
using MAA.Infrastructure.Data;
using MAA.Application.Eligibility.Repositories;
using Microsoft.EntityFrameworkCore;

namespace MAA.Infrastructure.DataAccess;

/// <summary>
/// Repository for accessing Eligibility Rules from the database
/// 
/// Phase 3 Implementation: T023
/// 
/// Key Responsibilities:
/// - Query active rules for a program by state
/// - Filter rules by effective date range
/// - Support rule version tracking and lookup
/// - Enable efficient lookups with proper indexing
/// 
/// Database Schema (from SessionContext):
///   eligibility_rules table:
///   - rule_id (PK, Guid)
///   - program_id (FK, Guid)
///   - state_code (string, indexed)
///   - rule_name (string)
///   - version (decimal 4.2)
///   - rule_logic (JSONB)
///   - effective_date (DateTime)
///   - end_date (DateTime, nullable)
///   - is_active (computed from date range)
///   - created_by (string)
///   - created_at (DateTime)
///   - updated_at (DateTime)
/// 
///   Index: (program_id, effective_date, end_date)
/// </summary>
/// <summary>
/// EF Core implementation of rule repository
/// </summary>
public class RuleRepository : IRuleRepository
{
    private readonly SessionContext _context;

    public RuleRepository(SessionContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    /// <summary>
    /// Gets the currently active rule for a program
    /// Filters by:
    /// 1. Program ID match
    /// 2. Effective date: now >= effective_date
    /// 3. End date: end_date is null OR now <= end_date
    /// 4. Latest version
    /// </summary>
    public async Task<EligibilityRule?> GetActiveRuleByProgramAsync(string stateCode, Guid programId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(stateCode, nameof(stateCode));
        ArgumentException.ThrowIfNullOrEmpty(programId.ToString(), nameof(programId));

        var now = DateTime.UtcNow;

        return await _context.EligibilityRules
            .Where(r => r.ProgramId == programId
                     && r.StateCode == stateCode
                     && r.EffectiveDate <= now
                     && (r.EndDate == null || r.EndDate >= now))
            .OrderByDescending(r => r.Version)  // Latest version
            .ThenByDescending(r => r.EffectiveDate)  // Most recent effective date
            .FirstOrDefaultAsync()
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Gets all active rules for a state at the current time
    /// Useful for bulk evaluation when checking all programs
    /// </summary>
    public async Task<IEnumerable<EligibilityRule>> GetRulesByStateAsync(string stateCode)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(stateCode, nameof(stateCode));

        var now = DateTime.UtcNow;

        return await _context.EligibilityRules
            .Where(r => r.StateCode == stateCode
                     && r.EffectiveDate <= now
                     && (r.EndDate == null || r.EndDate >= now))
            .OrderBy(r => r.ProgramId)
            .ThenByDescending(r => r.Version)
            .ToListAsync()
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Gets a specific rule by ID
    /// For audit queries: "What rule was used in evaluation XYZ?"
    /// </summary>
    public async Task<EligibilityRule?> GetByIdAsync(Guid ruleId)
    {
        ArgumentException.ThrowIfNullOrEmpty(ruleId.ToString(), nameof(ruleId));

        return await _context.EligibilityRules
            .FirstOrDefaultAsync(r => r.RuleId == ruleId)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Gets all versions of a rule (version history)
    /// For understanding rule evolution and debugging
    /// </summary>
    public async Task<IEnumerable<EligibilityRule>> GetRuleVersionsAsync(Guid programId)
    {
        ArgumentException.ThrowIfNullOrEmpty(programId.ToString(), nameof(programId));

        return await _context.EligibilityRules
            .Where(r => r.ProgramId == programId)
            .OrderByDescending(r => r.Version)
            .ThenByDescending(r => r.CreatedAt)
            .ToListAsync()
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Gets active rules for a specific eligibility pathway
    /// Performance optimization: some pathways may have specialized rules
    /// </summary>
    public async Task<IEnumerable<EligibilityRule>> GetRulesByPathwayAsync(string stateCode, EligibilityPathway pathway)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(stateCode, nameof(stateCode));

        var now = DateTime.UtcNow;

        return await _context.EligibilityRules
            .Include(r => r.Program)
            .Where(r => r.StateCode == stateCode
                     && r.Program.EligibilityPathway == pathway
                     && r.EffectiveDate <= now
                     && (r.EndDate == null || r.EndDate >= now))
            .OrderByDescending(r => r.Version)
            .ToListAsync()
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Gets all programs with their active rules for a state
    /// Returns tuples of (MedicaidProgram, EligibilityRule) for multi-program evaluation
    /// 
    /// Used by Phase 4 multi-program matching for bulk evaluation
    /// </summary>
    public async Task<List<(MedicaidProgram program, EligibilityRule rule)>> GetProgramsWithActiveRulesByStateAsync(string stateCode)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(stateCode, nameof(stateCode));

        var now = DateTime.UtcNow;

        var activeRules = await _context.EligibilityRules
            .Include(r => r.Program)
            .Where(r => r.StateCode == stateCode
                     && r.EffectiveDate <= now
                     && (r.EndDate == null || r.EndDate >= now))
            .OrderByDescending(r => r.Version)
            .ThenByDescending(r => r.EffectiveDate)
            .ToListAsync()
            .ConfigureAwait(false);

        // Group by program and take latest version per program
        var latestRulesByProgram = activeRules
            .GroupBy(r => r.ProgramId)
            .Select(g => g.First())  // Already ordered by version descending
            .ToList();

        // Return tuples of program + rule
        var results = new List<(MedicaidProgram program, EligibilityRule rule)>();
        foreach (var rule in latestRulesByProgram)
        {
            if (rule.Program != null)
            {
                results.Add((rule.Program, rule));
            }
        }

        return results;
    }
}
