using FluentAssertions;
using MAA.Domain.Eligibility;
using Xunit;

namespace MAA.Tests.Unit.Eligibility;

public class EligibilityEvaluatorTests
{
    [Fact]
    public void Evaluate_WithMatchingRule_ReturnsLikelyMatch()
    {
        var policy = new ConfidenceScoringPolicy();
        var evaluator = new EligibilityEvaluator(policy);
        var ruleSet = new RuleSetVersion
        {
            RuleSetVersionId = Guid.NewGuid(),
            StateCode = "IL",
            Version = "v1",
            EffectiveDate = DateTime.UtcNow.Date
        };

        var program = new ProgramDefinition
        {
            ProgramCode = "IL_BASIC",
            StateCode = "IL",
            ProgramName = "IL Basic Program",
            Category = ProgramCategory.Magi,
            IsActive = true
        };

        var rule = new EligibilityRule
        {
            EligibilityRuleId = Guid.NewGuid(),
            RuleSetVersionId = ruleSet.RuleSetVersionId,
            RuleSetVersion = ruleSet,
            ProgramCode = program.ProgramCode,
            Program = program,
            RuleLogic = "{ \"==\": [ { \"var\": \"isCitizen\" }, true ] }",
            Priority = 0,
            CreatedAt = DateTime.UtcNow
        };

        var request = new EligibilityRequest
        {
            StateCode = "IL",
            EffectiveDate = DateTime.UtcNow.Date,
            Answers = new Dictionary<string, object?>
            {
                ["isCitizen"] = true
            }
        };

        var result = evaluator.Evaluate(request, ruleSet, new List<EligibilityRule> { rule });

        result.Status.Should().Be(EligibilityStatus.Likely);
        result.MatchedPrograms.Should().HaveCount(1);
        result.MatchedPrograms[0].ProgramCode.Should().Be("IL_BASIC");
        result.ConfidenceScore.Should().Be(100);
    }

    [Fact]
    public void Evaluate_WithNoMatchingRules_ReturnsUnlikely()
    {
        var policy = new ConfidenceScoringPolicy();
        var evaluator = new EligibilityEvaluator(policy);
        var ruleSet = new RuleSetVersion
        {
            RuleSetVersionId = Guid.NewGuid(),
            StateCode = "IL",
            Version = "v1",
            EffectiveDate = DateTime.UtcNow.Date
        };

        var program = new ProgramDefinition
        {
            ProgramCode = "IL_BASIC",
            StateCode = "IL",
            ProgramName = "IL Basic Program",
            Category = ProgramCategory.Magi,
            IsActive = true
        };

        var rule = new EligibilityRule
        {
            EligibilityRuleId = Guid.NewGuid(),
            RuleSetVersionId = ruleSet.RuleSetVersionId,
            RuleSetVersion = ruleSet,
            ProgramCode = program.ProgramCode,
            Program = program,
            RuleLogic = "{ \"==\": [ { \"var\": \"isCitizen\" }, true ] }",
            Priority = 0,
            CreatedAt = DateTime.UtcNow
        };

        var request = new EligibilityRequest
        {
            StateCode = "IL",
            EffectiveDate = DateTime.UtcNow.Date,
            Answers = new Dictionary<string, object?>
            {
                ["isCitizen"] = false
            }
        };

        var result = evaluator.Evaluate(request, ruleSet, new List<EligibilityRule> { rule });

        result.Status.Should().Be(EligibilityStatus.Unlikely);
        result.MatchedPrograms.Should().BeEmpty();
        result.ConfidenceScore.Should().Be(50);
    }
}
