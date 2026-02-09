using MAA.Domain.Rules.ValueObjects;

namespace MAA.Domain.Rules;

/// <summary>
/// Pure Function: Confidence Scorer
/// Calculates confidence score (0-100) indicating certainty of eligibility determination
/// 
/// Phase 4 Implementation: T033
/// 
/// Purpose:
/// - Quantifies confidence in eligibility assessment
/// - Supports ranking multiple program matches (highest confidence first)
/// - Helps users understand degree of certainty in results
/// - Enables AI/ML integration for predictive scoring in future phases
/// 
/// Key Properties:
/// - Pure function: Same inputs produce same score
/// - Deterministic: Results reproducible for audit
/// - Fast: Simple factor weighting algorithm
/// - Transparent: Score components logged for debugging
/// 
/// Scoring Algorithm:
/// - Base score: 50 (neutral, pending factors)
/// - Matching factors: +10 per factor (up to 95 max)
/// - Disqualifying factors: -15 per factor (down to 25 min)
/// - Categorical eligibility (SSI): +45 bonus (auto 95)
/// - Final score: Clipped to 0-100 range
/// 
/// Confidence Levels:
/// - 90-100: Very High (all factors verify, exact match)
/// - 75-89: High (most factors verified, strong match)
/// - 50-74: Medium (some uncertainty, marginal case)
/// - 25-49: Low (multiple disqualifying factors)
/// - 0-24: Very Low (unlikely eligible)
/// 
/// Example:
/// - User: Income verified + age verified + citizenship verified = 3 factors
///   Score: 50 + (3 × 10) = 80 (High confidence)
/// - User: SSI recipient (categorical)
///   Score: 50 + 45 = 95 (Very High - categorical eligibility)
/// - User: Income over limit + missing docs = 1 matching + 1 disqualifying
///   Score: 50 + 10 - 15 = 45 (Low confidence)
/// 
/// Reference:
/// - Spec: specs/002-rules-engine/spec.md FR-005, SC-006
/// - Data Model: specs/002-rules-engine/data-model.md
/// </summary>
public class ConfidenceScorer
{
    /// <summary>
    /// Base confidence score (neutral starting point)
    /// </summary>
    private const int BASE_SCORE = 50;

    /// <summary>
    /// Points awarded per verified matching factor
    /// </summary>
    private const int POINTS_PER_MATCHING_FACTOR = 10;

    /// <summary>
    /// Points deducted per disqualifying factor
    /// </summary>
    private const int POINTS_PER_DISQUALIFYING_FACTOR = 15;

    /// <summary>
    /// Bonus points for categorical eligibility (SSI recipients)
    /// Categorical eligibility bypasses other requirements
    /// </summary>
    private const int CATEGORICAL_ELIGIBILITY_BONUS = 45;

    /// <summary>
    /// Minimum possible score (floors at this value)
    /// </summary>
    private const int MIN_SCORE = 0;

    /// <summary>
    /// Maximum possible score (ceiling at this value)
    /// </summary>
    private const int MAX_SCORE = 100;

    /// <summary>
    /// Calculates a confidence score based on matching and disqualifying factors
    /// </summary>
    /// <param name="matchingFactors">List of factors supporting eligibility (empty list is valid)</param>
    /// <param name="disqualifyingFactors">List of factors that may disqualify (empty list is valid)</param>
    /// <returns>ConfidenceScore object with value 0-100</returns>
    public ConfidenceScore ScoreConfidence(
        IEnumerable<string> matchingFactors,
        IEnumerable<string> disqualifyingFactors)
    {
        // Null safety: treat null as empty list
        var matching = matchingFactors?.ToList() ?? new List<string>();
        var disqualifying = disqualifyingFactors?.ToList() ?? new List<string>();

        // Check for categorical eligibility indicators
        bool hasCategoricalEligibility = matching
            .Any(f => f.Contains("SSI", StringComparison.OrdinalIgnoreCase) 
              || f.Contains("categorical", StringComparison.OrdinalIgnoreCase)
              || f.Contains("supplemental security income", StringComparison.OrdinalIgnoreCase));

        // Start with base score
        int score = BASE_SCORE;

        // Add points for matching factors
        score += matching.Count * POINTS_PER_MATCHING_FACTOR;

        // Apply categorical eligibility bonus if applicable
        if (hasCategoricalEligibility)
        {
            score += CATEGORICAL_ELIGIBILITY_BONUS;
        }

        // Deduct points for disqualifying factors
        score -= disqualifying.Count * POINTS_PER_DISQUALIFYING_FACTOR;

        // Ensure score stays within 0-100 bounds
        score = Math.Max(MIN_SCORE, Math.Min(MAX_SCORE, score));

        return new ConfidenceScore(score);
    }

    /// <summary>
    /// Calculates confidence score with detailed calculation information
    /// Useful for debugging and explanation generation
    /// </summary>
    public (ConfidenceScore score, ConfidenceCalculationDetails details) ScoreConfidenceDetailed(
        IEnumerable<string> matchingFactors,
        IEnumerable<string> disqualifyingFactors)
    {
        var matching = matchingFactors?.ToList() ?? new List<string>();
        var disqualifying = disqualifyingFactors?.ToList() ?? new List<string>();

        bool hasCategoricalEligibility = matching
            .Any(f => f.Contains("SSI", StringComparison.OrdinalIgnoreCase) 
              || f.Contains("categorical", StringComparison.OrdinalIgnoreCase)
              || f.Contains("supplemental security income", StringComparison.OrdinalIgnoreCase));

        int score = BASE_SCORE;
        var details = new ConfidenceCalculationDetails
        {
            BaseScore = BASE_SCORE,
            MatchingFactorCount = matching.Count,
            DisqualifyingFactorCount = disqualifying.Count,
            MatchingFactorPoints = matching.Count * POINTS_PER_MATCHING_FACTOR,
            DisqualifyingFactorPoints = disqualifying.Count * POINTS_PER_DISQUALIFYING_FACTOR,
            HasCategoricalEligibility = hasCategoricalEligibility,
            CategoricalBonusPoints = hasCategoricalEligibility ? CATEGORICAL_ELIGIBILITY_BONUS : 0
        };

        score += details.MatchingFactorPoints;
        if (hasCategoricalEligibility)
        {
            score += CATEGORICAL_ELIGIBILITY_BONUS;
        }
        score -= details.DisqualifyingFactorPoints;

        score = Math.Max(MIN_SCORE, Math.Min(MAX_SCORE, score));
        details.FinalScore = score;

        return (new ConfidenceScore(score), details);
    }

    /// <summary>
    /// Gets a human-readable confidence level description
    /// </summary>
    public static string GetConfidenceLevelDescription(int score)
    {
        return score switch
        {
            >= 90 => "Very High Confidence",
            >= 75 => "High Confidence",
            >= 50 => "Medium Confidence",
            >= 25 => "Low Confidence",
            _ => "Very Low Confidence"
        };
    }
}

/// <summary>
/// Details of confidence score calculation for debugging and explanation
/// </summary>
public class ConfidenceCalculationDetails
{
    /// <summary>Starting score before any adjustments</summary>
    public int BaseScore { get; set; }

    /// <summary>Number of verified matching factors</summary>
    public int MatchingFactorCount { get; set; }

    /// <summary>Number of disqualifying factors identified</summary>
    public int DisqualifyingFactorCount { get; set; }

    /// <summary>Points added for matching factors</summary>
    public int MatchingFactorPoints { get; set; }

    /// <summary>Points deducted for disqualifying factors</summary>
    public int DisqualifyingFactorPoints { get; set; }

    /// <summary>Categorical eligibility indicator present (SSI, etc.)</summary>
    public bool HasCategoricalEligibility { get; set; }

    /// <summary>Bonus points for categorical eligibility</summary>
    public int CategoricalBonusPoints { get; set; }

    /// <summary>Final score after all adjustments (0-100)</summary>
    public int FinalScore { get; set; }

    /// <summary>Human-readable summary of calculation</summary>
    public override string ToString()
    {
        var parts = new List<string>
        {
            $"Base: {BaseScore}",
            $"Matching factors: +{MatchingFactorPoints}",
            $"Disqualifying factors: -{DisqualifyingFactorPoints}"
        };

        if (HasCategoricalEligibility)
        {
            parts.Add($"Categorical bonus: +{CategoricalBonusPoints}");
        }

        parts.Add($"Final: {FinalScore}");

        return string.Join(" | ", parts);
    }
}
