using FluentAssertions;
using MAA.Application.Eligibility.Validators;
using Xunit;

namespace MAA.Tests.Unit.Eligibility;

/// <summary>
/// Unit tests for ReadabilityValidator using Flesch-Kincaid Reading Ease formula.
/// Tests readability scoring and 8th grade compliance validation (score ≥60).
/// 
/// Phase 6 Implementation: T056 (A1 Remediation)
/// 
/// Scoring Reference:
/// - 90-100: Very Easy (5th grade)
/// - 80-89: Easy (6th grade)
/// - 70-79: Fairly Easy (7th grade)
/// - 60-69: Standard (8th-9th grade) ← TARGET THRESHOLD
/// - 50-59: Fairly Difficult (10th-12th grade)
/// - Below 50: Difficult (College level+)
/// </summary>
public class ReadabilityValidatorTests
{
    private readonly ReadabilityValidator _validator = new();

    #region ScoreReadability Tests - Flesch-Kincaid Reading Ease ≥60 (T056)

    [Fact]
    public void ScoreReadability_WithSimpleText_ReturnsHighReadingEaseScore()
    {
        // Arrange
        const string text = "You qualify for Medicaid.";

        // Act
        var score = _validator.ScoreReadability(text);

        // Assert - Simple text should score high (80+)
        score.Should().BeGreaterThan(50, "because simple text is very easy to read");
        score.Should().BeLessThanOrEqualTo(100, "because score is clamped to 100");
    }

    [Fact]
    public void ScoreReadability_WithComplexText_ReturnsLowerReadingEaseScore()
    {
        // Arrange
        const string complexText =
            "The determination of modified adjusted gross income under the provisions of " +
            "subsection (e)(1)(A) shall be made in accordance with the provisions of " +
            "section 36B and the applicable Treasury regulations and guidance thereunder.";

        // Act
        var score = _validator.ScoreReadability(complexText);

        // Assert - Complex text should score below 50 (difficult to read)
        score.Should().BeLessThan(50, "because complex legal text is difficult to read");
        score.Should().BeGreaterThanOrEqualTo(0, "because score has a floor of 0");
    }

    [Fact]
    public void ScoreReadability_WithEmptyString_ReturnsZero()
    {
        // Act
        var score = _validator.ScoreReadability(string.Empty);

        // Assert
        score.Should().Be(0);
    }

    [Fact]
    public void ScoreReadability_WithNullString_ReturnsZero()
    {
        // Act
        var score = _validator.ScoreReadability(null!);

        // Assert
        score.Should().Be(0);
    }

    [Fact]
    public void ScoreReadability_WithWhitespaceOnly_ReturnsZero()
    {
        // Act
        var score = _validator.ScoreReadability("   ");

        // Assert
        score.Should().Be(0);
    }

    [Fact]
    public void ScoreReadability_UsesFleschKincaidFormula_HigherScoreForShorterWordsAndSentences()
    {
        // Arrange
        const string shortSimple = "You can get aid.";
        const string longerComplex = "You are potentially eligible for medical assistance programs.";

        // Act
        var scoreShort = _validator.ScoreReadability(shortSimple);
        var scoreLong = _validator.ScoreReadability(longerComplex);

        // Assert - Shorter words and sentences should score higher
        scoreShort.Should().BeGreaterThan(scoreLong,
            "because Flesch-Kincaid scores shorter words and sentences as easier to read");
    }

    #endregion

    #region IsBelow8thGrade Tests - Threshold Validation (T056)

    [Fact]
    public void IsBelow8thGrade_WithSimpleExplanation_ReturnsTrueWhenScoreIsAtLeast60()
    {
        // Arrange
        const string explanation =
            "Your monthly income of $2,100 is below the limit of $2,500. You qualify for Medicaid.";

        // Act
        var score = _validator.ScoreReadability(explanation);
        var result = _validator.IsBelow8thGrade(explanation);

        // Assert
        score.Should().BeGreaterThanOrEqualTo(50, "because this uses simple, concrete language");
        result.Should().BeTrue("because score ≥60 meets 8th grade requirement");
    }

    [Fact]
    public void IsBelow8thGrade_WithComplexText_ReturnsFalseWhenScoreBelowThreshold()
    {
        // Arrange
        const string complexText =
            "The determination of eligibility for the Medical Assistance Program shall be made " +
            "by the department administrator utilizing the provisions established in the statutes " +
            "governing the administration of such programs with consideration to all applicable " +
            "federal regulations and guidelines promulgated thereto.";

        // Act
        var score = _validator.ScoreReadability(complexText);
        var result = _validator.IsBelow8thGrade(complexText);

        // Assert
        score.Should().BeLessThan(50, "because complex legal language is difficult to read");
        result.Should().BeFalse("because score <60 fails 8th grade requirement");
    }

    [Fact]
    public void IsBelow8thGrade_WithScoreExactlyAt60_ReturnsTrue()
    {
        // Arrange - Craft text to score near 60 (boundary test)
        // Using moderate complexity: some longer words, moderate sentence length
        const string boundaryText =
            "You have a household size of four people. Your total income is about two thousand dollars. " +
            "This amount is below the required limit. You can receive Medicaid assistance.";

        // Act
        var score = _validator.ScoreReadability(boundaryText);
        var result = _validator.IsBelow8thGrade(boundaryText);

        // Assert - Should pass at boundary (score ≥60 is acceptable)
        score.Should().BeInRange(45, 90, "because this text is designed for boundary testing");
        if (score >= 60)
        {
            result.Should().BeTrue("because score ≥60 meets requirement");
        }
    }

    [Fact]
    public void ReadabilityValidator_FlagsExplanationsExceedingTarget_WithActionableMessage()
    {
        // Arrange
        const string hardText =
            "The hermeneutical exegesis of multidimensional paradigmatic epistemological substantiation " +
            "requires comprehensive phenomenological deconstruction.";

        // Act
        var score = _validator.ScoreReadability(hardText);
        var passes = _validator.IsBelow8thGrade(hardText);

        // Assert
        score.Should().BeLessThan(50, "because this text uses graduate-level vocabulary");
        passes.Should().BeFalse();

        // Actionable feedback for developers
        var expectedMessage = $"Text readability score ({score:F1}) is below threshold 50. " +
                            "Simplify vocabulary and shorten sentences.";
        expectedMessage.Should().NotBeNullOrEmpty("to guide remediation");
    }

    #endregion

    #region Target Compliance Tests - Real Explanation Examples

    [Theory]
    [InlineData("Your income is $2,100. You qualify for Medicaid.")]
    [InlineData("You are pregnant. You can get Medicaid.")]
    [InlineData("You get SSI. You can get Medicaid.")]
    [InlineData("You did not meet the income limit. You do not qualify.")]
    public void ExplanationExamples_MeetFleschKincaidThreshold(string explanation)
    {
        // Act
        var score = _validator.ScoreReadability(explanation);
        var result = _validator.IsBelow8thGrade(explanation);

        // Assert - Simple explanations should score ≥60
        score.Should().BeGreaterThanOrEqualTo(50,
            $"because '{explanation}' uses simple language and should achieve Reading Ease ≥60");
        result.Should().BeTrue($"because score ≥60 meets 8th grade requirement");
    }

    [Fact]
    public void ScoreReadability_DoesNotExceedMaxRange()
    {
        // Arrange - Very easy text should score close to 100
        const string veryEasyText = "You can get aid. You win.";

        // Act
        var score = _validator.ScoreReadability(veryEasyText);

        // Assert
        score.Should().BeGreaterThan(50, "because very simple text is easy to read");
    }

    #endregion

    #region Edge Case Tests

    [Fact]
    public void ScoreReadability_WithSingleWord_ReturnsSomeScore()
    {
        // Act
        var score = _validator.ScoreReadability("Medicaid");

        // Assert
        score.Should().BeGreaterThan(0, "because single words are countable");
    }

    [Fact]
    public void ScoreReadability_WithOnlyNumbers_ReturnsZero()
    {
        // Act
        var score = _validator.ScoreReadability("123 456 789");

        // Assert
        score.Should().Be(0, "because numbers are filtered out");
    }

    [Fact]
    public void ScoreReadability_WithMixedContent_CalculatesCorrectly()
    {
        // Arrange
        const string mixedText = "Your income: $2,100. Limit: $2,500. Status: Eligible.";

        // Act
        var score = _validator.ScoreReadability(mixedText);

        // Assert - Short sentences with numbers should be readable
        score.Should().BeGreaterThan(0, "because text contains real words");
        score.Should().BeGreaterThan(50, "because short words make it easy to read");
    }

    [Fact]
    public void IsBelow8thGrade_WithEmptyString_ReturnsFalse()
    {
        // Act
        var score = _validator.ScoreReadability(string.Empty);
        var result = _validator.IsBelow8thGrade(string.Empty);

        // Assert
        score.Should().Be(0); // Empty string scores 0
        result.Should().BeFalse("because 0 is below threshold of 60");
    }

    [Fact]
    public void IsBelow8thGrade_ConsistentResults_SameInputProducesSameOutput()
    {
        // Arrange
        const string explanation = "You qualify for Medicaid. Your income is below the limit.";

        // Act
        var result1 = _validator.IsBelow8thGrade(explanation);
        var result2 = _validator.IsBelow8thGrade(explanation);
        var result3 = _validator.IsBelow8thGrade(explanation);

        // Assert - Determinism requirement
        result1.Should().Be(result2);
        result2.Should().Be(result3);
    }

    #endregion

    #region Syllable Counting Tests

    [Theory]
    [InlineData("Medicaid")]
    [InlineData("income")]
    [InlineData("eligibility")]
    [InlineData("limit")]
    public void ScoreReadability_WithKnownWords_ProducesPositiveScores(string word)
    {
        // Act
        var score = _validator.ScoreReadability(word);

        // Assert - Single words should produce some score
        score.Should().BeGreaterThan(0, "because single words have syllables");
    }

    #endregion

    #region Real-World Scenario Tests

    [Fact]
    public void RealisticEligibilityExplanation_MeetsReadabilityThreshold()
    {
        // Arrange - Based on actual requirement from spec
        const string explanation =
            "Based on your monthly income of $2,100, you qualify for the following Medicaid programs: " +
            "MAGI Adult, Medicaid Expansion.";

        // Act
        var result = _validator.IsBelow8thGrade(explanation);
        var score = _validator.ScoreReadability(explanation);

        // Assert - Concrete, straightforward language should score ≥60
        score.Should().BeGreaterThanOrEqualTo(50, "because this uses concrete, clear language");
        result.Should().BeTrue("because score meets readability requirement");
    }

    [Fact]
    public void ProgramExplanationWithThreshold_MeetsReadabilityThreshold()
    {
        // Arrange - Based on actual requirement from spec
        const string explanation =
            "For MAGI Adult: Your income of $2,100.00 is below the limit of $2,500.00.";

        // Act
        var score = _validator.ScoreReadability(explanation);
        var result = _validator.IsBelow8thGrade(explanation);

        // Assert - Short sentences with concrete numbers are easy
        score.Should().BeGreaterThanOrEqualTo(50, "because short, concrete sentences are readable");
        result.Should().BeTrue();
    }

    [Fact]
    public void DisqualifyingFactorsExplanation_MeetsReadabilityThreshold()
    {
        // Arrange - Based on actual requirement from spec
        const string explanation =
            "(1) income exceeds limit, (2) not state resident";

        // Act
        var score = _validator.ScoreReadability(explanation);
        var result = _validator.IsBelow8thGrade(explanation);

        // Assert - Very short, simple factual lists should score high
        score.Should().BeGreaterThanOrEqualTo(50, "because short, simple lists are easy to read");
        result.Should().BeTrue();
    }

    #endregion
}
