using FluentAssertions;
using MAA.Domain.Wizard;
using Xunit;

namespace MAA.Tests.Unit.Wizard;

/// <summary>
/// Unit tests for downstream invalidation rules.
/// </summary>
[Trait("Category", "Unit")]
public class StepInvalidationTests
{
    [Fact]
    public void GetDownstreamStepIds_FromFirstStep_ReturnsLaterSteps()
    {
        var provider = new StepDefinitionProvider();
        var service = new StepInvalidationService(provider);

        var downstream = service.GetDownstreamStepIds("household-size");

        downstream.Should().Contain(new[] { "household-members", "household-income" });
    }

    [Fact]
    public void GetDownstreamStepIds_FromFinalStep_ReturnsEmpty()
    {
        var provider = new StepDefinitionProvider();
        var service = new StepInvalidationService(provider);

        var downstream = service.GetDownstreamStepIds("household-income");

        downstream.Should().BeEmpty();
    }
}
