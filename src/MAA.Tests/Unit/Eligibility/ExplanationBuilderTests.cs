using FluentAssertions;
using MAA.Domain.Eligibility;
using Xunit;

namespace MAA.Tests.Unit.Eligibility;

public class ExplanationBuilderTests
{
    [Fact]
    public void BuildExplanation_WithMetCriteria_IncludesMetInExplanation()
    {
        // Arrange
        var builder = new ExplanationBuilder();
        var metCriteria = new List<string> { "citizenship_requirement", "income_threshold" };
        var unmetCriteria = new List<string>();
        var missingCriteria = new List<string>();

        // Act
        var explanation = builder.GenerateExplanation(metCriteria, unmetCriteria, missingCriteria);

        // Assert
        explanation.Should().NotBeNullOrEmpty();
        explanation.ToLower().Should().Contain("meet");
    }

    [Fact]
    public void BuildExplanation_WithUnmetCriteria_IncludesUnmetInExplanation()
    {
        // Arrange
        var builder = new ExplanationBuilder();
        var metCriteria = new List<string>();
        var unmetCriteria = new List<string> { "income_threshold" };
        var missingCriteria = new List<string>();

        // Act
        var explanation = builder.GenerateExplanation(metCriteria, unmetCriteria, missingCriteria);

        // Assert
        explanation.Should().NotBeNullOrEmpty();
        explanation.ToLower().Should().Contain("do not meet");
    }

    [Fact]
    public void BuildExplanation_WithMissingCriteria_IncludesMissingInExplanation()
    {
        // Arrange
        var builder = new ExplanationBuilder();
        var metCriteria = new List<string>();
        var unmetCriteria = new List<string>();
        var missingCriteria = new List<string> { "employment_status" };

        // Act
        var explanation = builder.GenerateExplanation(metCriteria, unmetCriteria, missingCriteria);

        // Assert
        explanation.Should().NotBeNullOrEmpty();
        explanation.ToLower().Should().Contain("missing");
    }

    [Fact]
    public void BuildExplanationItems_ReturnsDeterministicOutput()
    {
        // Arrange
        var builder = new ExplanationBuilder();
        var metCriteria = new List<string> { "citizenship_requirement" };
        var unmetCriteria = new List<string> { "asset_limit" };
        var missingCriteria = new List<string> { "employment_status" };

        // Act
        var items1 = builder.BuildExplanationItems(metCriteria, unmetCriteria, missingCriteria);
        var items2 = builder.BuildExplanationItems(metCriteria, unmetCriteria, missingCriteria);

        // Assert
        items1.Should().HaveCount(items2.Count);
        for (int i = 0; i < items1.Count; i++)
        {
            items1[i].Message.Should().Be(items2[i].Message);
            items1[i].Status.Should().Be(items2[i].Status);
        }
    }

    [Fact]
    public void BuildExplanationItems_WithMetCriteria_ReturnsMetStatus()
    {
        // Arrange
        var builder = new ExplanationBuilder();
        var metCriteria = new List<string> { "citizenship_requirement" };
        var unmetCriteria = new List<string>();
        var missingCriteria = new List<string>();

        // Act
        var items = builder.BuildExplanationItems(metCriteria, unmetCriteria, missingCriteria);

        // Assert
        items.Should().NotBeEmpty();
        items.Should().Contain(item => item.Status == ExplanationItemStatus.Met);
    }

    [Fact]
    public void BuildExplanationItems_WithUnmetCriteria_ReturnsUnmetStatus()
    {
        // Arrange
        var builder = new ExplanationBuilder();
        var metCriteria = new List<string>();
        var unmetCriteria = new List<string> { "income_threshold" };
        var missingCriteria = new List<string>();

        // Act
        var items = builder.BuildExplanationItems(metCriteria, unmetCriteria, missingCriteria);

        // Assert
        items.Should().NotBeEmpty();
        items.Should().Contain(item => item.Status == ExplanationItemStatus.Unmet);
    }

    [Fact]
    public void BuildExplanationItems_WithMissingCriteria_ReturnsMissingStatus()
    {
        // Arrange
        var builder = new ExplanationBuilder();
        var metCriteria = new List<string>();
        var unmetCriteria = new List<string>();
        var missingCriteria = new List<string> { "employment_status" };

        // Act
        var items = builder.BuildExplanationItems(metCriteria, unmetCriteria, missingCriteria);

        // Assert
        items.Should().NotBeEmpty();
        items.Should().Contain(item => item.Status == ExplanationItemStatus.Missing);
    }

    [Fact]
    public void GenerateExplanation_UsesPlainLanguage_NoTechnicalJargon()
    {
        // Arrange
        var builder = new ExplanationBuilder();
        var metCriteria = new List<string> { "citizenship_requirement", "income_threshold" };
        var unmetCriteria = new List<string>();
        var missingCriteria = new List<string>();

        // Act
        var explanation = builder.GenerateExplanation(metCriteria, unmetCriteria, missingCriteria);

        // Assert
        explanation.Should().NotContain("regex");
        explanation.Should().NotContain("algorithm");
        explanation.Should().NotContain("JSONLogic");
    }

    [Fact]
    public void BuildExplanationItems_WithMixedCriteria_ReturnsMixedStatuses()
    {
        // Arrange
        var builder = new ExplanationBuilder();
        var metCriteria = new List<string> { "citizenship_requirement" };
        var unmetCriteria = new List<string> { "income_threshold" };
        var missingCriteria = new List<string> { "employment_status" };

        // Act
        var items = builder.BuildExplanationItems(metCriteria, unmetCriteria, missingCriteria);

        // Assert
        items.Should().Contain(item => item.Status == ExplanationItemStatus.Met);
        items.Should().Contain(item => item.Status == ExplanationItemStatus.Unmet);
        items.Should().Contain(item => item.Status == ExplanationItemStatus.Missing);
    }

    [Fact]
    public void GenerateExplanation_WithAllMetCriteria_ShouldIndicateApproval()
    {
        // Arrange
        var builder = new ExplanationBuilder();
        var metCriteria = new List<string> { "citizenship_requirement", "income_threshold", "residency_requirement" };
        var unmetCriteria = new List<string>();
        var missingCriteria = new List<string>();

        // Act
        var explanation = builder.GenerateExplanation(metCriteria, unmetCriteria, missingCriteria);

        // Assert
        explanation.Should().NotBeNullOrEmpty();
        explanation.ToLower().Should().Contain("eligible");
    }

    [Fact]
    public void GenerateExplanation_WithAllUnmetCriteria_ShouldIndicateDenial()
    {
        // Arrange
        var builder = new ExplanationBuilder();
        var metCriteria = new List<string>();
        var unmetCriteria = new List<string> { "citizenship_requirement", "income_threshold" };
        var missingCriteria = new List<string>();

        // Act
        var explanation = builder.GenerateExplanation(metCriteria, unmetCriteria, missingCriteria);

        // Assert
        explanation.Should().NotBeNullOrEmpty();
        explanation.ToLower().Should().Contain("do not appear");
    }

    [Fact]
    public void BuildExplanationItems_EmptyLists_ReturnsEmptyList()
    {
        // Arrange
        var builder = new ExplanationBuilder();
        var metCriteria = new List<string>();
        var unmetCriteria = new List<string>();
        var missingCriteria = new List<string>();

        // Act
        var items = builder.BuildExplanationItems(metCriteria, unmetCriteria, missingCriteria);

        // Assert
        items.Should().BeEmpty();
    }
}
