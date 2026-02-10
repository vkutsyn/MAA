using FluentAssertions;
using MAA.Domain.Rules;
using MAA.Domain.Rules.ValueObjects;
using Xunit;

namespace MAA.Tests.Unit.Rules;

/// <summary>
/// Unit tests for ProgramMatcher pure function
/// Tests multi-program eligibility matching and ranking
/// 
/// Phase 4 Implementation: T034
/// 
/// Test Coverage:
/// - Single program match
/// - Multiple program matches
/// - No matching programs
/// - Sorting by confidence (highest first)
/// - Different pathways (MAGI, Aged, Disabled, Pregnancy, SSI)
/// - Empty input lists
/// - Error handling
/// </summary>
[Trait("Category", "Unit")]
public class ProgramMatcherTests
{
    private readonly RuleEngine _ruleEngine = new();
    private readonly ConfidenceScorer _confidenceScorer = new();

    [Fact]
    public void FindMatchingPrograms_SingleProgramMatchesUser_ReturnsSingleMatch()
    {
        var matcher = new ProgramMatcher(_ruleEngine, _confidenceScorer);

        var user = CreateUserInput(monthlyIncomeCents: 200_000, age: 35);  // $2,000/month
        var program = CreateProgram("MAGI Adult", EligibilityPathway.MAGI);
        var rule = CreateRule(program.ProgramId, ruleLogic: "{\"<=\":[{\"var\":\"monthly_income_cents\"},300000]}");

        var results = matcher.FindMatchingPrograms(user, new[] { (program, rule) });

        results.Should().HaveCount(1);
        results[0].ProgramName.Should().Be("MAGI Adult");
        results[0].Status.Should().BeOneOf(EligibilityStatus.LikelyEligible, EligibilityStatus.PossiblyEligible);
    }

    [Fact]
    public void FindMatchingPrograms_UserQualifiesForMultiplePrograms_ReturnsAllMatches()
    {
        var matcher = new ProgramMatcher(_ruleEngine, _confidenceScorer);

        var user = CreateUserInput(
            monthlyIncomeCents: 200_000,  // $2,000/month
            age: 25,
            isPregnant: true);

        // Create multiple programs with matching rules
        var magiAdultProgram = CreateProgram("MAGI Adult", EligibilityPathway.MAGI);
        var magiRule = CreateRule(magiAdultProgram.ProgramId, ruleLogic: "{\"<=\":[{\"var\":\"monthly_income_cents\"},300000]}");

        var pregnancyProgram = CreateProgram("Pregnancy-Related", EligibilityPathway.Pregnancy);
        var pregnancyRule = CreateRule(pregnancyProgram.ProgramId, ruleLogic: "{\"var\":\"is_pregnant\"}");

        var programsWithRules = new[]
        {
            (magiAdultProgram, magiRule),
            (pregnancyProgram, pregnancyRule)
        };

        var results = matcher.FindMatchingPrograms(user, programsWithRules);

        results.Should().HaveCount(2, "User qualifies for both MAGI Adult and Pregnancy-Related");
        results.Select(r => r.ProgramName).Should().Contain(new[] { "MAGI Adult", "Pregnancy-Related" });
    }

    [Fact]
    public void FindMatchingPrograms_NoMatchingPrograms_ReturnsEmptyList()
    {
        var matcher = new ProgramMatcher(_ruleEngine, _confidenceScorer);

        var user = CreateUserInput(monthlyIncomeCents: 500_000, age: 45);  // High income
        var program = CreateProgram("MAGI Adult", EligibilityPathway.MAGI);
        var rule = CreateRule(program.ProgramId, ruleLogic: "{\"<=\":[{\"var\":\"monthly_income_cents\"},300000]}");

        var results = matcher.FindMatchingPrograms(user, new[] { (program, rule) });

        results.Should().BeEmpty("User income exceeds threshold for all programs");
    }

    [Fact]
    public void FindMatchingPrograms_ResultsSortedByConfidenceDescending()
    {
        var matcher = new ProgramMatcher(_ruleEngine, _confidenceScorer);

        var user = CreateUserInput(
            monthlyIncomeCents: 200_000,
            age: 30,
            isPregnant: true);

        // Program 1: Will have high confidence (income matches + pregnancy)
        var program1 = CreateProgram("MAGI Adult + Pregnancy", EligibilityPathway.MAGI);
        var rule1 = CreateRule(program1.ProgramId,
            ruleLogic: "{\"and\":[{\"<=\":[{\"var\":\"monthly_income_cents\"},300000]},{\"var\":\"is_pregnant\"}]}");

        // Program 2: Will have lower confidence (income only)
        var program2 = CreateProgram("MAGI Adult", EligibilityPathway.MAGI);
        var rule2 = CreateRule(program2.ProgramId, ruleLogic: "{\"<=\":[{\"var\":\"monthly_income_cents\"},250000]}");

        var programsWithRules = new[] { (program1, rule1), (program2, rule2) };
        var results = matcher.FindMatchingPrograms(user, programsWithRules);

        // Results should be sorted highest confidence first
        results.Should().BeInDescendingOrder(r => r.ConfidenceScore.Value);
    }

    [Fact]
    public void FindMatchingPrograms_AgedPathwayProgram_IncludedForEligibleAge()
    {
        var matcher = new ProgramMatcher(_ruleEngine, _confidenceScorer);

        // 70-year-old user
        var user = CreateUserInput(monthlyIncomeCents: 150_000, age: 70);

        var agedProgram = CreateProgram("Aged Medicaid", EligibilityPathway.NonMAGI_Aged);
        var agedRule = CreateRule(agedProgram.ProgramId,
            ruleLogic: "{\"and\":[{\">=\":[{\"var\":\"age\"},65]},{\"<=\":[{\"var\":\"monthly_income_cents\"},200000]}]}");

        var results = matcher.FindMatchingPrograms(user, new[] { (agedProgram, agedRule) });

        results.Should().HaveCount(1);
        results[0].EligibilityPathway.Should().Be(EligibilityPathway.NonMAGI_Aged);
    }

    [Fact]
    public void FindMatchingPrograms_CategoricalEligibility_HighConfidenceScore()
    {
        var matcher = new ProgramMatcher(_ruleEngine, _confidenceScorer);

        var user = CreateUserInput(monthlyIncomeCents: 500_000, age: 40, receivesSsi: true);

        var ssiProgram = CreateProgram("SSI-Linked Medicaid", EligibilityPathway.SSI_Linked);
        var ssiRule = CreateRule(ssiProgram.ProgramId, ruleLogic: "{\"var\":\"receives_ssi\"}");

        var results = matcher.FindMatchingPrograms(user, new[] { (ssiProgram, ssiRule) });

        results.Should().HaveCount(1);
        // Categorical eligibility should result in very high confidence
        results[0].ConfidenceScore.Value.Should().BeGreaterThanOrEqualTo(90);
    }

    [Fact]
    public void FindMatchingPrograms_UnlikelyEligibleExcluded()
    {
        var matcher = new ProgramMatcher(_ruleEngine, _confidenceScorer);

        var user = CreateUserInput(monthlyIncomeCents: 100_000, age: 30);

        // Program user will NOT match
        var program1 = CreateProgram("Income Test Program", EligibilityPathway.MAGI);
        var rule1 = CreateRule(program1.ProgramId, ruleLogic: "{\"<=\":[{\"var\":\"monthly_income_cents\"},50000]}");  // Too restrictive

        // Program user WILL match
        var program2 = CreateProgram("Standard MAGI", EligibilityPathway.MAGI);
        var rule2 = CreateRule(program2.ProgramId, ruleLogic: "{\"<=\":[{\"var\":\"monthly_income_cents\"},200000]}");

        var programsWithRules = new[] { (program1, rule1), (program2, rule2) };
        var results = matcher.FindMatchingPrograms(user, programsWithRules);

        results.Should().HaveCount(1, "Only matching programs should be included");
        results[0].ProgramName.Should().Be("Standard MAGI");
    }

    [Fact]
    public void FindMatchingPrograms_EmptyProgramList_ReturnsEmptyList()
    {
        var matcher = new ProgramMatcher(_ruleEngine, _confidenceScorer);
        var user = CreateUserInput(monthlyIncomeCents: 200_000, age: 30);

        var results = matcher.FindMatchingPrograms(user, new List<(MedicaidProgram, EligibilityRule)>());

        results.Should().BeEmpty();
    }

    [Fact]
    public void FindMatchingPrograms_NullInput_ThrowsArgumentNullException()
    {
        var matcher = new ProgramMatcher(_ruleEngine, _confidenceScorer);
        var user = CreateUserInput(monthlyIncomeCents: 200_000, age: 30);

        Action act = () => matcher.FindMatchingPrograms(null, new List<(MedicaidProgram, EligibilityRule)>());

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void FindBestMatch_MultiplePrograms_ReturnHighestConfidence()
    {
        var matcher = new ProgramMatcher(_ruleEngine, _confidenceScorer);

        var user = CreateUserInput(monthlyIncomeCents: 200_000, age: 30);

        var program1 = CreateProgram("Standard MAGI", EligibilityPathway.MAGI);
        var rule1 = CreateRule(program1.ProgramId, ruleLogic: "{\"<=\":[{\"var\":\"monthly_income_cents\"},250000]}");

        var program2 = CreateProgram("Strict MAGI", EligibilityPathway.MAGI);
        var rule2 = CreateRule(program2.ProgramId, ruleLogic: "{\"<=\":[{\"var\":\"monthly_income_cents\"},150000]}");

        var programsWithRules = new[] { (program1, rule1), (program2, rule2) };
        var best = matcher.FindBestMatch(user, programsWithRules);

        best.Should().NotBeNull();
        // The less restrictive program (1) should have higher confidence
        best!.ProgramName.Should().Be("Standard MAGI");
    }

    [Fact]
    public void FindBestMatch_NoMatches_ReturnsNull()
    {
        var matcher = new ProgramMatcher(_ruleEngine, _confidenceScorer);

        var user = CreateUserInput(monthlyIncomeCents: 500_000, age: 30);
        var program = CreateProgram("Income Test", EligibilityPathway.MAGI);
        var rule = CreateRule(program.ProgramId, ruleLogic: "{\"<=\":[{\"var\":\"monthly_income_cents\"},200000]}");

        var best = matcher.FindBestMatch(user, new[] { (program, rule) });

        best.Should().BeNull();
    }

    [Fact]
    public void FindMatchingPrograms_DifferentPathways_AllIncluded()
    {
        var matcher = new ProgramMatcher(_ruleEngine, _confidenceScorer);

        var user = CreateUserInput(
            monthlyIncomeCents: 150_000,
            age: 68,
            hasDisability: true,
            receivesSsi: false);

        var programs = new List<(MedicaidProgram, EligibilityRule)>
        {
            (CreateProgram("MAGI Adult", EligibilityPathway.MAGI),
             CreateRule(Guid.NewGuid(), "{\"<=\":[{\"var\":\"monthly_income_cents\"},200000]}")),

            (CreateProgram("Aged Medicaid", EligibilityPathway.NonMAGI_Aged),
             CreateRule(Guid.NewGuid(), "{\">=\":[{\"var\":\"age\"},65]}")),

            (CreateProgram("Disabled Medicaid", EligibilityPathway.NonMAGI_Disabled),
             CreateRule(Guid.NewGuid(), "{\"var\":\"has_disability\"}"))
        };

        var results = matcher.FindMatchingPrograms(user, programs);

        // User should qualify for all pathways
        results.Select(r => r.EligibilityPathway).Should()
            .Contain(new[] { EligibilityPathway.MAGI, EligibilityPathway.NonMAGI_Aged, EligibilityPathway.NonMAGI_Disabled });
    }

    [Fact]
    public void FindMatchingPrograms_StableSort_SameProgramNamesBothReturned()
    {
        var matcher = new ProgramMatcher(_ruleEngine, _confidenceScorer);

        var user = CreateUserInput(monthlyIncomeCents: 200_000, age: 30);

        // Create two programs with identical names but different IDs (shouldn't happen in real system)
        var program1 = CreateProgram("Standard MAGI", EligibilityPathway.MAGI);
        var rule1 = CreateRule(program1.ProgramId, ruleLogic: "{\"<=\":[{\"var\":\"monthly_income_cents\"},250000]}");

        var program2 = CreateProgram("Standard MAGI", EligibilityPathway.MAGI);
        var rule2 = CreateRule(program2.ProgramId, ruleLogic: "{\"<=\":[{\"var\":\"monthly_income_cents\"},250000]}");

        var programsWithRules = new[] { (program1, rule1), (program2, rule2) };
        var results = matcher.FindMatchingPrograms(user, programsWithRules);

        results.Should().HaveCount(2, "Both programs should match with same confidence");
    }

    // Helper methods for creating test data

    private static UserEligibilityInput CreateUserInput(
        long monthlyIncomeCents = 200_000,
        int age = 35,
        int householdSize = 2,
        bool hasDisability = false,
        bool isPregnant = false,
        bool receivesSsi = false,
        bool isCitizen = true,
        long? assetsCents = null)
    {
        return new UserEligibilityInput
        {
            StateCode = "IL",
            HouseholdSize = householdSize,
            MonthlyIncomeCents = monthlyIncomeCents,
            Age = age,
            HasDisability = hasDisability,
            IsPregnant = isPregnant,
            ReceivesSsi = receivesSsi,
            IsCitizen = isCitizen,
            AssetsCents = assetsCents,
            CurrentDate = DateTime.UtcNow
        };
    }

    private static MedicaidProgram CreateProgram(
        string name,
        EligibilityPathway pathway,
        string state = "IL")
    {
        return new MedicaidProgram
        {
            ProgramId = Guid.NewGuid(),
            StateCode = state,
            ProgramName = name,
            ProgramCode = name.Replace(" ", "_").ToUpperInvariant(),
            EligibilityPathway = pathway,
            Description = $"Test program: {name}",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    private static EligibilityRule CreateRule(
        Guid programId,
        string ruleLogic = "{\"<=\":[{\"var\":\"monthly_income_cents\"},300000]}")
    {
        return new EligibilityRule
        {
            RuleId = Guid.NewGuid(),
            ProgramId = programId,
            StateCode = "IL",
            RuleName = "Test Rule",
            Version = 1.0m,
            RuleLogic = ruleLogic,
            EffectiveDate = DateTime.UtcNow.AddMonths(-1),
            EndDate = null,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }
}
