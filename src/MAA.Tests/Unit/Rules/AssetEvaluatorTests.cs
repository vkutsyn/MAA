using FluentAssertions;
using MAA.Domain.Rules;
using Xunit;

namespace MAA.Tests.Unit.Rules;

/// <summary>
/// Unit tests for AssetEvaluator pure function
/// Tests asset eligibility determination for non-MAGI pathways
/// 
/// Phase 4 Implementation: T034a
/// 
/// Test Coverage:
/// - Asset limits by state (IL, CA, NY, TX, FL)
/// - Non-MAGI pathways (Aged, Disabled)
/// - MAGI pathway (no asset test)
/// - SSI pathway (categorical eligibility)
/// - Pregnancy pathway (no asset test)
/// - Edge cases (zero assets, exact limits, overage)
/// - Error handling (invalid state codes)
/// </summary>
[Trait("Category", "Unit")]
public class AssetEvaluatorTests
{
    /// <summary>
    /// Test: Assets below state limit for Aged pathway should be eligible
    /// </summary>
    [Fact]
    public void EvaluateAssets_AgedPathwayBelowLimit_ReturnsEligibleTrue()
    {
        // IL has $2,000 limit; test with $1,500
        var assetsInCents = 150_000L;  // $1,500.00
        var pathway = EligibilityPathway.NonMAGI_Aged;
        var state = "IL";

        var (isEligible, reason) = AssetEvaluator.EvaluateAssets(assetsInCents, pathway, state, 2026);

        isEligible.Should().Be(true);
        reason.Should().Contain("within the");
        reason.Should().Contain("IL");
    }

    /// <summary>
    /// Test: Assets at exact state limit for Aged pathway should be eligible
    /// </summary>
    [Fact]
    public void EvaluateAssets_AgedPathwayAtLimit_ReturnsEligibleTrue()
    {
        // IL has $2,000 limit; test with exactly $2,000
        var assetsInCents = 200_000L;  // $2,000.00
        var pathway = EligibilityPathway.NonMAGI_Aged;
        var state = "IL";

        var (isEligible, reason) = AssetEvaluator.EvaluateAssets(assetsInCents, pathway, state, 2026);

        isEligible.Should().Be(true);
        reason.Should().Contain("within the");
    }

    /// <summary>
    /// Test: Assets above state limit for Aged pathway should be ineligible with reason
    /// </summary>
    [Fact]
    public void EvaluateAssets_AgedPathwayAboveLimit_ReturnsEligibleFalseWithReason()
    {
        // IL has $2,000 limit; test with $2,500
        var assetsInCents = 250_000L;  // $2,500.00
        var pathway = EligibilityPathway.NonMAGI_Aged;
        var state = "IL";

        var (isEligible, reason) = AssetEvaluator.EvaluateAssets(assetsInCents, pathway, state, 2026);

        isEligible.Should().Be(false);
        reason.Should().Contain("exceed");
        reason.Should().Contain("$2000");  // Exact limit message
        reason.Should().Contain("overage");  // Should mention overage amount
    }

    /// <summary>
    /// Test: Different states have different asset limits
    /// IL: $2,000 vs CA: $3,000 - same user has different results
    /// </summary>
    [Theory]
    [InlineData("IL", 200_000L, true)]      // $2,000 in IL is at limit (eligible)
    [InlineData("IL", 200_001L, false)]     // $2,000.01 in IL exceeds limit (ineligible)
    [InlineData("CA", 300_000L, true)]      // $3,000 in CA is at limit (eligible)
    [InlineData("CA", 300_001L, false)]     // $3,000.01 in CA exceeds limit (ineligible)
    public void EvaluateAssets_DifferentStatesHaveDifferentLimits_ResultsReflectStateLimits(
        string state, long assetsInCents, bool expectedEligible)
    {
        var pathway = EligibilityPathway.NonMAGI_Aged;

        var (isEligible, reason) = AssetEvaluator.EvaluateAssets(assetsInCents, pathway, state, 2026);

        isEligible.Should().Be(expectedEligible);
        reason.Should().Contain(state);
    }

    /// <summary>
    /// Test: Disabled pathway uses same asset limits as Aged pathway (per spec)
    /// </summary>
    [Fact]
    public void EvaluateAssets_DisabledPathwayUsesAppropriateLimit_BelowLimitReturnsEligible()
    {
        // TX has $2,000 limit for both Aged and Disabled
        var assetsInCents = 150_000L;  // $1,500.00
        var pathway = EligibilityPathway.NonMAGI_Disabled;
        var state = "TX";

        var (isEligible, reason) = AssetEvaluator.EvaluateAssets(assetsInCents, pathway, state, 2026);

        isEligible.Should().Be(true);
        reason.Should().Contain("Disabled");  // Should reference Disabled pathway
    }

    /// <summary>
    /// Test: MAGI pathway has no asset test (always eligible)
    /// </summary>
    [Fact]
    public void EvaluateAssets_MAGIPathway_AlwaysReturnsEligibleRegardlessOfAssets()
    {
        // MAGI has no asset test - test with high assets
        var assetsInCents = 10_000_000L;  // $100,000 - would fail Aged/Disabled test
        var pathway = EligibilityPathway.MAGI;
        var state = "IL";

        var (isEligible, reason) = AssetEvaluator.EvaluateAssets(assetsInCents, pathway, state, 2026);

        isEligible.Should().Be(true);
        reason.Should().Contain("MAGI");
        reason.Should().Contain("does not include");
    }

    /// <summary>
    /// Test: SSI pathway has no asset test (categorical eligibility)
    /// </summary>
    [Fact]
    public void EvaluateAssets_SSIPathway_AlwaysReturnsEligible()
    {
        var assetsInCents = 10_000_000L;  // High assets
        var pathway = EligibilityPathway.SSI_Linked;
        var state = "CA";

        var (isEligible, reason) = AssetEvaluator.EvaluateAssets(assetsInCents, pathway, state, 2026);

        isEligible.Should().Be(true);
        reason.Should().Contain("categorically eligible");
    }

    /// <summary>
    /// Test: Pregnancy pathway has no asset test
    /// </summary>
    [Fact]
    public void EvaluateAssets_PregnancyPathway_AlwaysReturnsEligible()
    {
        var assetsInCents = 5_000_000L;
        var pathway = EligibilityPathway.Pregnancy;
        var state = "NY";

        var (isEligible, reason) = AssetEvaluator.EvaluateAssets(assetsInCents, pathway, state, 2026);

        isEligible.Should().Be(true);
        reason.Should().Contain("Pregnancy");
    }

    /// <summary>
    /// Test: Null/zero assets should be treated as $0
    /// </summary>
    [Fact]
    public void EvaluateAssets_ZeroAssets_ReturnsEligibleForAgedPathway()
    {
        var assetsInCents = 0L;
        var pathway = EligibilityPathway.NonMAGI_Aged;
        var state = "FL";

        var (isEligible, reason) = AssetEvaluator.EvaluateAssets(assetsInCents, pathway, state, 2026);

        isEligible.Should().Be(true);
        reason.Should().Contain("within the");
    }

    /// <summary>
    /// Test: Invalid state code should return ineligible with error message
    /// </summary>
    [Fact]
    public void EvaluateAssets_InvalidStateCode_ReturnsIneligibleWithErrorMessage()
    {
        var assetsInCents = 100_000L;
        var pathway = EligibilityPathway.NonMAGI_Aged;
        var invalidState = "ZZ";

        var (isEligible, reason) = AssetEvaluator.EvaluateAssets(assetsInCents, pathway, invalidState, 2026);

        isEligible.Should().Be(false);
        reason.Should().Contain("Unknown state");
    }

    /// <summary>
    /// Test: All state limits are correctly defined
    /// Verify the asset limits match spec: IL $2k, CA $3k, NY $4.5k, TX $2k, FL $2.5k
    /// </summary>
    [Theory]
    [InlineData("IL", 200_000L)]   // $2,000
    [InlineData("CA", 300_000L)]   // $3,000
    [InlineData("NY", 450_000L)]   // $4,500
    [InlineData("TX", 200_000L)]   // $2,000
    [InlineData("FL", 250_000L)]   // $2,500
    public void EvaluateAssets_AllStatesHaveCorrectSpecificLimits_AtLimitReturnsEligible(
        string state, long expectedLimitCents)
    {
        var pathway = EligibilityPathway.NonMAGI_Aged;

        // At the limit should be eligible
        var (shouldBeEligible, _) = AssetEvaluator.EvaluateAssets(
            expectedLimitCents, pathway, state, 2026);

        // One cent over should be ineligible
        var (shouldBeIneligible, _) = AssetEvaluator.EvaluateAssets(
            expectedLimitCents + 1, pathway, state, 2026);

        shouldBeEligible.Should().Be(true, $"Assets at limit should be eligible for {state}");
        shouldBeIneligible.Should().Be(false, $"Assets over limit should be ineligible for {state}");
    }

    /// <summary>
    /// Test: GetAssetLimitCents helper method returns correct limits
    /// </summary>
    [Theory]
    [InlineData("IL", EligibilityPathway.NonMAGI_Aged, 200_000L)]
    [InlineData("CA", EligibilityPathway.NonMAGI_Disabled, 300_000L)]
    [InlineData("NY", EligibilityPathway.NonMAGI_Aged, 450_000L)]
    [InlineData("TX", EligibilityPathway.NonMAGI_Disabled, 200_000L)]
    [InlineData("FL", EligibilityPathway.NonMAGI_Aged, 250_000L)]
    public void GetAssetLimitCents_NonMAGIPathways_ReturnsCorrectStateLimit(
        string state, EligibilityPathway pathway, long expectedLimitCents)
    {
        var limit = AssetEvaluator.GetAssetLimitCents(state, pathway);

        limit.Should().Be(expectedLimitCents);
    }

    /// <summary>
    /// Test: GetAssetLimitCents returns null for pathways without asset tests
    /// </summary>
    [Theory]
    [InlineData(EligibilityPathway.MAGI)]
    [InlineData(EligibilityPathway.SSI_Linked)]
    [InlineData(EligibilityPathway.Pregnancy)]
    public void GetAssetLimitCents_PathwaysWithoutAssetTest_ReturnsNull(
        EligibilityPathway pathway)
    {
        var limit = AssetEvaluator.GetAssetLimitCents("IL", pathway);

        limit.Should().BeNull("These pathways do not have asset tests");
    }

    /// <summary>
    /// Test: Reason explanations include concrete dollar amounts for user clarity
    /// </summary>
    [Fact]
    public void EvaluateAssets_ReasonExplanations_IncludeConcreteValues()
    {
        var assetsInCents = 280_000L;  // $2,800
        var pathway = EligibilityPathway.NonMAGI_Aged;
        var state = "IL";  // Has $2,000 limit

        var (isEligible, reason) = AssetEvaluator.EvaluateAssets(assetsInCents, pathway, state, 2026);

        // Should mention actual user's asset amount
        reason.Should().Contain("$2800");
        // Should mention state limit
        reason.Should().Contain("$2000");
    }
}
