using FluentAssertions;
using MAA.Domain;
using MAA.Infrastructure.Data;
using MAA.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace MAA.Tests.Integration;

/// <summary>
/// Integration tests for QuestionRepository against PostgreSQL Testcontainers.
/// </summary>
[Collection("Database collection")]
public class QuestionRepositoryTests : IAsyncLifetime
{
    private readonly DatabaseFixture _databaseFixture;

    public QuestionRepositoryTests(DatabaseFixture databaseFixture)
    {
        _databaseFixture = databaseFixture;
    }

    public async Task InitializeAsync()
    {
        using var context = _databaseFixture.CreateContext();
        await context.QuestionOptions.ExecuteDeleteAsync();
        await context.Questions.ExecuteDeleteAsync();
        await context.ConditionalRules.ExecuteDeleteAsync();
    }

    public async Task DisposeAsync()
    {
        await Task.CompletedTask;
    }

    [Fact]
    public async Task GetQuestionsAsync_ReturnsOrderedQuestionsWithOptions()
    {
        // Arrange
        using var context = _databaseFixture.CreateContext();
        var repository = new QuestionRepository(context);

        var now = DateTime.UtcNow;
        var stateCode = "IL";
        var programCode = "IL-TEST";

        var questionOneId = Guid.NewGuid();
        var questionTwoId = Guid.NewGuid();

        context.Questions.AddRange(
            new Question
            {
                QuestionId = questionTwoId,
                StateCode = stateCode,
                ProgramCode = programCode,
                DisplayOrder = 2,
                QuestionText = "Second question",
                FieldType = QuestionFieldType.Text,
                IsRequired = true,
                CreatedAt = now,
                UpdatedAt = now
            },
            new Question
            {
                QuestionId = questionOneId,
                StateCode = stateCode,
                ProgramCode = programCode,
                DisplayOrder = 1,
                QuestionText = "First question",
                FieldType = QuestionFieldType.Select,
                IsRequired = false,
                CreatedAt = now,
                UpdatedAt = now,
                Options = new List<QuestionOption>
                {
                    new()
                    {
                        OptionId = Guid.NewGuid(),
                        QuestionId = questionOneId,
                        OptionLabel = "Option A",
                        OptionValue = "a",
                        DisplayOrder = 1,
                        CreatedAt = now,
                        UpdatedAt = now
                    }
                }
            }
        );

        await context.SaveChangesAsync();

        // Act
        var result = await repository.GetQuestionsAsync(stateCode, programCode);

        // Assert
        result.Should().HaveCount(2);
        result[0].DisplayOrder.Should().Be(1);
        result[0].Options.Should().HaveCount(1);
        result[1].DisplayOrder.Should().Be(2);
    }

    [Fact]
    public async Task GetConditionalRulesAsync_ReturnsOnlyRequestedRules()
    {
        // Arrange
        using var context = _databaseFixture.CreateContext();
        var repository = new QuestionRepository(context);

        var now = DateTime.UtcNow;
        var ruleA = new ConditionalRule
        {
            ConditionalRuleId = Guid.NewGuid(),
            RuleExpression = "rule-a",
            CreatedAt = now,
            UpdatedAt = now
        };

        var ruleB = new ConditionalRule
        {
            ConditionalRuleId = Guid.NewGuid(),
            RuleExpression = "rule-b",
            CreatedAt = now,
            UpdatedAt = now
        };

        context.ConditionalRules.AddRange(ruleA, ruleB);
        await context.SaveChangesAsync();

        // Act
        var result = await repository.GetConditionalRulesAsync(new[] { ruleA.ConditionalRuleId });

        // Assert
        result.Should().HaveCount(1);
        result[0].ConditionalRuleId.Should().Be(ruleA.ConditionalRuleId);
    }
}
