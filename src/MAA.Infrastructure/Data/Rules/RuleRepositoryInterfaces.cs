namespace MAA.Infrastructure.Data.Rules;

/// <summary>
/// Repository for accessing Medicaid Program data
/// Provides methods for querying programs by state, pathway, and other criteria
/// 
/// Phase 2 Implementation: Not yet implemented
/// Phase 2 data-model.md: Programs are stored in SessionContext DbSet
/// </summary>
public interface IProgramRepository
{
    // Placeholder for Phase 2 implementation
    // Methods like: GetProgramsByState, GetProgramByCode, etc.
}

/// <summary>
/// Repository for accessing Eligibility Rule data
/// Provides methods for retrieving active rules for program evaluation
/// 
/// Phase 2 Implementation: Not yet implemented
/// Phase 3 Implementation: T023 RuleRepository
/// </summary>
public interface IRuleRepository
{
    // Placeholder for Phase 2/3 implementation
    // Methods like: GetActiveRuleByProgram, GetRulesByState, etc.
}

/// <summary>
/// Repository for accessing Federal Poverty Level data
/// Provides methods for FPL lookups by year, household size, and state
/// 
/// Phase 2 Implementation: Not yet implemented
/// Phase 3 Implementation: T024 FPLRepository
/// </summary>
public interface IFplRepository
{
    // Placeholder for Phase 2/3 implementation
    // Methods like: GetFPLByYearAndHouseholdSize, GetFPLForState, etc.
}
