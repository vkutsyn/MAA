using FluentAssertions;
using MAA.Domain.Rules;
using Xunit;

namespace MAA.Tests.Unit.Rules;

/// <summary>
/// Unit tests for PathwayIdentifier - pure function pathway determination logic.
/// Tests all path combinations and edge cases for deterministic output.
/// </summary>
public class PathwayIdentifierTests
{
    private readonly PathwayIdentifier _identifier = new();

    #region Basic Pathway Determination

    [Fact]
    public void DetermineApplicablePathways_Age35NoDisabilityNotSsi_ReturnsMAGI()
    {
        var pathways = _identifier.DetermineApplicablePathways(35, false, false);
        pathways.Should().ContainSingle(EligibilityPathway.MAGI);
    }

    [Fact]
    public void DetermineApplicablePathways_Age68_ReturnsAged()
    {
        var pathways = _identifier.DetermineApplicablePathways(68, false, false);
        pathways.Should().ContainSingle(EligibilityPathway.NonMAGI_Aged);
    }

    [Fact]
    public void DetermineApplicablePathways_Age45WithDisability_ReturnsDisabled()
    {
        var pathways = _identifier.DetermineApplicablePathways(45, true, false);
        pathways.Should().ContainSingle(EligibilityPathway.NonMAGI_Disabled);
    }

    [Fact]
    public void DetermineApplicablePathways_ReceivesSsi_ReturnsSsiLinked()
    {
        var pathways = _identifier.DetermineApplicablePathways(50, false, true);
        pathways.Should().ContainSingle(EligibilityPathway.SSI_Linked);
    }

    #endregion

    #region Multiple Pathways

    [Fact]
    public void DetermineApplicablePathways_Age68WithDisability_ReturnsBothAgedAndDisabled()
    {
        var pathways = _identifier.DetermineApplicablePathways(68, true, false);
        pathways.Should().HaveCount(2);
        pathways.Should().Contain(EligibilityPathway.NonMAGI_Aged);
        pathways.Should().Contain(EligibilityPathway.NonMAGI_Disabled);
    }

    [Fact]
    public void DetermineApplicablePathways_Pregnant25YearOldFemale_ReturnsBothMAGIAndPregnancy()
    {
        var pathways = _identifier.DetermineApplicablePathways(
            age: 25,
            hasDisability: false,
            receivesSsi: false,
            isPregnant: true,
            isFemale: true
        );
        
        pathways.Should().HaveCount(2);
        pathways.Should().Contain(EligibilityPathway.MAGI);
        pathways.Should().Contain(EligibilityPathway.Pregnancy);
    }

    #endregion

    #region Edge Cases - Age Boundaries

    [Fact]
    public void DetermineApplicablePathways_Age19_ReturnsMAGI()
    {
        var pathways = _identifier.DetermineApplicablePathways(19, false, false);
        pathways.Should().ContainSingle(EligibilityPathway.MAGI);
    }

    [Fact]
    public void DetermineApplicablePathways_Age18_DoesNotReturnMAGI()
    {
        var pathways = _identifier.DetermineApplicablePathways(18, false, false);
        pathways.Should().BeEmpty();
    }

    [Fact]
    public void DetermineApplicablePathways_Age65_ReturnsAged()
    {
        var pathways = _identifier.DetermineApplicablePathways(65, false, false);
        pathways.Should().ContainSingle(EligibilityPathway.NonMAGI_Aged);
    }

    #endregion

    #region Validation

    [Fact]
    public void DetermineApplicablePathways_NegativeAge_ThrowsArgumentException()
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            _identifier.DetermineApplicablePathways(-1, false, false)
        );
        ex.Message.Should().Contain(" between 0 and 120");
    }

    [Fact]
    public void DetermineApplicablePathways_Age121_ThrowsArgumentException()
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            _identifier.DetermineApplicablePathways(121, false, false)
        );
        ex.Message.Should().Contain("between 0 and 120");
    }

    #endregion

    #region Pregnancy-Related Edge Cases

    [Fact]
    public void DetermineApplicablePathways_PregnantMale_DoesNotReturnPregnancy()
    {
        var pathways = _identifier.DetermineApplicablePathways(25, false, false, true, false);
        pathways.Should().ContainSingle(EligibilityPathway.MAGI);
        pathways.Should().NotContain(EligibilityPathway.Pregnancy);
    }

    [Fact]
    public void DetermineApplicablePathways_NonpregnantFemale_DoesNotReturnPregnancy()
    {
        var pathways = _identifier.DetermineApplicablePathways(30, false, false, false, true);
        pathways.Should().ContainSingle(EligibilityPathway.MAGI);
        pathways.Should().NotContain(EligibilityPathway.Pregnancy);
    }

    #endregion

    #region Determinism

    [Fact]
    public void DetermineApplicablePathways_SameInputGeneratesSameOutput()
    {
        var result1 = _identifier.DetermineApplicablePathways(45, true, false, false, true);
        var result2 = _identifier.DetermineApplicablePathways(45, true, false, false, true);
        result1.Should().BeEquivalentTo(result2);
    }

    [Fact]
    public void DetermineApplicablePathways_OutputIsSorted()
    {
        var pathways = _identifier.DetermineApplicablePathways(68, true, true);
        var sorted = pathways.OrderBy(p => p.ToString()).ToList();
        pathways.Should().BeEquivalentTo(sorted, options => options.WithStrictOrdering());
    }

    #endregion

    #region Convenience Methods

    [Fact]
    public void IsPathwayApplicable_MAGIFor35YearOld_ReturnsTrue()
    {
        var isApplicable = _identifier.IsPathwayApplicable(EligibilityPathway.MAGI, 35, false, false);
        isApplicable.Should().BeTrue();
    }

    [Fact]
    public void IsPathwayApplicable_PregnancyFor35YearOldMale_ReturnsFalse()
    {
        var isApplicable = _identifier.IsPathwayApplicable(
            EligibilityPathway.Pregnancy, 35, false, false, false, false);
        isApplicable.Should().BeFalse();
    }

    #endregion
}
