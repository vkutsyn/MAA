using MAA.Domain.Rules.ValueObjects;
using MAA.Domain.Rules.Exceptions;

namespace MAA.Domain.Rules;

/// <summary>
/// Pure Eligibility Rule Engine
/// Evaluates a user against a single eligibility rule using JSONLogic
/// 
/// Phase 3 Implementation: T019
/// 
/// Key Properties:
/// - Pure function: Same input always produces same output
/// - No I/O: Does not access database, cache, or external services
/// - No dependencies: Only uses rule logic and user input
/// - Deterministic: Results can be reproduced exactly for audit/testing
/// 
/// Usage Example:
///   var ruleEngine = new RuleEngine();
///   var result = ruleEngine.Evaluate(rule, userInput);
///   if (result.Status == EligibilityStatus.LikelyEligible)
///   {
///       // User qualifies
///   }
/// </summary>
public class RuleEngine
{
    /// <summary>
    /// Evaluates whether a user matches an eligibility rule
    /// </summary>
    /// <param name="rule">The eligibility rule to evaluate against</param>
    /// <param name="input">The user input data to evaluate</param>
    /// <returns>Evaluation result with status and reasoning</returns>
    public EligibilityRuleEvaluationResult Evaluate(EligibilityRule rule, UserEligibilityInput input)
    {
        if (rule == null)
            throw new ArgumentNullException(nameof(rule));
        if (input == null)
            throw new ArgumentNullException(nameof(input));

        var startTime = DateTime.UtcNow;

        try
        {
            // Parse and evaluate JSONLogic from the rule
            var ruleLogic = ParseRuleLogic(rule.RuleLogic);
            
            // Build evaluation context with user input
            var evaluationContext = BuildEvaluationContext(input);

            // Evaluate against rule logic
            var ruleResult = EvaluateJsonLogic(ruleLogic, evaluationContext);

            // Determine eligibility status and factors
            var (status, confidence, matchingFactors, disqualifyingFactors) = 
                DetermineEligibilityFromResult(ruleResult, input);

            return new EligibilityRuleEvaluationResult
            {
                Status = status,
                ConfidenceScore = new ConfidenceScore(confidence),
                EvaluatedAt = DateTime.UtcNow,
                EvaluationDurationMs = (int)(DateTime.UtcNow - startTime).TotalMilliseconds,
                MatchingFactors = matchingFactors,
                DisqualifyingFactors = disqualifyingFactors,
                RuleUsed = rule,
                InputUsed = input
            };
        }
        catch (Exception ex)
        {
            throw new EligibilityEvaluationException(
                $"Failed to evaluate rule '{rule.RuleName}' version {rule.Version}: {ex.Message}", 
                ex);
        }
    }

    /// <summary>
    /// Parses a rule logic JSON string into an evaluable form
    /// Placeholder for actual JSONLogic integration
    /// </summary>
    private object ParseRuleLogic(string ruleLogicJson)
    {
        if (string.IsNullOrWhiteSpace(ruleLogicJson))
            throw new EligibilityEvaluationException("Rule logic cannot be empty");

        // TODO: Parse JSONLogic from JSON string
        // For now, return the JSON string itself; actual implementation will parse to JSONLogic objects
        // This is where JSONLogic.Net library will be integrated
        return ruleLogicJson;
    }

    /// <summary>
    /// Builds the evaluation context dictionary from user input
    /// These are the variables available to the rule logic engine
    /// </summary>
    private Dictionary<string, object> BuildEvaluationContext(UserEligibilityInput input)
    {
        var context = new Dictionary<string, object>
        {
            { "monthly_income_cents", input.MonthlyIncomeCents },
            { "household_size", input.HouseholdSize },
            { "age", input.Age },
            { "has_disability", input.HasDisability },
            { "is_pregnant", input.IsPregnant },
            { "receives_ssi", input.ReceivesSsi },
            { "is_citizen", input.IsCitizen },
            { "assets_cents", input.AssetsCents ?? 0L },
            { "current_date", DateTime.UtcNow }
        };

        return context;
    }

    /// <summary>
    /// Evaluates the parsed rule logic against the context
    /// Placeholder for actual JSONLogic evaluation
    /// </summary>
    private (bool passed, string explanation) EvaluateJsonLogic(object ruleLogic, Dictionary<string, object> context)
    {
        // TODO: Integrate JSONLogic.Net evaluation
        // For now, this is a placeholder that always returns true
        // Actual implementation will use JsonLogic library to evaluate rule against context
        return (true, "Rule evaluation passed");
    }

    /// <summary>
    /// Determines eligibility status based on rule evaluation result
    /// </summary>
    private (EligibilityStatus status, int confidence, List<string> matching, List<string> disqualifying) 
        DetermineEligibilityFromResult(
            (bool passed, string explanation) ruleResult,
            UserEligibilityInput input)
    {
        var matchingFactors = new List<string>();
        var disqualifyingFactors = new List<string>();

        // Analyze factors from input
        AnalyzeIncomeFactors(input, ruleResult.passed, matchingFactors, disqualifyingFactors);
        AnalyzeDemographicFactors(input, ruleResult.passed, matchingFactors, disqualifyingFactors);
        AnalyzeCategoricalEligibility(input, ruleResult.passed, matchingFactors, disqualifyingFactors);

        // Determine status and confidence
        var (status, confidence) = ComputeEligibilityStatus(ruleResult.passed, matchingFactors, disqualifyingFactors);

        return (status, confidence, matchingFactors, disqualifyingFactors);
    }

    /// <summary>
    /// Analyzes income-related factors
    /// </summary>
    private void AnalyzeIncomeFactors(
        UserEligibilityInput input,
        bool rulesPassed,
        List<string> matchingFactors,
        List<string> disqualifyingFactors)
    {
        if (input.MonthlyIncomeCents == 0)
        {
            if (rulesPassed)
                matchingFactors.Add("$0 monthly income meets minimum threshold");
            else
                disqualifyingFactors.Add("$0 income does not meet requirement");
        }
        else if (input.MonthlyIncomeCents > 0)
        {
            if (rulesPassed)
                matchingFactors.Add($"Monthly income of ${input.MonthlyIncomeCents / 100m:F2} meets threshold");
            else
                disqualifyingFactors.Add($"Monthly income of ${input.MonthlyIncomeCents / 100m:F2} exceeds limit");
        }
    }

    /// <summary>
    /// Analyzes demographic factors (age, disability, pregnancy)
    /// </summary>
    private void AnalyzeDemographicFactors(
        UserEligibilityInput input,
        bool rulesPassed,
        List<string> matchingFactors,
        List<string> disqualifyingFactors)
    {
        if (input.Age.HasValue)
        {
            if (input.Age >= 65)
            {
                if (rulesPassed)
                    matchingFactors.Add($"Age {input.Age} qualifies for Aged pathway");
            }
            else if (input.Age < 18)
            {
                if (rulesPassed)
                    matchingFactors.Add($"Age {input.Age} qualifies for Child pathway");
            }
        }

        if (input.HasDisability && rulesPassed)
            matchingFactors.Add("Disability status qualifies for Disabled pathway");

        if (input.IsPregnant && rulesPassed)
            matchingFactors.Add("Pregnancy qualifies for Pregnancy-Related Medicaid");
    }

    /// <summary>
    /// Analyzes categorical eligibility (SSI, etc.)
    /// </summary>
    private void AnalyzeCategoricalEligibility(
        UserEligibilityInput input,
        bool rulesPassed,
        List<string> matchingFactors,
        List<string> disqualifyingFactors)
    {
        if (input.ReceivesSsi && rulesPassed)
            matchingFactors.Add("SSI receipt provides categorical eligibility");

        if (!input.IsCitizen && !rulesPassed)
            disqualifyingFactors.Add("Non-citizen status may limit eligibility (Emergency Medicaid only)");
    }

    /// <summary>
    /// Computes final eligibility status and confidence score
    /// </summary>
    private (EligibilityStatus status, int confidence) ComputeEligibilityStatus(
        bool rulesPassed,
        List<string> matchingFactors,
        List<string> disqualifyingFactors)
    {
        if (rulesPassed)
        {
            if (disqualifyingFactors.Count == 0)
            {
                // All factors positive - high confidence likely eligible
                return (EligibilityStatus.LikelyEligible, 95);
            }
            else if (disqualifyingFactors.Count == 1)
            {
                // Some uncertainty but more positive factors - medium confidence possibly eligible
                return (EligibilityStatus.PossiblyEligible, 75);
            }
            else
            {
                // Multiple concerns - low confidence possibly eligible
                return (EligibilityStatus.PossiblyEligible, 50);
            }
        }
        else
        {
            // Rule did not pass
            if (disqualifyingFactors.Count > 0)
            {
                // Clear reasons for ineligibility - high confidence unlikely eligible
                return (EligibilityStatus.UnlikelyEligible, 15);
            }
            else
            {
                // Unclear but didn't pass - low confidence unlikely eligible
                return (EligibilityStatus.UnlikelyEligible, 35);
            }
        }
    }
}

/// <summary>
/// Result of evaluating a single eligibility rule
/// Contains all information about why a user was/wasn't eligible
/// </summary>
public class EligibilityRuleEvaluationResult
{
    /// <summary>
    /// Overall eligibility status for this rule/program
    /// </summary>
    public required EligibilityStatus Status { get; set; }

    /// <summary>
    /// Confidence score (0-100) for this evaluation
    /// </summary>
    public required ConfidenceScore ConfidenceScore { get; set; }

    /// <summary>
    /// When the evaluation occurred
    /// </summary>
    public DateTime EvaluatedAt { get; set; }

    /// <summary>
    /// How long the evaluation took (milliseconds)
    /// Used for performance monitoring
    /// </summary>
    public int EvaluationDurationMs { get; set; }

    /// <summary>
    /// Factors that made user eligible
    /// </summary>
    public List<string> MatchingFactors { get; set; } = new();

    /// <summary>
    /// Factors that might disqualify user
    /// </summary>
    public List<string> DisqualifyingFactors { get; set; } = new();

    /// <summary>
    /// The rule that was used for this evaluation
    /// Useful for audit trail
    /// </summary>
    public EligibilityRule? RuleUsed { get; set; }

    /// <summary>
    /// The user input used for this evaluation
    /// Useful for reproducibility and debugging
    /// </summary>
    public UserEligibilityInput? InputUsed { get; set; }
}

/// <summary>
/// Simple user eligibility input structure used by RuleEngine
/// (Alternative to the DTO for domain logic)
/// </summary>
public class UserEligibilityInput
{
    public required string StateCode { get; set; }
    public required int HouseholdSize { get; set; }
    public required long MonthlyIncomeCents { get; set; }
    public int? Age { get; set; }
    public bool HasDisability { get; set; }
    public bool IsPregnant { get; set; }
    public bool ReceivesSsi { get; set; }
    public required bool IsCitizen { get; set; }
    public long? AssetsCents { get; set; }
    public DateTime CurrentDate { get; set; } = DateTime.UtcNow;
}
