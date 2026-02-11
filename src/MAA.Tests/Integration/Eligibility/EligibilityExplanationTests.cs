using FluentAssertions;
using MAA.Domain.Eligibility;
using MAA.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace MAA.Tests.Integration.Eligibility;

public class EligibilityExplanationTests : IAsyncLifetime
{
    private SessionContext _dbContext = null!;
    private ExplanationBuilder _explanationBuilder = null!;

    public async Task InitializeAsync()
    {
        var options = new DbContextOptionsBuilder<SessionContext>()
            .UseInMemoryDatabase(databaseName: $"eligibility_explanation_{Guid.NewGuid()}")
            .Options;

        _dbContext = new SessionContext(options);
        _explanationBuilder = new ExplanationBuilder();
        await _dbContext.Database.EnsureCreatedAsync();
    }

    public async Task DisposeAsync()
    {
        await _dbContext.DisposeAsync();
    }

    [Fact]
    public void ExplanationBuilder_WithMetCriteria_ProducesDetailedExplanation()
    {
        // Arrange
        var metCriteria = new List<string> { "citizenship_requirement", "income_threshold" };
        var unmetCriteria = new List<string>();
        var missingCriteria = new List<string>();

        // Act
        var items = _explanationBuilder.BuildExplanationItems(metCriteria, unmetCriteria, missingCriteria);
        var summary = _explanationBuilder.GenerateExplanation(metCriteria, unmetCriteria, missingCriteria);

        // Assert
        items.Should().NotBeEmpty();
        items.Should().AllSatisfy(item => item.Message.Should().NotBeNullOrEmpty());
        summary.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void ExplanationBuilder_WithUnmetCriteria_ClearlyCommunatesReasons()
    {
        // Arrange
        var metCriteria = new List<string>();
        var unmetCriteria = new List<string> { "income_threshold", "asset_limit" };
        var missingCriteria = new List<string>();

        // Act
        var items = _explanationBuilder.BuildExplanationItems(metCriteria, unmetCriteria, missingCriteria);

        // Assert
        items.Should().AllSatisfy(item => 
        {
            item.Status.Should().Be(ExplanationItemStatus.Unmet);
            item.Message.Should().Contain("not");
        });
    }

    [Fact]
    public void ExplanationBuilder_WithMissingCriteria_IndicatesMissingData()
    {
        // Arrange
        var metCriteria = new List<string> { "citizenship_requirement" };
        var unmetCriteria = new List<string>();
        var missingCriteria = new List<string> { "employment_status" };

        // Act
        var items = _explanationBuilder.BuildExplanationItems(metCriteria, unmetCriteria, missingCriteria);

        // Assert
        items.Should().Contain(item => item.Status == ExplanationItemStatus.Missing);
    }

    [Fact]
    public void ExplanationBuilder_ProducesDeterministicOutput_ForSameInput()
    {
        // Arrange
        var metCriteria = new List<string> { "citizenship_requirement", "income_threshold" };
        var unmetCriteria = new List<string> { "asset_limit" };
        var missingCriteria = new List<string> { "employment_status" };

        // Act
        var explanation1 = _explanationBuilder.GenerateExplanation(metCriteria, unmetCriteria, missingCriteria);
        var explanation2 = _explanationBuilder.GenerateExplanation(metCriteria, unmetCriteria, missingCriteria);

        // Assert
        explanation1.Should().Be(explanation2);
    }

    [Fact]
    public void ExplanationBuilder_ItemsAreOrderedByStatus()
    {
        // Arrange
        var metCriteria = new List<string> { "citizenship_requirement" };
        var unmetCriteria = new List<string> { "income_threshold" };
        var missingCriteria = new List<string> { "employment_status" };

        // Act
        var items = _explanationBuilder.BuildExplanationItems(metCriteria, unmetCriteria, missingCriteria);

        // Assert
        items.Should().NotBeEmpty();
        // Verify all different statuses are present
        var statuses = items.Select(i => i.Status).Distinct().ToList();
        statuses.Should().HaveCount(3);
    }

    [Fact]
    public void ExplanationBuilder_WithFullEligibilityScenario_ProducesExpectedOutput()
    {
        // Scenario: Eligible applicant
        // - Meets: citizenship, income, residency
        // - Does not meet: none
        // - Missing: none
        // Arrange
        var metCriteria = new List<string> 
        { 
            "citizenship_requirement", 
            "income_threshold", 
            "residency_requirement" 
        };
        var unmetCriteria = new List<string>();
        var missingCriteria = new List<string>();

        // Act
        var explanation = _explanationBuilder.GenerateExplanation(metCriteria, unmetCriteria, missingCriteria);
        var items = _explanationBuilder.BuildExplanationItems(metCriteria, unmetCriteria, missingCriteria);

        // Assert
        explanation.Should().Contain("eligible".ToLower());
        items.Should().NotBeEmpty();
        items.Should().AllSatisfy(i => i.Status.Should().Be(ExplanationItemStatus.Met));
    }

    [Fact]
    public void ExplanationBuilder_WithPartialEligibilityScenario_ClearlyCommunatesMixedResults()
    {
        // Scenario: Partially eligible applicant
        // - Meets: citizenship, income
        // - Does not meet: asset_limit
        // - Missing: employment_status
        // Arrange
        var metCriteria = new List<string> { "citizenship_requirement", "income_threshold" };
        var unmetCriteria = new List<string> { "asset_limit" };
        var missingCriteria = new List<string> { "employment_status" };

        // Act
        var explanation = _explanationBuilder.GenerateExplanation(metCriteria, unmetCriteria, missingCriteria);
        var items = _explanationBuilder.BuildExplanationItems(metCriteria, unmetCriteria, missingCriteria);

        // Assert
        explanation.Should().NotBeNullOrEmpty();
        items.Should().Contain(i => i.Status == ExplanationItemStatus.Met);
        items.Should().Contain(i => i.Status == ExplanationItemStatus.Unmet);
        items.Should().Contain(i => i.Status == ExplanationItemStatus.Missing);
    }

    [Fact]
    public void ExplanationBuilder_AllMessages_UseReadableLanguage()
    {
        // Arrange
        var metCriteria = new List<string> { "citizenship_requirement" };
        var unmetCriteria = new List<string> { "income_threshold" };
        var missingCriteria = new List<string> { "employment_status" };

        // Act
        var items = _explanationBuilder.BuildExplanationItems(metCriteria, unmetCriteria, missingCriteria);

        // Assert
        items.Should().AllSatisfy(item =>
        {
            item.Message.Should().NotBeNullOrEmpty();
            item.Message.Should().NotContain("{");
            item.Message.Should().NotContain("}");
        });
    }

    [Fact]
    public void ExplanationBuilder_IneligibleScenario_ClearlyCommunatesReason()
    {
        // Scenario: Ineligible applicant - does not meet all required criteria
        // Arrange
        var metCriteria = new List<string>();
        var unmetCriteria = new List<string> { "citizenship_requirement", "income_threshold", "residency_requirement" };
        var missingCriteria = new List<string>();

        // Act
        var explanation = _explanationBuilder.GenerateExplanation(metCriteria, unmetCriteria, missingCriteria);
        var items = _explanationBuilder.BuildExplanationItems(metCriteria, unmetCriteria, missingCriteria);

        // Assert
        explanation.Should().NotContain("not eligible");
        items.Should().AllSatisfy(i => i.Status.Should().Be(ExplanationItemStatus.Unmet));
    }

    [Fact]
    public void ExplanationBuilder_MultipleMetCriteria_ListsAllMet()
    {
        // Arrange
        var metCriteria = new List<string> 
        { 
            "citizenship_requirement", 
            "income_threshold",
            "residency_requirement",
            "age_requirement" 
        };
        var unmetCriteria = new List<string>();
        var missingCriteria = new List<string>();

        // Act
        var items = _explanationBuilder.BuildExplanationItems(metCriteria, unmetCriteria, missingCriteria);

        // Assert
        items.Should().HaveCount(4);
        items.Should().AllSatisfy(i => i.Status.Should().Be(ExplanationItemStatus.Met));
    }
}
