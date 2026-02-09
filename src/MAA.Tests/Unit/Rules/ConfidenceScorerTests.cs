using FluentAssertions;
using MAA.Domain.Rules;
using MAA.Domain.Rules.ValueObjects;
using Xunit;

namespace MAA.Tests.Unit.Rules;

/// <summary>
/// Unit tests for ConfidenceScorer pure function
/// Tests confidence score calculation based on matching and disqualifying factors
/// 
/// Phase 4 Implementation: T035
/// 
/// Test Coverage:
/// - Base score calculation
/// - Matching factor contributions (+10 per factor)
/// - Disqualifying factor deductions (-15 per factor)
/// - Categorical eligibility bonus (+45)
/// - Score bounds (0-100)
/// - Edge cases (empty lists, many factors)
/// 
/// Scoring Algorithm:
/// - Base: 50 points
/// - Per matching factor: +10 points
/// - Per disqualifying factor: -15 points
/// - Categorical eligibility: +45 bonus (SSI, etc.)
/// - Final: Clipped to [0, 100]
/// </summary>
[Trait("Category", "Unit")]
public class ConfidenceScorerTests
{
    [Fact]
    public void ScoreConfidence_NoFactors_ReturnsBaseScore()
    {
        var scorer = new ConfidenceScorer();
        var matchingFactors = new List<string>();
        var disqualifyingFactors = new List<string>();

        var result = scorer.ScoreConfidence(matchingFactors, disqualifyingFactors);

        result.Value.Should().Be(50, "Base score with no factors should be 50");
    }

    [Fact]
    public void ScoreConfidence_AllFactorsPresent_ReturnsHighConfidence()
    {
        var scorer = new ConfidenceScorer();
        var matchingFactors = new List<string>
        {
            "Income verified",
            "Citizenship confirmed",
            "Household size documented"
        };
        var disqualifyingFactors = new List<string>();

        var result = scorer.ScoreConfidence(matchingFactors, disqualifyingFactors);

        // 50 (base) + 3 * 10 (factors) = 80
        result.Value.Should().Be(80);
        result.Category.Should().Be("High");
    }

    [Fact]
    public void ScoreConfidence_SingleMatchingFactor_Returns60()
    {
        var scorer = new ConfidenceScorer();
        var matchingFactors = new List<string> { "Income meets threshold" };
        var disqualifyingFactors = new List<string>();

        var result = scorer.ScoreConfidence(matchingFactors, disqualifyingFactors);

        // 50 + 10 = 60
        result.Value.Should().Be(60);
    }

    [Fact]
    public void ScoreConfidence_MultipleDisqualifyingFactors_ReducesScore()
    {
        var scorer = new ConfidenceScorer();
        var matchingFactors = new List<string>();
        var disqualifyingFactors = new List<string>
        {
            "Income exceeds limit",
            "Not documented as disabled"
        };

        var result = scorer.ScoreConfidence(matchingFactors, disqualifyingFactors);

        // 50 - 2 * 15 = 20
        result.Value.Should().Be(20);
        result.Category.Should().Be("Very Low");
    }

    [Fact]
    public void ScoreConfidence_CategoricalEligibilitySSI_AddsBonus()
    {
        var scorer = new ConfidenceScorer();
        var matchingFactors = new List<string>
        {
            "Receives Supplemental Security Income (SSI)"
        };
        var disqualifyingFactors = new List<string>();

        var result = scorer.ScoreConfidence(matchingFactors, disqualifyingFactors);

        // 50 + 10 (factor) + 45 (categorical bonus) = 95
        result.Value.Should().Be(95);
        result.Category.Should().Be("Very High");
    }

    [Fact]
    public void ScoreConfidence_CategoricalKeywordVariations_AllDetected()
    {
        var scorer = new ConfidenceScorer();
        
        var testCases = new List<string>
        {
            "Supplemental Security Income (SSI)",
            "SSI",
            "Categorical eligibility confirmed",
            "ssi recipient",  // lowercase
            "Receives SSI benefits"
        };

        foreach (var keyword in testCases)
        {
            var matchingFactors = new List<string> { keyword };
            var disqualifyingFactors = new List<string>();

            var result = scorer.ScoreConfidence(matchingFactors, disqualifyingFactors);

            result.Value.Should().BeGreaterThanOrEqualTo(95, 
                $"Should detect categorical eligibility in: '{keyword}'");
        }
    }

    [Fact]
    public void ScoreConfidence_MixedFactors_CalculatesCorrectly()
    {
        var scorer = new ConfidenceScorer();
        var matchingFactors = new List<string>
        {
            "Income within limit",
            "State resident"
        };
        var disqualifyingFactors = new List<string>
        {
            "Asset test pending"
        };

        var result = scorer.ScoreConfidence(matchingFactors, disqualifyingFactors);

        // 50 + (2 * 10) - (1 * 15) = 55
        result.Value.Should().Be(55);
    }

    [Fact]
    public void ScoreConfidence_ScoreNeverExceeds100()
    {
        var scorer = new ConfidenceScorer();
        var matchingFactors = Enumerable.Range(0, 20)
            .Select(i => $"Match factor {i}")
            .ToList();
        var disqualifyingFactors = new List<string>();

        var result = scorer.ScoreConfidence(matchingFactors, disqualifyingFactors);

        result.Value.Should().Be(100, "Score should never exceed 100");
    }

    [Fact]
    public void ScoreConfidence_ScoreNeverBelowZero()
    {
        var scorer = new ConfidenceScorer();
        var matchingFactors = new List<string>();
        var disqualifyingFactors = Enumerable.Range(0, 20)
            .Select(i => $"Disqualifying factor {i}")
            .ToList();

        var result = scorer.ScoreConfidence(matchingFactors, disqualifyingFactors);

        result.Value.Should().Be(0, "Score should never go below 0");
    }

    [Fact]
    public void ScoreConfidence_NullLists_TreatsAsEmpty()
    {
        var scorer = new ConfidenceScorer();

        var result = scorer.ScoreConfidence(null, null);

        result.Value.Should().Be(50, "Null lists should be treated as empty");
    }

    [Theory]
    [InlineData(90, "Very High")]
    [InlineData(80, "High")]
    [InlineData(60, "Medium")]
    [InlineData(35, "Low")]
    [InlineData(0, "Very Low")]
    public void ScoreConfidence_ResturnsCorrectConfidenceLevel(int expectedScore, string expectedCategory)
    {
        var scorer = new ConfidenceScorer();
        
        // Build factors to achieve target score
        int baseScore = 50;
        int targetFactors = (expectedScore - baseScore) / 10;
        
        var matchingFactors = Enumerable
            .Range(0, Math.Max(0, targetFactors))
            .Select(i => $"Factor {i}")
            .ToList();
        
        var disqualifyingFactors = targetFactors < 0
            ? Enumerable
                .Range(0, Math.Abs(targetFactors))
                .Select(i => $"Disqualifying {i}")
                .ToList()
            : new List<string>();

        var result = scorer.ScoreConfidence(matchingFactors, disqualifyingFactors);

        result.Value.Should().Be(expectedScore);
        result.Category.Should().Be(expectedCategory);
    }

    [Fact]
    public void ScoreConfidenceDetailed_ProvidesCalculationBreakdown()
    {
        var scorer = new ConfidenceScorer();
        var matchingFactors = new List<string> { "Match 1", "Match 2" };
        var disqualifyingFactors = new List<string> { "Disqualify 1" };

        var (score, details) = scorer.ScoreConfidenceDetailed(matchingFactors, disqualifyingFactors);

        details.BaseScore.Should().Be(50);
        details.MatchingFactorCount.Should().Be(2);
        details.DisqualifyingFactorCount.Should().Be(1);
        details.MatchingFactorPoints.Should().Be(20);    // 2 * 10
        details.DisqualifyingFactorPoints.Should().Be(15); // 1 * 15
        details.FinalScore.Should().Be(55);  // 50 + 20 - 15
        score.Value.Should().Be(55);
    }

    [Fact]
    public void ScoreConfidenceDetailed_WithCategoricalEligibility_IncludesBonus()
    {
        var scorer = new ConfidenceScorer();
        var matchingFactors = new List<string> { "SSI recipient" };
        var disqualifyingFactors = new List<string>();

        var (score, details) = scorer.ScoreConfidenceDetailed(matchingFactors, disqualifyingFactors);

        details.HasCategoricalEligibility.Should().Be(true);
        details.CategoricalBonusPoints.Should().Be(45);
        details.FinalScore.Should().Be(95);  // 50 + 10 + 45

        var explanation = details.ToString();
        explanation.Should().Contain("95");
        explanation.Should().Contain("Categorical");
    }

    [Fact]
    public void GetConfidenceLevelDescription_AllRanges_ReturnsCorrectDescription()
    {
        ConfidenceScorer.GetConfidenceLevelDescription(95).Should().Be("Very High Confidence");
        ConfidenceScorer.GetConfidenceLevelDescription(76).Should().Be("High Confidence");
        ConfidenceScorer.GetConfidenceLevelDescription(60).Should().Be("Medium Confidence");
        ConfidenceScorer.GetConfidenceLevelDescription(40).Should().Be("Low Confidence");
        ConfidenceScorer.GetConfidenceLevelDescription(10).Should().Be("Very Low Confidence");
    }

    [Fact]
    public void ConfidenceScore_Value_Object_IsComparable()
    {
        var score1 = new ConfidenceScore(75);
        var score2 = new ConfidenceScore(50);
        var score3 = new ConfidenceScore(75);

        score1.CompareTo(score2).Should().BePositive();
        score2.CompareTo(score1).Should().BeNegative();
        score1.CompareTo(score3).Should().Be(0);
    }

    [Fact]
    public void ConfidenceScore_Value_Object_IsEquatable()
    {
        var score1 = new ConfidenceScore(75);
        var score2 = new ConfidenceScore(75);
        var score3 = new ConfidenceScore(50);

        score1.Equals(score2).Should().Be(true);
        score1.Equals(score3).Should().Be(false);
        (score1 == score2).Should().Be(true);
        (score1 != score3).Should().Be(true);
    }

    [Fact]
    public void ScoreConfidence_DeterministicBehavior_SameInputProducesSameOutput()
    {
        var scorer = new ConfidenceScorer();
        var matchingFactors = new List<string> { "Factor A", "Factor B" };
        var disqualifyingFactors = new List<string> { "Disqual X" };

        // Evaluate multiple times
        var result1 = scorer.ScoreConfidence(matchingFactors, disqualifyingFactors);
        var result2 = scorer.ScoreConfidence(matchingFactors, disqualifyingFactors);
        var result3 = scorer.ScoreConfidence(matchingFactors, disqualifyingFactors);

        result1.Value.Should().Be(result2.Value);
        result2.Value.Should().Be(result3.Value);
        result1.Value.Should().Be(55); // Verify absolute value
    }
}
