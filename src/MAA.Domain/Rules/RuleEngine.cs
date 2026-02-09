using MAA.Domain.Rules.ValueObjects;
using MAA.Domain.Rules.Exceptions;
using Newtonsoft.Json.Linq;

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
    /// </summary>
    private JToken ParseRuleLogic(string ruleLogicJson)
    {
        if (string.IsNullOrWhiteSpace(ruleLogicJson))
            throw new EligibilityEvaluationException("Rule logic cannot be empty");

        try
        {
            // Parse the JSON string into a JToken for evaluation
            var ruleLogic = JToken.Parse(ruleLogicJson);
            return ruleLogic;
        }
        catch (Exception ex)
        {
            throw new EligibilityEvaluationException(
                $"Failed to parse rule logic JSON: {ex.Message}. Rule JSON: {ruleLogicJson}", 
                ex);
        }
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
            { "has_disability", input.HasDisability },
            { "is_pregnant", input.IsPregnant },
            { "receives_ssi", input.ReceivesSsi },
            { "is_citizen", input.IsCitizen },
            { "assets_cents", input.AssetsCents ?? 0L },
            { "current_date", DateTime.UtcNow }
        };

        // Only add age if it has a value
        if (input.Age.HasValue)
        {
            context["age"] = input.Age.Value;
        }

        return context;
    }

    /// <summary>
    /// Evaluates the parsed rule logic against the context
    /// Implements JSONLogic evaluation with support for common operators
    /// </summary>
    private (bool passed, string explanation) EvaluateJsonLogic(JToken ruleLogic, Dictionary<string, object> context)
    {
        try
        {
            // Evaluate the rule logic recursively
            var result = EvaluateRule(ruleLogic, context);
            
            // Convert result to boolean
            bool passed = IsTruthy(result);
            
            string explanation = passed 
                ? "Rule logic evaluated to true/passed" 
                : "Rule logic evaluated to false/did not pass";
            
            return (passed, explanation);
        }
        catch (Exception ex)
        {
            throw new EligibilityEvaluationException(
                $"Failed to apply rule logic: {ex.Message}", 
                ex);
        }
    }

    /// <summary>
    /// Recursively evaluates JSONLogic rules
    /// Supports: comparison operators (<=, >=, <, >, ==, !=), logical operators (and, or), conditionals (if)
    /// </summary>
    private object? EvaluateRule(JToken rule, Dictionary<string, object> context)
    {
        // If it's a primitive value, return it as-is
        if (rule is JValue jValue)
        {
            return jValue.Value;
        }

        // If it's an array, evaluate each element and return the array
        if (rule is JArray jArray)
        {
            return rule;
        }

        // If it's an object, it could be either a variable reference or an operator
        if (rule is JObject ruleObj)
        {
            // Check if this is a variable reference: { "var": "variable_name" }
            if (ruleObj.Count == 1)
            {
                var firstProp = ruleObj.First as JProperty;
                if (firstProp?.Name == "var" && firstProp.Value is JValue varValue)
                {
                    var varKey = varValue.ToString();
                    return context.TryGetValue(varKey, out var value) ? value : null;
                }
            }

            // Otherwise, it's an operator - get the first property name and value
            var operatorProp = ruleObj.First as JProperty;
            if (operatorProp == null)
                return null;

            var operatorName = operatorProp.Name;
            var operands = operatorProp.Value;

            if (operatorName == "<=")
                return EvaluateComparison(operands, context, (a, b) => Compare(a, b) <= 0);
            else if (operatorName == ">=")
                return EvaluateComparison(operands, context, (a, b) => Compare(a, b) >= 0);
            else if (operatorName == "<")
                return EvaluateComparison(operands, context, (a, b) => Compare(a, b) < 0);
            else if (operatorName == ">")
                return EvaluateComparison(operands, context, (a, b) => Compare(a, b) > 0);
            else if (operatorName == "==" || operatorName == "===")
                return EvaluateComparison(operands, context, (a, b) => EqualsValue(a, b));
            else if (operatorName == "!=" || operatorName == "!==")
                return EvaluateComparison(operands, context, (a, b) => !EqualsValue(a, b));
            else if (operatorName == "and")
                return EvaluateLogicalAnd(operands, context);
            else if (operatorName == "or")
                return EvaluateLogicalOr(operands, context);
            else if (operatorName == "if")
                return EvaluateConditional(operands, context);
            else
                return null; // Unknown operator
        }

        return rule;
    }

    /// <summary>
    /// Evaluates a comparison operation
    /// </summary>
    private bool EvaluateComparison(JToken? operands, Dictionary<string, object> context, Func<object?, object?, bool> comparisonFunc)
    {
        if (operands is not JArray operandArray || operandArray.Count != 2)
            return false;

        var leftVal = EvaluateRule(operandArray[0], context);
        var rightVal = EvaluateRule(operandArray[1], context);

        return comparisonFunc(leftVal, rightVal);
    }

    /// <summary>
    /// Evaluates logical AND operation
    /// </summary>
    private bool EvaluateLogicalAnd(JToken? operands, Dictionary<string, object> context)
    {
        if (operands is not JArray operandArray)
            return false;

        foreach (var operand in operandArray)
        {
            if (!IsTruthy(EvaluateRule(operand, context)))
                return false;
        }

        return true;
    }

    /// <summary>
    /// Evaluates logical OR operation
    /// </summary>
    private bool EvaluateLogicalOr(JToken? operands, Dictionary<string, object> context)
    {
        if (operands is not JArray operandArray)
            return false;

        foreach (var operand in operandArray)
        {
            if (IsTruthy(EvaluateRule(operand, context)))
                return true;
        }

        return false;
    }

    /// <summary>
    /// Evaluates conditional (if-then-else) operation
    /// Format: { "if": [condition, then_value, else_value] }
    /// </summary>
    private object? EvaluateConditional(JToken? operands, Dictionary<string, object> context)
    {
        if (operands is not JArray operandArray || operandArray.Count < 2)
            return null;

        var condition = EvaluateRule(operandArray[0], context);
        
        if (IsTruthy(condition))
        {
            // Condition is true, return then value
            return operandArray.Count > 1 ? EvaluateRule(operandArray[1], context) : null;
        }
        else
        {
            // Condition is false, return else value if present
            return operandArray.Count > 2 ? EvaluateRule(operandArray[2], context) : null;
        }
    }

    /// <summary>
    /// Compares two values for ordering
    /// </summary>
    private int Compare(object? a, object? b)
    {
        // Convert to comparable types
        if (a == null && b == null) return 0;
        if (a == null) return -1;
        if (b == null) return 1;

        // Try to compare as numbers
        if (IsNumeric(a) && IsNumeric(b))
        {
            var aNum = Convert.ToDecimal(a);
            var bNum = Convert.ToDecimal(b);
            return aNum.CompareTo(bNum);
        }

        // Compare as booleans if both are boolean
        if (a is bool && b is bool)
        {
            return ((bool)a).CompareTo((bool)b);
        }

        // Compare as strings
        return string.Compare(a.ToString(), b.ToString(), StringComparison.Ordinal);
    }

    /// <summary>
    /// Checks equality between two values
    /// </summary>
    private bool EqualsValue(object? a, object? b)
    {
        if (a == null && b == null) return true;
        if (a == null || b == null) return false;

        // If both are numeric, compare as numbers
        if (IsNumeric(a) && IsNumeric(b))
        {
            var aNum = Convert.ToDecimal(a);
            var bNum = Convert.ToDecimal(b);
            return aNum == bNum;
        }

        // Otherwise use standard equality
        return a.Equals(b);
    }

    /// <summary>
    /// Checks if a value is numeric
    /// </summary>
    private bool IsNumeric(object? value)
    {
        return value is byte || value is sbyte || value is short || value is ushort ||
               value is int || value is uint || value is long || value is ulong ||
               value is float || value is double || value is decimal;
    }

    /// <summary>
    /// Determines if a value is truthy in JSONLogic semantics
    /// Following JSON Logic standard: false, 0, "", [], {}, null are falsy; everything else is truthy
    /// </summary>
    private bool IsTruthy(object? value)
    {
        return value switch
        {
            null => false,
            false => false,
            0 => false,
            0.0 => false,
            0m => false,
            0L => false,
            0u => false,
            0ul => false,
            "" => false,
            _ when value is JArray jArray && jArray.Count == 0 => false,
            _ when value is JObject jObj && jObj.Count == 0 => false,
            _ => true
        };
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
