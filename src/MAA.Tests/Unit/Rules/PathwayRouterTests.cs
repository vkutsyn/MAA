using FluentAssertions;
using MAA.Domain.Rules;
using Xunit;

namespace MAA.Tests.Unit.Rules;

/// <summary>
/// Unit tests for PathwayRouter - program filtering by eligibility pathway.
/// </summary>
public class PathwayRouterTests
{
    private readonly PathwayRouter _router = new();

    private List<MedicaidProgram> CreateSamplePrograms()
    {
        return new List<MedicaidProgram>
        {
            new MedicaidProgram { ProgramId = 1, ProgramName = "MAGI Adult", ProgramCode = "MAGI_ADULT", State = "IL", EligibilityPathway = EligibilityPathway.MAGI },
            new MedicaidProgram { ProgramId = 2, ProgramName = "Aged Medicaid", ProgramCode = "AGED", State = "IL", EligibilityPathway = EligibilityPathway.NonMAGI_Aged },
            new MedicaidProgram { ProgramId = 3, ProgramName = "Disabled Medicaid", ProgramCode = "DISABLED", State = "IL", EligibilityPathway = EligibilityPathway.NonMAGI_Disabled },
            new MedicaidProgram { ProgramId = 4, ProgramName = "Pregnancy-Related", ProgramCode = "PREG", State = "IL", EligibilityPathway = EligibilityPathway.Pregnancy }
        };
    }

    [Fact]
    public void RouteToProgramsForPathways_SinglePathway_ReturnsMatching()
    {
        var programs = CreateSamplePrograms();
        var pathways = new List<EligibilityPathway> { EligibilityPathway.MAGI };
        
        var routed = _router.RouteToProgramsForPathways(pathways, programs);

        routed.Should().ContainSingle();
        routed[0].ProgramCode.Should().Be("MAGI_ADULT");
    }

    [Fact]
    public void RouteToProgramsForPathways_MultiplePathways_ReturnsAllMatching()
    {
        var programs = CreateSamplePrograms();
        var pathways = new List<EligibilityPathway> 
        { 
            EligibilityPathway.NonMAGI_Aged,
            EligibilityPathway.NonMAGI_Disabled 
        };
        
        var routed = _router.RouteToProgramsForPathways(pathways, programs);

        routed.Should().HaveCount(2);
        routed.Should().Contain(p => p.ProgramCode == "AGED");
        routed.Should().Contain(p => p.ProgramCode == "DISABLED");
    }

    [Fact]
    public void RouteToProgramsForPathways_NoMatchingPathways_ReturnsEmpty()
    {
        var programs = CreateSamplePrograms();
        var pathways = new List<EligibilityPathway>();
        
        var routed = _router.RouteToProgramsForPathways(pathways, programs);
        routed.Should().BeEmpty();
    }

    [Fact]
    public void RouteToProgramsForPathways_NullPathways_ThrowsArgumentNullException()
    {
        var programs = CreateSamplePrograms();
        Assert.Throws<ArgumentNullException>(() =>
            _router.RouteToProgramsForPathways(null!, programs)
        );
    }

    [Fact]
    public void CountAvailableProgramsForPathways_ReturnsCorrectCount()
    {
        var programs = CreateSamplePrograms();
        var pathways = new List<EligibilityPathway> { EligibilityPathway.MAGI };
        
        var count = _router.CountAvailableProgramsForPathways(pathways, programs);
        count.Should().Be(1);
    }

    [Fact]
    public void HasAvailableProgramsForPathways_WithMatching_ReturnsTrue()
    {
        var programs = CreateSamplePrograms();
        var pathways = new List<EligibilityPathway> { EligibilityPathway.Pregnancy };
        
        var hasPrograms = _router.HasAvailableProgramsForPathways(pathways, programs);
        hasPrograms.Should().BeTrue();
    }

    [Fact]
    public void HasAvailableProgramsForPathways_NoMatching_ReturnsFalse()
    {
        var programs = new List<MedicaidProgram>();
        var pathways = new List<EligibilityPathway> { EligibilityPathway.MAGI };
        
        var hasPrograms = _router.HasAvailableProgramsForPathways(pathways, programs);
        hasPrograms.Should().BeFalse();
    }
}
