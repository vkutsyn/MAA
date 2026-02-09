using MAA.Domain.Rules;

namespace MAA.Application.Eligibility.Repositories;

/// <summary>
/// Repository interface for accessing Eligibility Rules
/// Defined in Application layer (dependency inversion)
/// Implemented in Infrastructure layer
/// </summary>
public interface IRuleRepository
{
    Task<EligibilityRule?> GetActiveRuleByProgramAsync(string stateCode, Guid programId);
    Task<IEnumerable<EligibilityRule>> GetRulesByStateAsync(string stateCode);
    Task<EligibilityRule?> GetByIdAsync(Guid ruleId);
    Task<IEnumerable<EligibilityRule>> GetRuleVersionsAsync(Guid programId);
    Task<IEnumerable<EligibilityRule>> GetRulesByPathwayAsync(string stateCode, EligibilityPathway pathway);
}

/// <summary>
/// Repository interface for accessing Federal Poverty Level data
/// Defined in Application layer (dependency inversion)
/// Implemented in Infrastructure layer
/// </summary>
public interface IFplRepository
{
    Task<FederalPovertyLevel?> GetFplByYearAndHouseholdSizeAsync(int year, int householdSize);
    Task<FederalPovertyLevel?> GetFplForStateAsync(int year, int householdSize, string stateCode);
    Task<IEnumerable<FederalPovertyLevel>> GetFplsByYearAsync(int year);
    Task<IEnumerable<FederalPovertyLevel>> GetBaselineFplsByYearAsync(int year);
    Task<IEnumerable<FederalPovertyLevel>> GetStateFplAdjustmentsByYearAsync(int year, string stateCode);
}
