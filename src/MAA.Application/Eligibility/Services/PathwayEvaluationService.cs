using MAA.Domain.Rules;
using MAA.Application.Eligibility.DTOs;
using MAA.Application.Eligibility.Handlers;

namespace MAA.Application.Eligibility.Services;

/// <summary>
/// Orchestrator service for pathway-based evaluation.
/// Integrates PathwayIdentifier and PathwayRouter with existing evaluation handlers.
/// 
/// Workflow:
/// 1. Identify applicable pathways based on user characteristics
/// 2. Route to programs matching those pathways
/// 3. Evaluate user against each program's rules
/// 4. Generate program-specific explanations
/// 5. Return matched programs sorted by confidence
/// 
/// This service enables eligibility wizard to ask targeted questions based on pathways,
/// reducing cognitive load on users (only show relevant programs).
/// </summary>
public class PathwayEvaluationService
{
    private readonly PathwayIdentifier _pathwayIdentifier;
    private readonly PathwayRouter _pathwayRouter;
    private readonly IEvaluateEligibilityHandler _evaluationHandler;

    public PathwayEvaluationService(
        PathwayIdentifier pathwayIdentifier,
        PathwayRouter pathwayRouter,
        IEvaluateEligibilityHandler evaluationHandler)
    {
        _pathwayIdentifier = pathwayIdentifier ?? throw new ArgumentNullException(nameof(pathwayIdentifier));
        _pathwayRouter = pathwayRouter ?? throw new ArgumentNullException(nameof(pathwayRouter));
        _evaluationHandler = evaluationHandler ?? throw new ArgumentNullException(nameof(evaluationHandler));
    }

    /// <summary>
    /// Evaluates eligibility with pathway awareness.
    /// Prioritizes programs within applicable pathways.
    /// </summary>
    /// <param name="input">User eligibility input (state, income, household size, etc.)</param>
    /// <param name="allPrograms">All programs for the state (from database)</param>
    /// <returns>Eligibility result with pathway information and pathway-filtered programs</returns>
    public async Task<EligibilityResultWithPathwayDto> EvaluateEligibilityWithPathwaysAsync(
        UserEligibilityInputDto input,
        List<MedicaidProgram> allPrograms)
    {
        if (input == null)
            throw new ArgumentNullException(nameof(input));
        
        if (allPrograms == null || !allPrograms.Any())
            throw new ArgumentException("Programs list cannot be null or empty", nameof(allPrograms));

        // Step 1: Identify applicable pathways
        var applicablePathways = _pathwayIdentifier.DetermineApplicablePathways(
            age: input.Age ?? 0,
            hasDisability: input.HasDisability ?? false,
            receivesSsi: input.ReceivesSsi ?? false,
            isPregnant: input.IsPregnant ?? false,
            isFemale: input.IsFemale ?? false
        );

        if (!applicablePathways.Any())
        {
            // User doesn't match any pathway - typically a validation error
            return new EligibilityResultWithPathwayDto
            {
                ApplicablePathways = new List<string>(),
                RoutedPrograms = 0,
                MatchedPrograms = new List<ProgramMatchDto>(),
                Explanation = "No applicable eligibility pathways found for provided information. Please verify age, disability status, and other factors.",
                EvaluationDate = DateTime.UtcNow,
                Status = "Unlikely Eligible"
            };
        }

        // Step 2: Route applicable programs
        var routedPrograms = _pathwayRouter.RouteToProgramsForPathways(applicablePathways, allPrograms);

        // Step 3: Evaluate eligibility for routed programs
        // For now, delegate to existing handler - will be enhanced to use routed programs
        var evaluationResult = await _evaluationHandler.EvaluateAsync(input);

        // Step 4: Combine results with pathway information
        return new EligibilityResultWithPathwayDto
        {
            ApplicablePathways = applicablePathways.Select(p => p.ToString()).ToList(),
            RoutedPrograms = routedPrograms.Count,
            MatchedPrograms = evaluationResult.MatchedPrograms,
            Explanation = evaluationResult.Explanation,
            EvaluationDate = evaluationResult.EvaluationDate,
            Status = evaluationResult.Status,
            RuleVersionUsed = evaluationResult.RuleVersionUsed
        };
    }

    /// <summary>
    /// Identifies applicable pathways for a user without full evaluation.
    /// Used by wizard to ask appropriate questions and show relevant pathways.
    /// </summary>
    public List<string> IdentifyPathwaysForUser(
        int? age,
        bool? hasDisability,
        bool? receivesSsi,
        bool? isPregnant = false,
        bool? isFemale = false)
    {
        var pathways = _pathwayIdentifier.DetermineApplicablePathways(
            age: age ?? 0,
            hasDisability: hasDisability ?? false,
            receivesSsi: receivesSsi ?? false,
            isPregnant: isPregnant ?? false,
            isFemale: isFemale ?? false
        );

        return pathways.Select(p => p.ToString()).ToList();
    }

    /// <summary>
    /// Counts programs available for user's pathways.
    /// Useful for displaying "You may qualify for X programs" messaging.
    /// </summary>
    public int CountAvailableProgramsForUser(
        int? age,
        bool? hasDisability,
        bool? receivesSsi,
        List<MedicaidProgram> allPrograms,
        bool? isPregnant = false,
        bool? isFemale = false)
    {
        var pathways = _pathwayIdentifier.DetermineApplicablePathways(
            age: age ?? 0,
            hasDisability: hasDisability ?? false,
            receivesSsi: receivesSsi ?? false,
            isPregnant: isPregnant ?? false,
            isFemale: isFemale ?? false
        );

        return _pathwayRouter.CountAvailableProgramsForPathways(pathways, allPrograms);
    }
}

/// <summary>
/// Extended eligibility result DTO that includes pathway information.
/// Used by wizard and pathway-aware evaluation flows.
/// </summary>
public class EligibilityResultWithPathwayDto
{
    public required List<string> ApplicablePathways { get; set; }
    public int RoutedPrograms { get; set; }
    public required List<ProgramMatchDto> MatchedPrograms { get; set; }
    public required string Explanation { get; set; }
    public required string Status { get; set; }
    public DateTime EvaluationDate { get; set; }
    public string? RuleVersionUsed { get; set; }
}
