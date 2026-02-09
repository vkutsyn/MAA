namespace MAA.Domain.Rules;

/// <summary>
/// Pure Function: Program Matcher
/// Identifies which Medicaid programs a user qualifies for based on eligibility rules
/// 
/// Phase 4 Implementation: T031
/// 
/// Purpose:
/// - Evaluates user against multiple program rules in parallel
/// - Returns all matches (not just first match)
/// - Provides confidence scoring for each match
/// - Supports multi-program evaluation workflows
/// 
/// Key Properties:
/// - Pure function: Same input always produces same output
/// - No I/O: Does not access database or external services
/// - Deterministic: Results reproducible for audit/testing
/// - Immutable: Does not modify inputs or state
/// 
/// Process:
/// 1. Filter programs by state (already filtered by caller)
/// 2. Evaluate user against each program's rule
/// 3. Collect matching programs (status = Likely/Possibly Eligible)
/// 4. Score confidence for each match
/// 5. Return sorted list (highest confidence first)
/// 
/// Matching Criteria:
/// - Include programs where evaluation status is NOT UnlikelyEligible
/// - Exclude programs that result in UnlikelyEligible status
/// - Collect all matching programs (multiple programs per user possible)
/// 
/// Example Usage:
///   var matcher = new ProgramMatcher();
///   var matches = matcher.FindMatchingPrograms(userInput, allProgramRules);
///   // Returns: [Program A (95% confidence), Program B (75% confidence)]
/// 
/// Reference:
/// - Spec: specs/002-rules-engine/spec.md US2
/// - Data Model: specs/002-rules-engine/data-model.md
/// </summary>
public class ProgramMatcher
{
    private readonly RuleEngine _ruleEngine;
    private readonly ConfidenceScorer _confidenceScorer;

    public ProgramMatcher(RuleEngine ruleEngine, ConfidenceScorer confidenceScorer)
    {
        _ruleEngine = ruleEngine ?? throw new ArgumentNullException(nameof(ruleEngine));
        _confidenceScorer = confidenceScorer ?? throw new ArgumentNullException(nameof(confidenceScorer));
    }

    /// <summary>
    /// Finds all programs that match a user's eligibility profile
    /// </summary>
    /// <param name="input">User eligibility input (state, income, household size, etc.)</param>
    /// <param name="programsWithRules">List of programs with their active rules (pre-filtered by state)</param>
    /// <returns>
    /// List of ProgramMatch objects sorted by confidence score (high to low)
    /// Empty list if no programs match
    /// </returns>
    public List<ProgramMatch> FindMatchingPrograms(
        UserEligibilityInput input,
        IEnumerable<(MedicaidProgram program, EligibilityRule rule)> programsWithRules)
    {
        if (input == null)
            throw new ArgumentNullException(nameof(input));
        if (programsWithRules == null)
            throw new ArgumentNullException(nameof(programsWithRules));

        var matches = new List<ProgramMatch>();

        // Evaluate user against each program's rule
        foreach (var (program, rule) in programsWithRules)
        {
            try
            {
                // Evaluate against the rule
                var evaluationResult = _ruleEngine.Evaluate(rule, input);

                // Include if status is not UnlikelyEligible
                if (evaluationResult.Status != EligibilityStatus.UnlikelyEligible)
                {
                    // Create program match with confidence score
                    var confidenceScore = _confidenceScorer.ScoreConfidence(
                        evaluationResult.MatchingFactors,
                        evaluationResult.DisqualifyingFactors);

                    var match = new ProgramMatch
                    {
                        ProgramId = program.ProgramId,
                        ProgramName = program.ProgramName,
                        ProgramCode = program.ProgramCode,
                        EligibilityPathway = program.EligibilityPathway,
                        Status = evaluationResult.Status,
                        ConfidenceScore = confidenceScore,
                        MatchingFactors = evaluationResult.MatchingFactors,
                        DisqualifyingFactors = evaluationResult.DisqualifyingFactors,
                        RuleVersion = rule.Version,
                        Description = program.Description
                    };

                    matches.Add(match);
                }
            }
            catch (Exception ex)
            {
                // Log but don't fail entire evaluation
                // Programs with evaluation errors are skipped
                System.Diagnostics.Debug.WriteLine(
                    $"Error evaluating program {program.ProgramCode}: {ex.Message}");
                continue;
            }
        }

        // Sort by confidence score descending (highest first)
        return matches
            .OrderByDescending(m => m.ConfidenceScore.Value)
            .ThenBy(m => m.ProgramName)  // Stable sort by name for consistent ordering
            .ToList();
    }

    /// <summary>
    /// Finds the single best matching program (highest confidence score)
    /// Useful for simple single-program evaluation workflows
    /// </summary>
    public ProgramMatch? FindBestMatch(
        UserEligibilityInput input,
        IEnumerable<(MedicaidProgram program, EligibilityRule rule)> programsWithRules)
    {
        return FindMatchingPrograms(input, programsWithRules).FirstOrDefault();
    }
}

/// <summary>
/// Represents a single program match in multi-program evaluation results
/// </summary>
public class ProgramMatch
{
    /// <summary>Program database identifier (GUID)</summary>
    public required Guid ProgramId { get; set; }

    /// <summary>Program name (e.g., "MAGI Adult", "Aged Medicaid")</summary>
    public required string ProgramName { get; set; }

    /// <summary>Program unique code (e.g., "IL_MAGI_ADULT")</summary>
    public string? ProgramCode { get; set; }

    /// <summary>Eligibility pathway for this program</summary>
    public required EligibilityPathway EligibilityPathway { get; set; }

    /// <summary>Overall eligibility status</summary>
    public required EligibilityStatus Status { get; set; }

    /// <summary>Confidence score (0-100) for this match</summary>
    public required ValueObjects.ConfidenceScore ConfidenceScore { get; set; }

    /// <summary>Factors supporting eligibility for this program</summary>
    public required List<string> MatchingFactors { get; set; } = new();

    /// <summary>Factors potentially disqualifying for this program</summary>
    public required List<string> DisqualifyingFactors { get; set; } = new();

    /// <summary>Version of the rule used for evaluation</summary>
    public decimal? RuleVersion { get; set; }

    /// <summary>Plain-language description of the program</summary>
    public string? Description { get; set; }
}
