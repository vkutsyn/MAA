using FluentAssertions;
using MAA.Domain.Rules;
using Xunit;

namespace MAA.Tests.Unit.Rules;

/// <summary>
/// Unit tests for rule versioning and effective date handling.
/// Validates that rule versions are tracked correctly and active status is determined properly.
/// Tests the versioning properties and logic (rule entity only).
/// </summary>
[Trait("Category", "Unit")]
public class RuleVersioningTests
{
    private const string DefaultRuleLogic = "{\"if\":[{\"<=\":[{\"var\":\"monthly_income_cents\"},300000]},true,false]}";

    #region Active Rule Detection

    [Fact]
    public void RuleWithEffectiveDateInPast_IsActive()
    {
        // Rule effective yesterday should be active today
        var now = DateTime.UtcNow;
        var rule = CreateRule(
            version: 1.0m,
            effectiveDate: now.AddDays(-1),
            endDate: null
        );

        rule.IsActive.Should().BeTrue();
    }

    [Fact]
    public void RuleWithEffectiveDateInFuture_IsNotActive()
    {
        // Rule effective tomorrow should not be active yet
        var now = DateTime.UtcNow;
        var futureRule = CreateRule(
            version: 2.0m,
            effectiveDate: now.AddDays(1),
            endDate: null
        );

        futureRule.IsActive.Should().BeFalse();
    }

    [Fact]
    public void RuleWithEffectiveDateToday_IsActive()
    {
        // Rule effective today at midnight should be active
        var today = DateTime.UtcNow.Date;
        var rule = CreateRule(
            version: 1.0m,
            effectiveDate: today,
            endDate: null
        );

        rule.IsActive.Should().BeTrue();
    }

    [Fact]
    public void RuleWithEndDateInPast_IsNotActive()
    {
        // Rule that ended yesterday should be inactive
        var now = DateTime.UtcNow;
        var supersededRule = CreateRule(
            version: 1.0m,
            effectiveDate: now.AddDays(-30),
            endDate: now.AddDays(-1)
        );

        supersededRule.IsActive.Should().BeFalse();
    }

    [Fact]
    public void RuleWithEndDateInFuture_IsActive()
    {
        // Rule that ends tomorrow should be active today
        var now = DateTime.UtcNow;
        var activeRule = CreateRule(
            version: 1.0m,
            effectiveDate: now.AddDays(-30),
            endDate: now.AddDays(1)
        );

        activeRule.IsActive.Should().BeTrue();
    }

    [Fact]
    public void RuleWithEndDateToday_IsStillActive()
    {
        // Rule that ends today at start of day is still active during today
        // (becomes inactive after today ends)
        var today = DateTime.UtcNow.Date;
        var rule = CreateRule(
            version: 1.0m,
            effectiveDate: today.AddDays(-30),
            endDate: today
        );

        // Rule with end date = today is still considered active during today
        rule.IsActive.Should().BeTrue();
    }

    [Fact]
    public void RuleWithEndDateYesterday_IsNotActive()
    {
        // Rule that ended yesterday should not be active today
        var yesterday = DateTime.UtcNow.Date.AddDays(-1);
        var rule = CreateRule(
            version: 1.0m,
            effectiveDate: yesterday.AddDays(-30),
            endDate: yesterday
        );

        rule.IsActive.Should().BeFalse();
    }

    #endregion

    #region Version Field Population

    [Fact]
    public void RuleVersionFieldPopulated_VersionNumberCorrect()
    {
        var rule = CreateRule(version: 1.5m);

        rule.Version.Should().Be(1.5m);
    }

    [Theory]
    [InlineData("1.0")]
    [InlineData("1.1")]
    [InlineData("2.0")]
    [InlineData("2.5")]
    public void RuleVersionStringRepresentation_FormattedCorrectly(string versionString)
    {
        var version = Convert.ToDecimal(versionString);
        var rule = CreateRule(version: version);

        rule.Version.ToString().Should().Contain(versionString);
    }

    #endregion

    #region Multiple Rule Versions

    [Fact]
    public void MultipleRuleVersions_CorrectVersionSelected()
    {
        // Simulate having multiple versions of the same rule
        var now = DateTime.UtcNow;
        var v1 = CreateRule(
            version: 1.0m,
            effectiveDate: now.AddDays(-30),
            endDate: now.AddDays(-1),
            ruleName: "IL MAGI Adult Income Threshold v1.0"
        );
        var v2 = CreateRule(
            version: 2.0m,
            effectiveDate: now.AddDays(-1),
            endDate: null,
            ruleName: "IL MAGI Adult Income Threshold v2.0"
        );

        // v1 should be inactive (ended yesterday)
        v1.IsActive.Should().BeFalse();
        
        // v2 should be active (started yesterday, no end date)
        v2.IsActive.Should().BeTrue();
        
        // Versions should be different
        v1.Version.Should().NotBe(v2.Version);
        v1.Version.Should().Be(1.0m);
        v2.Version.Should().Be(2.0m);
    }

    [Fact]
    public void VersionTransition_BothRulesHaveCompleteMetadata()
    {
        // Simulate rule upgrade from v1.0 to v1.1
        var now = DateTime.UtcNow;
        var v1 = CreateRule(
            version: 1.0m,
            effectiveDate: now.AddDays(-30),
            endDate: now.AddDays(-1)
        );
        var v11 = CreateRule(
            version: 1.1m,
            effectiveDate: now.AddDays(-1),
            endDate: null
        );

        // Both rules should have complete metadata
        v1.RuleId.Should().NotBeEmpty();
        v11.RuleId.Should().NotBeEmpty();
        v1.ProgramId.Should().NotBeEmpty();
        v11.ProgramId.Should().NotBeEmpty();
        v1.Version.Should().Be(1.0m);
        v11.Version.Should().Be(1.1m);
        v1.EffectiveDate.Should().BeAfter(DateTime.MinValue);
        v11.EffectiveDate.Should().BeAfter(DateTime.MinValue);
        v1.StateCode.Should().NotBeNullOrEmpty();
        v11.StateCode.Should().NotBeNullOrEmpty();
    }

    #endregion

    #region Effective Date and End Date

    [Fact]
    public void RuleEffectiveDates_RecordedAccurately()
    {
        var effectiveDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var endDate = new DateTime(2026, 6, 30, 23, 59, 59, DateTimeKind.Utc);
        var rule = CreateRule(
            effectiveDate: effectiveDate,
            endDate: endDate
        );

        rule.EffectiveDate.Should().Be(effectiveDate);
        rule.EndDate.Should().Be(endDate);
    }

    [Fact]
    public void RuleWithNullEndDate_RemainsActiveIndefinitely()
    {
        var now = DateTime.UtcNow;
        var rule = CreateRule(
            effectiveDate: now.AddDays(-100),
            endDate: null
        );

        rule.EndDate.Should().BeNull();
        rule.IsActive.Should().BeTrue();
    }

    #endregion

    #region Rule Metadata

    [Fact]
    public void RuleMetadata_ContainsAllRequiredFields()
    {
        var rule = CreateRule();

        rule.RuleId.Should().NotBeEmpty();
        rule.ProgramId.Should().NotBeEmpty();
        rule.StateCode.Should().NotBeNullOrEmpty();
        rule.RuleName.Should().NotBeNullOrEmpty();
        rule.Version.Should().BeGreaterThan(0);
        rule.RuleLogic.Should().NotBeNullOrEmpty();
        rule.EffectiveDate.Should().BeAfter(DateTime.MinValue);
        rule.CreatedAt.Should().BeAfter(DateTime.MinValue);
        rule.UpdatedAt.Should().BeAfter(DateTime.MinValue);
    }

    [Fact]
    public void RuleDescription_IsOptional()
    {
        var ruleWithDescription = CreateRule();
        ruleWithDescription.Description = "Eligible if income < 200% FPL and citizen";

        var ruleWithoutDescription = CreateRule();

        ruleWithDescription.Description.Should().NotBeNullOrEmpty();
        ruleWithoutDescription.Description.Should().BeNull();
    }

    [Fact]
    public void RuleAuditTrail_TracksCreationAndUpdates()
    {
        var createdAt = DateTime.UtcNow.AddSeconds(-10);
        var createdBy = Guid.NewGuid();
        var rule = new EligibilityRule
        {
            RuleId = Guid.NewGuid(),
            ProgramId = Guid.NewGuid(),
            StateCode = "IL",
            RuleName = "Test Rule",
            Version = 1.0m,
            RuleLogic = DefaultRuleLogic,
            EffectiveDate = createdAt,
            EndDate = null,
            CreatedAt = createdAt,
            CreatedBy = createdBy,
            UpdatedAt = DateTime.UtcNow
        };

        // Audit trail should have timestamps and creator
        rule.CreatedAt.Should().Be(createdAt);
        rule.CreatedBy.Should().Be(createdBy);
        rule.UpdatedAt.Should().BeAfter(rule.CreatedAt);
    }

    #endregion

    #region Determinism

    [Fact]
    public void SameRulePropertiesMultipleTimes_AlwaysYieldSameResults()
    {
        var now = DateTime.UtcNow;
        var rule1 = CreateRule(effectiveDate: now.AddDays(-1), endDate: null);
        var rule2 = CreateRule(effectiveDate: now.AddDays(-1), endDate: null);

        rule1.IsActive.Should().Be(rule2.IsActive);
    }

    #endregion

    #region Helper Methods

    private EligibilityRule CreateRule(
        decimal version = 1.0m,
        DateTime? effectiveDate = null,
        DateTime? endDate = null,
        string ruleName = "Test Rule"
    )
    {
        return new EligibilityRule
        {
            RuleId = Guid.NewGuid(),
            ProgramId = Guid.NewGuid(),
            StateCode = "IL",
            RuleName = ruleName,
            Version = version,
            RuleLogic = DefaultRuleLogic,
            EffectiveDate = effectiveDate ?? DateTime.UtcNow.AddDays(-1),
            EndDate = endDate,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = Guid.NewGuid(),
            UpdatedAt = DateTime.UtcNow,
            Description = null
        };
    }

    #endregion
}

