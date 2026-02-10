using FluentAssertions;
using MAA.Domain.Rules;
using Xunit;

namespace MAA.Tests.Domain;

[Trait("Category", "Unit")]
public class ConditionalRuleEvaluatorTests
{
    [Fact]
    public void Evaluate_StringEquality_ReturnsTrueWhenMatch()
    {
        var questionId = Guid.NewGuid();
        var expression = $"{questionId} == 'yes'";
        var answers = new Dictionary<Guid, string?>
        {
            [questionId] = "yes"
        };

        var result = ConditionalRuleEvaluator.Evaluate(expression, answers);

        result.Should().BeTrue();
    }

    [Fact]
    public void Evaluate_StringEquality_ReturnsFalseWhenNoMatch()
    {
        var questionId = Guid.NewGuid();
        var expression = $"{questionId} == 'yes'";
        var answers = new Dictionary<Guid, string?>
        {
            [questionId] = "no"
        };

        var result = ConditionalRuleEvaluator.Evaluate(expression, answers);

        result.Should().BeFalse();
    }

    [Fact]
    public void Evaluate_NumericComparison_ReturnsExpectedResult()
    {
        var questionId = Guid.NewGuid();
        var expression = $"{questionId} >= 10";
        var answers = new Dictionary<Guid, string?>
        {
            [questionId] = "10"
        };

        var result = ConditionalRuleEvaluator.Evaluate(expression, answers);

        result.Should().BeTrue();
    }

    [Fact]
    public void Evaluate_AndOrLogic_ReturnsTrueWhenAnyBranchMatches()
    {
        var questionA = Guid.NewGuid();
        var questionB = Guid.NewGuid();
        var questionC = Guid.NewGuid();
        var expression = $"{questionA} == 'yes' AND ({questionB} > 5 OR {questionC} == 'maybe')";

        var answers = new Dictionary<Guid, string?>
        {
            [questionA] = "yes",
            [questionB] = "3",
            [questionC] = "maybe"
        };

        var result = ConditionalRuleEvaluator.Evaluate(expression, answers);

        result.Should().BeTrue();
    }

    [Fact]
    public void Evaluate_NotOperator_InvertsResult()
    {
        var questionId = Guid.NewGuid();
        var expression = $"NOT ({questionId} == 'no')";
        var answers = new Dictionary<Guid, string?>
        {
            [questionId] = "yes"
        };

        var result = ConditionalRuleEvaluator.Evaluate(expression, answers);

        result.Should().BeTrue();
    }

    [Fact]
    public void Evaluate_InOperator_ReturnsTrueForMatch()
    {
        var questionId = Guid.NewGuid();
        var expression = $"{questionId} IN ['a','b','c']";
        var answers = new Dictionary<Guid, string?>
        {
            [questionId] = "b"
        };

        var result = ConditionalRuleEvaluator.Evaluate(expression, answers);

        result.Should().BeTrue();
    }

    [Fact]
    public void Evaluate_MissingAnswer_ReturnsFalse()
    {
        var questionId = Guid.NewGuid();
        var expression = $"{questionId} == 'yes'";

        var result = ConditionalRuleEvaluator.Evaluate(expression, new Dictionary<Guid, string?>());

        result.Should().BeFalse();
    }

    [Fact]
    public void Evaluate_InvalidExpression_Throws()
    {
        var questionId = Guid.NewGuid();
        var expression = $"{questionId} ==";

        var action = () => ConditionalRuleEvaluator.Evaluate(expression, new Dictionary<Guid, string?>());

        action.Should().Throw<ArgumentException>();
    }
}
