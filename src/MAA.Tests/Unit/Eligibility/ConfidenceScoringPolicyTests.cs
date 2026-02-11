using FluentAssertions;
using MAA.Domain.Eligibility;
using Xunit;

namespace MAA.Tests.Unit.Eligibility;

public class ConfidenceScoringPolicyTests
{
    [Fact]
    public void CalculateScore_WithMatchAndAnswers_ReturnsFullScore()
    {
        var policy = new ConfidenceScoringPolicy();
        var answers = new Dictionary<string, object?>
        {
            ["householdSize"] = 2
        };

        var score = policy.CalculateScore(answers, ruleMatched: true);

        score.Should().Be(100);
        policy.GetStatus(score).Should().Be(EligibilityStatus.Likely);
    }

    [Fact]
    public void CalculateScore_WithNoMatch_ReturnsUnlikelyScore()
    {
        var policy = new ConfidenceScoringPolicy();
        var answers = new Dictionary<string, object?>
        {
            ["householdSize"] = 2
        };

        var score = policy.CalculateScore(answers, ruleMatched: false);

        score.Should().Be(50);
        policy.GetStatus(score).Should().Be(EligibilityStatus.Unlikely);
    }
}
