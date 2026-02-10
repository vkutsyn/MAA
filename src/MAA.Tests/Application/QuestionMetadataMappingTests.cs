using FluentAssertions;
using MAA.Application.Handlers;
using MAA.Application.Interfaces;
using MAA.Application.Validation;
using MAA.Domain;
using MAA.Domain.Rules;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace MAA.Tests.Application;

public class QuestionMetadataMappingTests
{
    [Fact]
    public async Task HandleAsync_MapsMetadataOptionsAndRules()
    {
        var stateCode = "CA";
        var programCode = "MEDI-CAL";
        var now = DateTime.UtcNow;

        var triggerQuestionId = Guid.NewGuid();
        var questionId = Guid.NewGuid();
        var ruleId = Guid.NewGuid();

        var triggerQuestion = new Question
        {
            QuestionId = triggerQuestionId,
            StateCode = stateCode,
            ProgramCode = programCode,
            DisplayOrder = 1,
            QuestionText = "Do you have dependents?",
            FieldType = QuestionFieldType.Select,
            IsRequired = true,
            CreatedAt = now,
            UpdatedAt = now,
            Options = new List<QuestionOption>
            {
                new()
                {
                    OptionId = Guid.NewGuid(),
                    QuestionId = triggerQuestionId,
                    OptionLabel = "Yes",
                    OptionValue = "yes",
                    DisplayOrder = 1,
                    CreatedAt = now,
                    UpdatedAt = now
                }
            }
        };

        var question = new Question
        {
            QuestionId = questionId,
            StateCode = stateCode,
            ProgramCode = programCode,
            DisplayOrder = 2,
            QuestionText = "Household size",
            FieldType = QuestionFieldType.Select,
            IsRequired = true,
            HelpText = "Include everyone in your home",
            ValidationRegex = "^[0-9]+$",
            ConditionalRuleId = ruleId,
            CreatedAt = now,
            UpdatedAt = now,
            Options = new List<QuestionOption>
            {
                new()
                {
                    OptionId = Guid.NewGuid(),
                    QuestionId = questionId,
                    OptionLabel = "Two",
                    OptionValue = "2",
                    DisplayOrder = 2,
                    CreatedAt = now,
                    UpdatedAt = now
                },
                new()
                {
                    OptionId = Guid.NewGuid(),
                    QuestionId = questionId,
                    OptionLabel = "One",
                    OptionValue = "1",
                    DisplayOrder = 1,
                    CreatedAt = now,
                    UpdatedAt = now
                }
            }
        };

        var rule = new ConditionalRule
        {
            ConditionalRuleId = ruleId,
            RuleExpression = $"{triggerQuestionId} == 'yes'",
            Description = "Show only if one",
            CreatedAt = now,
            UpdatedAt = now
        };

        var repository = new Mock<IQuestionRepository>();
        repository.Setup(r => r.GetQuestionsAsync(stateCode, programCode, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Question> { triggerQuestion, question });
        repository.Setup(r => r.GetConditionalRulesAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ConditionalRule> { rule });

        var cache = new Mock<IQuestionDefinitionsCache>();
        cache.Setup(c => c.GetAsync(stateCode, programCode, It.IsAny<CancellationToken>()))
            .ReturnsAsync((QuestionDefinitionsCacheEntry?)null);

        var handler = new GetQuestionDefinitionsHandler(
            repository.Object,
            cache.Object,
            CreateValidator(stateCode, programCode),
            new ConditionalRuleValidator(),
            new Mock<ILogger<GetQuestionDefinitionsHandler>>().Object);

        var result = await handler.HandleAsync(new GetQuestionDefinitionsQuery
        {
            StateCode = stateCode,
            ProgramCode = programCode
        });

        result.Questions.Should().HaveCount(2);
        var dto = result.Questions[1];
        dto.QuestionId.Should().Be(questionId);
        dto.HelpText.Should().Be("Include everyone in your home");
        dto.ValidationRegex.Should().Be("^[0-9]+$");
        dto.Options.Should().HaveCount(2);
        dto.Options![0].DisplayOrder.Should().Be(1);
        dto.Options![0].OptionLabel.Should().Be("One");
        dto.Options![1].DisplayOrder.Should().Be(2);

        result.ConditionalRules.Should().ContainSingle();
        result.ConditionalRules[0].ConditionalRuleId.Should().Be(ruleId);
        result.ConditionalRules[0].Description.Should().Be("Show only if one");
    }

    private static StateProgramValidator CreateValidator(string stateCode, string programCode)
    {
        var stateConfigRepo = new Mock<MAA.Application.StateContext.IStateConfigurationRepository>();
        stateConfigRepo.Setup(r => r.ExistsAsync(It.IsAny<string>())).ReturnsAsync(true);

        var programRepo = new Mock<MAA.Application.Eligibility.Repositories.IMedicaidProgramRepository>();
        programRepo.Setup(r => r.GetByStateAsync(It.IsAny<string>()))
            .ReturnsAsync(new List<MedicaidProgram>
            {
                new()
                {
                    ProgramId = Guid.NewGuid(),
                    StateCode = stateCode,
                    ProgramName = "Test Program",
                    ProgramCode = programCode,
                    EligibilityPathway = EligibilityPathway.MAGI,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                }
            });

        return new StateProgramValidator(stateConfigRepo.Object, programRepo.Object);
    }
}
