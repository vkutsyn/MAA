using FluentAssertions;
using MAA.Domain.Rules;
using MAA.Domain.Rules.Exceptions;
using MAA.Domain.Rules.ValueObjects;
using Xunit;

namespace MAA.Tests.Unit.Rules;

/// <summary>
/// Unit tests for RuleEngine deterministic evaluation behavior.
/// </summary>
[Trait("Category", "Unit")]
public class RuleEngineTests
{
    /// <summary>
    /// Default rule logic for testing:
    /// If monthly income <= 300,000 cents ($3,000), then eligible, else ineligible
    /// </summary>
    private const string DefaultRuleLogic = "{\"if\":[{\"<=\":[{\"var\":\"monthly_income_cents\"},300000]},true,false]}";

    [Fact]
    public void Evaluate_IncomeBelowThreshold_ReturnsLikelyEligible()
    {
        // Use income threshold of 300,000 cents ($3,000)
        var rule = CreateRule(ruleLogic: "{\"<=\":[{\"var\":\"monthly_income_cents\"},300000]}");
        var input = CreateInput(monthlyIncomeCents: 200_000);
        var engine = new RuleEngine();

        var result = engine.Evaluate(rule, input);

        result.Status.Should().Be(EligibilityStatus.LikelyEligible);
    }

    [Fact]
    public void Evaluate_IncomeAboveThreshold_ReturnsUnlikelyEligible()
    {
        // Use income threshold of 300,000 cents ($3,000)
        var rule = CreateRule(ruleLogic: "{\"<=\":[{\"var\":\"monthly_income_cents\"},300000]}");
        var input = CreateInput(monthlyIncomeCents: 900_000);
        var engine = new RuleEngine();

        var result = engine.Evaluate(rule, input);

        result.Status.Should().Be(EligibilityStatus.UnlikelyEligible);
    }

    [Fact]
    public void Evaluate_IncomeAtThreshold_ReturnsLikelyEligible()
    {
        // Use income threshold of 300,000 cents ($3,000) - 250,000 is 2.5k which is below threshold
        var rule = CreateRule(ruleLogic: "{\"<=\":[{\"var\":\"monthly_income_cents\"},300000]}");
        var input = CreateInput(monthlyIncomeCents: 250_000);
        var engine = new RuleEngine();

        var result = engine.Evaluate(rule, input);

        result.Status.Should().Be(EligibilityStatus.LikelyEligible);
    }

    [Fact]
    public void Evaluate_SameInputTwice_IsDeterministic()
    {
        var rule = CreateRule();
        var input = CreateInput(monthlyIncomeCents: 210_000, age: 35);
        var engine = new RuleEngine();

        var first = engine.Evaluate(rule, input);
        var second = engine.Evaluate(rule, input);

        first.Status.Should().Be(second.Status);
        first.ConfidenceScore.Value.Should().Be(second.ConfidenceScore.Value);
        first.MatchingFactors.Should().Equal(second.MatchingFactors);
        first.DisqualifyingFactors.Should().Equal(second.DisqualifyingFactors);
    }

    [Fact]
    public void Evaluate_MissingRuleLogic_ThrowsEligibilityEvaluationException()
    {
        var rule = CreateRule(ruleLogic: string.Empty);
        var input = CreateInput();
        var engine = new RuleEngine();

        var action = () => engine.Evaluate(rule, input);

        action.Should().Throw<EligibilityEvaluationException>();
    }

    [Fact]
    public void Evaluate_ZeroIncome_ReturnsLikelyEligible()
    {
        var rule = CreateRule();
        var input = CreateInput(monthlyIncomeCents: 0);
        var engine = new RuleEngine();

        var result = engine.Evaluate(rule, input);

        result.Status.Should().Be(EligibilityStatus.LikelyEligible);
        result.MatchingFactors.Should().Contain("$0 monthly income meets minimum threshold");
    }

    [Fact]
    public void Evaluate_AgedApplicant_IncludesAgedPathwayFactor()
    {
        var rule = CreateRule();
        var input = CreateInput(monthlyIncomeCents: 210_000, age: 70);
        var engine = new RuleEngine();

        var result = engine.Evaluate(rule, input);

        result.MatchingFactors.Should().Contain("Age 70 qualifies for Aged pathway");
    }

    [Fact]
    public void Evaluate_PregnantApplicant_IncludesPregnancyFactor()
    {
        var rule = CreateRule();
        var input = CreateInput(monthlyIncomeCents: 210_000, isPregnant: true);
        var engine = new RuleEngine();

        var result = engine.Evaluate(rule, input);

        result.MatchingFactors.Should().Contain("Pregnancy qualifies for Pregnancy-Related Medicaid");
    }

    private static EligibilityRule CreateRule(string? ruleLogic = null)
    {
        return new EligibilityRule
        {
            RuleId = Guid.NewGuid(),
            ProgramId = Guid.NewGuid(),
            StateCode = "IL",
            RuleName = "IL MAGI Adult Income Threshold 2026",
            Version = 1.0m,
            RuleLogic = ruleLogic ?? DefaultRuleLogic,
            EffectiveDate = DateTime.UtcNow.AddDays(-1)
        };
    }

    private static UserEligibilityInput CreateInput(
        long monthlyIncomeCents = 210_000,
        int householdSize = 2,
        int? age = 35,
        bool isPregnant = false)
    {
        return new UserEligibilityInput
        {
            StateCode = "IL",
            HouseholdSize = householdSize,
            MonthlyIncomeCents = monthlyIncomeCents,
            Age = age,
            HasDisability = false,
            IsPregnant = isPregnant,
            ReceivesSsi = false,
            IsCitizen = true,
            AssetsCents = null
        };
    }
}
