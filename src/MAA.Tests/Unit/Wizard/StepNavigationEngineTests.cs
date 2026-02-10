using FluentAssertions;
using MAA.Domain.Wizard;
using System.Text.Json;
using Xunit;

namespace MAA.Tests.Unit.Wizard;

/// <summary>
/// Unit tests for step navigation engine.
/// </summary>
[Trait("Category", "Unit")]
public class StepNavigationEngineTests
{
    [Fact]
    public void GetNextStep_WithHouseholdSizeOne_ReturnsIncomeStep()
    {
        var provider = new StepDefinitionProvider();
        var engine = new StepNavigationEngine(provider);

        var answers = new Dictionary<string, JsonElement>
        {
            { "household-size", CreateJsonElement("{\"householdSize\":1}") }
        };

        var nextStep = engine.GetNextStep("household-size", answers);

        nextStep.Should().NotBeNull();
        nextStep!.StepId.Should().Be("household-income");
    }

    [Fact]
    public void GetNextStep_WithHouseholdSizeGreaterThanOne_ReturnsMembersStep()
    {
        var provider = new StepDefinitionProvider();
        var engine = new StepNavigationEngine(provider);

        var answers = new Dictionary<string, JsonElement>
        {
            { "household-size", CreateJsonElement("{\"householdSize\":3}") }
        };

        var nextStep = engine.GetNextStep("household-size", answers);

        nextStep.Should().NotBeNull();
        nextStep!.StepId.Should().Be("household-members");
    }

    private static JsonElement CreateJsonElement(string json)
    {
        using var document = JsonDocument.Parse(json);
        return document.RootElement.Clone();
    }
}
