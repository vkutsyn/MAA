using MAA.Application.Eligibility.DTOs;
using MAA.Application.Eligibility.Caching;
using MAA.Application.Eligibility.Repositories;
using MAA.Domain.Rules;
using MAA.Domain.Rules.Exceptions;

namespace MAA.Application.Eligibility.Handlers;

/// <summary>
/// Application Layer Handler for Multi-Program Matching
/// 
/// Phase 4 Implementation: T032
/// 
/// Responsibilities:
/// - Orchestrates evaluation across ALL programs in a state (not just first match)
/// - Fetches all active program rules for user's state
/// - Evaluates user against each program's rule using RuleEngine
/// - Applies asset evaluation for non-MAGI pathways
/// - Scores confidence for each match
/// - Sorts results by confidence (highest first)
/// - Returns comprehensive list of all matching programs
/// 
/// Process Flow:
/// 1. Validate input (state code, required fields)
/// 2. Fetch all active programs and rules for state (with cache)
/// 3. For each program:
///    a. Evaluate basic eligibility via RuleEngine
///    b. If non-MAGI pathway: Evaluate assets via AssetEvaluator
///    c. Score confidence via ConfidenceScorer
///    d. Collect match if eligible
/// 4. Sort all matches by confidence descending
/// 5. Return comprehensive result with all matches + any failed evaluations
/// 
/// Design Patterns:
/// - Handler pattern: Orchestrates multiple services
/// - Dependency injection: All services injected via constructor
/// - Pure functions: Calls deterministic RuleEngine, ConfidenceScorer
/// - Repository pattern: Abstracts database access
/// - Cache pattern: Lazy cache population on first lookup
/// 
/// Performance Target:
/// - ≤2 seconds (p95) for multi-program evaluation with 1,000 concurrent users
/// - Cache hit rate: 90-95% (after first user per state)
/// - Database queries: 1-2 per state (fetch rules, not repeated for each evaluation)
/// 
/// Error Handling:
/// - ArgumentException: Invalid input parameters
/// - EligibilityEvaluationException: Rule evaluation failure (logged but not blocking)
/// - Individual program evaluation failures don't fail entire flow
/// 
/// Difference from EvaluateEligibilityHandler (T021):
/// - T021: Single program evaluation (detail view)
/// - T032: Multi-program matching (primary evaluation endpoint, returns all matches)
/// 
/// Note: This replaces the primary /api/rules/evaluate endpoint usage
/// T021 is reserved for detailed single-program analysis
/// </summary>
public interface IProgramMatchingHandler
{
    /// <summary>
    /// Evaluates a user's eligibility across ALL programs in their state
    /// Returns all matching programs sorted by confidence (high to low)
    /// </summary>
    /// <param name="input">User input DTO with demographics and state selection</param>
    /// <returns>Result with matched programs and any failed evaluations</returns>
    Task<EligibilityResultDto> EvaluateMultiProgramAsync(UserEligibilityInputDto input);
}

/// <summary>
/// Concrete handler implementation for multi-program matching
/// </summary>
public class ProgramMatchingHandler : IProgramMatchingHandler
{
    private readonly IRuleRepository _ruleRepository;
    private readonly IFplRepository _fplRepository;
    private readonly IRuleCacheService _cacheService;
    private readonly RuleEngine _ruleEngine;
    private readonly FPLCalculator _fplCalculator;
    private readonly ProgramMatcher _programMatcher;
    private readonly ConfidenceScorer _confidenceScorer;

    public ProgramMatchingHandler(
        IRuleRepository ruleRepository,
        IFplRepository fplRepository,
        IRuleCacheService cacheService,
        RuleEngine ruleEngine,
        FPLCalculator fplCalculator,
        ProgramMatcher programMatcher,
        ConfidenceScorer confidenceScorer)
    {
        _ruleRepository = ruleRepository ?? throw new ArgumentNullException(nameof(ruleRepository));
        _fplRepository = fplRepository ?? throw new ArgumentNullException(nameof(fplRepository));
        _cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));
        _ruleEngine = ruleEngine ?? throw new ArgumentNullException(nameof(ruleEngine));
        _fplCalculator = fplCalculator ?? throw new ArgumentNullException(nameof(fplCalculator));
        _programMatcher = programMatcher ?? throw new ArgumentNullException(nameof(programMatcher));
        _confidenceScorer = confidenceScorer ?? throw new ArgumentNullException(nameof(confidenceScorer));
    }

    /// <summary>
    /// Evaluates a user against all programs in their state
    /// Collects all matching programs and sorts by confidence
    /// </summary>
    public async Task<EligibilityResultDto> EvaluateMultiProgramAsync(UserEligibilityInputDto input)
    {
        if (input == null)
            throw new ArgumentNullException(nameof(input));

        var startTime = DateTime.UtcNow;

        try
        {
            // Validate input (performed by validator middleware, but double-check here)
            ValidateInput(input);

            // Convert DTO to domain input
            var domainInput = ConvertToUserEligibilityInput(input);

            // Fetch all active programs and rules for state (with cache check)
            var programsWithRules = await FetchProgramsForStateAsync(input.StateCode);

            if (!programsWithRules.Any())
            {
                throw new EligibilityEvaluationException(
                    $"No programs found for state '{input.StateCode}'. Please contact support.");
            }

            // Use ProgramMatcher to find all matches
            var matches = _programMatcher.FindMatchingPrograms(domainInput, programsWithRules);

            // Check for asset eligibility for non-MAGI pathways
            var matchesWithAssetCheck = await ApplyAssetCheckAsync(matches, domainInput);

            // Determine overall eligibility status
            var overallStatus = DetermineOverallStatus(matchesWithAssetCheck);

            // Calculate overall confidence as average of all matches (or 0 if no matches)
            int overallConfidence = matchesWithAssetCheck.Any()
                ? (int)Math.Round(matchesWithAssetCheck.Average(m => m.ConfidenceScore.Value))
                : 0;

            // Convert matches to DTOs
            var matchDtos = matchesWithAssetCheck
                .Select(m => new ProgramMatchDto
                {
                    ProgramId = m.ProgramId,
                    ProgramName = m.ProgramName,
                    EligibilityPathway = m.EligibilityPathway.ToString(),
                    ConfidenceScore = m.ConfidenceScore.Value,
                    MatchingFactors = m.MatchingFactors,
                    DisqualifyingFactors = m.DisqualifyingFactors,
                    EligibilityStatus = m.Status.ToString(),
                    Explanation = GenerateProgramExplanation(m, input),
                    RuleVersionUsed = m.RuleVersion,
                    EvaluatedAt = DateTime.UtcNow
                })
                .OrderByDescending(m => m.ConfidenceScore)
                .ToList();

            // Build result DTO
            var result = new EligibilityResultDto
            {
                EvaluationDate = DateTime.UtcNow,
                StateCode = input.StateCode,
                OverallStatus = overallStatus,
                ConfidenceScore = overallConfidence,
                Explanation = GenerateOverallExplanation(matchDtos, input),
                MatchedPrograms = matchDtos.Where(m => m.EligibilityStatus == "LikelyEligible" || m.EligibilityStatus == "PossiblyEligible").ToList(),
                FailedProgramEvaluations = matchDtos.Where(m => m.EligibilityStatus == "UnlikelyEligible").ToList(),
                UserInputSummary = $"Household: {input.HouseholdSize}, Income: ${input.MonthlyIncomeCents / 100}M"
            };

            return result;
        }
        catch (Exception ex)
        {
            throw new EligibilityEvaluationException(
                $"Multi-program evaluation failed for state '{input.StateCode}': {ex.Message}",
                ex);
        }
    }

    /// <summary>
    /// Validates input before processing
    /// </summary>
    private void ValidateInput(UserEligibilityInputDto input)
    {
        if (string.IsNullOrEmpty(input.StateCode))
            throw new ArgumentException("State code is required", nameof(input.StateCode));

        var validStates = new[] { "IL", "CA", "NY", "TX", "FL" };
        if (!validStates.Contains(input.StateCode.ToUpperInvariant()))
            throw new ArgumentException(
                $"State '{input.StateCode}' is not supported. Pilot states: {string.Join(", ", validStates)}",
                nameof(input.StateCode));

        if (input.HouseholdSize < 1 || input.HouseholdSize > 20)
            throw new ArgumentException("Household size must be between 1 and 20", nameof(input.HouseholdSize));

        if (input.MonthlyIncomeCents < 0)
            throw new ArgumentException("Income cannot be negative", nameof(input.MonthlyIncomeCents));
    }

    /// <summary>
    /// Converts DTO to domain entity
    /// </summary>
    private UserEligibilityInput ConvertToUserEligibilityInput(UserEligibilityInputDto input)
    {
        return new UserEligibilityInput
        {
            StateCode = input.StateCode,
            HouseholdSize = input.HouseholdSize,
            MonthlyIncomeCents = input.MonthlyIncomeCents,
            Age = input.Age > 0 ? input.Age : null,
            HasDisability = input.HasDisability,
            IsPregnant = input.IsPregnant,
            ReceivesSsi = input.ReceivesSsi,
            IsCitizen = input.IsCitizen,
            AssetsCents = input.AssetsCents,
            CurrentDate = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Fetches all active programs and their rules for the state
    /// </summary>
    private async Task<List<(MedicaidProgram program, EligibilityRule rule)>> FetchProgramsForStateAsync(string stateCode)
    {
        // Fetch from repository (caching handled at individual rule level)
        var programs = await _ruleRepository.GetProgramsWithActiveRulesByStateAsync(stateCode);
        return programs;
    }

    /// <summary>
    /// Applies asset evaluation for non-MAGI pathways
    /// Filters out matches that fail asset test
    /// </summary>
    private async Task<List<ProgramMatch>> ApplyAssetCheckAsync(
        List<ProgramMatch> matches,
        UserEligibilityInput input)
    {
        var result = new List<ProgramMatch>();

        foreach (var match in matches)
        {
            // Only apply asset check for non-MAGI pathways
            if (match.EligibilityPathway is EligibilityPathway.NonMAGI_Aged or EligibilityPathway.NonMAGI_Disabled)
            {
                var (isAssetEligible, assetReason) = AssetEvaluator.EvaluateAssets(
                    input.AssetsCents ?? 0,
                    match.EligibilityPathway,
                    input.StateCode,
                    DateTime.UtcNow.Year);

                if (!isAssetEligible)
                {
                    // Add asset disqualification reason
                    match.DisqualifyingFactors.Add(assetReason);
                    match.Status = EligibilityStatus.UnlikelyEligible;

                    // Lower confidence score due to asset failure
                    var newScore = Math.Max(0, match.ConfidenceScore.Value - 25);
                    match.ConfidenceScore = new Domain.Rules.ValueObjects.ConfidenceScore(newScore);
                    continue;  // Skip this match, don't add to results
                }
            }

            result.Add(match);
        }

        return result;
    }

    /// <summary>
    /// Determines overall eligibility status based on all program matches
    /// </summary>
    private string DetermineOverallStatus(List<ProgramMatch> matches)
    {
        if (!matches.Any())
            return "Unlikely Eligible";

        // If any program shows Likely Eligible, overall is Likely
        if (matches.Any(m => m.Status == EligibilityStatus.LikelyEligible))
            return "Likely Eligible";

        // If any program shows Possibly Eligible, overall is Possibly
        if (matches.Any(m => m.Status == EligibilityStatus.PossiblyEligible))
            return "Possibly Eligible";

        // All programs Unlikely
        return "Unlikely Eligible";
    }

    /// <summary>
    /// Generates plain-language overall explanation
    /// </summary>
    private string GenerateOverallExplanation(List<ProgramMatchDto> matches, UserEligibilityInputDto input)
    {
        if (!matches.Any())
        {
            return $"Based on your household information (income of ${input.MonthlyIncomeCents / 100m:F2}/month, " +
                   $"household size of {input.HouseholdSize} in {input.StateCode}), you may not qualify for Medicaid at this time. " +
                   $"Please contact your state Medicaid office for more information or if circumstances change.";
        }

        var likelyPrograms = matches.Count(m => m.EligibilityStatus == "LikelyEligible");
        var possiblyPrograms = matches.Count(m => m.EligibilityStatus == "PossiblyEligible");

        if (likelyPrograms > 0)
        {
            return $"Good news! You appear to qualify for {likelyPrograms} Medicaid program(s). " +
                   $"Explore your options below and select the program that best fits your needs.";
        }

        if (possiblyPrograms > 0)
        {
            return $"You may qualify for Medicaid, but some verification is needed. " +
                   $"We've identified {possiblyPrograms} potential program(s) below. Contact your state Medicaid office for more details.";
        }

        return "No matching programs found at this time. Please contact your state Medicaid office for assistance.";
    }

    /// <summary>
    /// Generates program-specific explanation
    /// </summary>
    private string GenerateProgramExplanation(ProgramMatch match, UserEligibilityInputDto input)
    {
        var explanation = $"{match.ProgramName}: ";

        if (match.Status == EligibilityStatus.LikelyEligible)
        {
            explanation += "You appear to qualify for this program based on your income and household composition.";
        }
        else if (match.Status == EligibilityStatus.PossiblyEligible)
        {
            explanation += "You may qualify, but additional verification is needed.";
        }
        else
        {
            explanation += "You do not appear to qualify for this program at this time.";
        }

        // Add disqualifying factors if present
        if (match.DisqualifyingFactors.Any())
        {
            explanation += $" Reason: {string.Join("; ", match.DisqualifyingFactors)}";
        }

        return explanation;
    }
}
