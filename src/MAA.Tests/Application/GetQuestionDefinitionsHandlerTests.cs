using FluentAssertions;
using MAA.Application.DTOs;
using MAA.Application.Handlers;
using MAA.Application.Interfaces;
using MAA.Application.StateContext;
using MAA.Application.Validation;
using MAA.Domain;
using MAA.Domain.Exceptions;
using MAA.Domain.Rules;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace MAA.Tests.Application;

public class GetQuestionDefinitionsHandlerTests
{
    [Fact]
    public async Task HandleAsync_CacheHit_ReturnsCachedResponse()
    {
        // Arrange
        var stateCode = "CA";
        var programCode = "MEDI-CAL";
        var now = DateTime.UtcNow;
        var questionId = Guid.NewGuid();
        var ruleId = Guid.NewGuid();

        var cachedEntry = new QuestionDefinitionsCacheEntry(
            new List<Question>
            {
                new()
                {
                    QuestionId = questionId,
                    StateCode = stateCode,
                    ProgramCode = programCode,
                    DisplayOrder = 1,
                    QuestionText = "Do you have dependents?",
                    FieldType = QuestionFieldType.Select,
                    IsRequired = false,
                    ConditionalRuleId = ruleId,
                    CreatedAt = now,
                    UpdatedAt = now,
                    Options = new List<QuestionOption>
                    {
                        new()
                        {
                            OptionId = Guid.NewGuid(),
                            QuestionId = questionId,
                            OptionLabel = "Yes",
                            OptionValue = "yes",
                            DisplayOrder = 1,
                            CreatedAt = now,
                            UpdatedAt = now
                        }
                    }
                }
            },
            new List<ConditionalRule>
            {
                new()
                {
                    ConditionalRuleId = ruleId,
                    RuleExpression = $"{questionId} == 'yes'",
                    CreatedAt = now,
                    UpdatedAt = now
                }
            },
            now
        );

        var cache = new Mock<IQuestionDefinitionsCache>();
        cache.Setup(c => c.GetAsync(stateCode, programCode, It.IsAny<CancellationToken>()))
            .ReturnsAsync(cachedEntry);

        var repository = new Mock<IQuestionRepository>();
        var logger = new Mock<ILogger<GetQuestionDefinitionsHandler>>();

        var validator = CreateValidator(stateCode, programCode);

        var handler = new GetQuestionDefinitionsHandler(
            repository.Object,
            cache.Object,
            validator,
            new ConditionalRuleValidator(),
            logger.Object);

        // Act
        var result = await handler.HandleAsync(new GetQuestionDefinitionsQuery
        {
            StateCode = stateCode,
            ProgramCode = programCode
        });

        // Assert
        result.StateCode.Should().Be(stateCode);
        result.ProgramCode.Should().Be(programCode);
        result.Questions.Should().HaveCount(1);
        result.ConditionalRules.Should().HaveCount(1);
        repository.Verify(r => r.GetQuestionsAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_NoQuestions_ReturnsEmptyResponse()
    {
        // Arrange
        var stateCode = "IL";
        var programCode = "IL-TEST";

        var cache = new Mock<IQuestionDefinitionsCache>();
        cache.Setup(c => c.GetAsync(stateCode, programCode, It.IsAny<CancellationToken>()))
            .ReturnsAsync((QuestionDefinitionsCacheEntry?)null);

        var repository = new Mock<IQuestionRepository>();
        repository.Setup(r => r.GetQuestionsAsync(stateCode, programCode, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Question>());
        repository.Setup(r => r.GetConditionalRulesAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ConditionalRule>());

        var logger = new Mock<ILogger<GetQuestionDefinitionsHandler>>();
        var validator = CreateValidator(stateCode, programCode);

        var handler = new GetQuestionDefinitionsHandler(
            repository.Object,
            cache.Object,
            validator,
            new ConditionalRuleValidator(),
            logger.Object);

        // Act
        var result = await handler.HandleAsync(new GetQuestionDefinitionsQuery
        {
            StateCode = stateCode,
            ProgramCode = programCode
        });

        // Assert
        result.Questions.Should().BeEmpty();
        result.ConditionalRules.Should().BeEmpty();
        cache.Verify(c => c.SetAsync(stateCode, programCode, It.IsAny<QuestionDefinitionsCacheEntry>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_InvalidState_ThrowsValidationException()
    {
        // Arrange
        var cache = new Mock<IQuestionDefinitionsCache>();
        var repository = new Mock<IQuestionRepository>();
        var logger = new Mock<ILogger<GetQuestionDefinitionsHandler>>();

        var invalidValidator = CreateValidator("XX", "BAD", stateExists: false, programExists: false);
        var handler = new GetQuestionDefinitionsHandler(
            repository.Object,
            cache.Object,
            invalidValidator,
            new ConditionalRuleValidator(),
            logger.Object);

        // Act
        var action = () => handler.HandleAsync(new GetQuestionDefinitionsQuery
        {
            StateCode = "XX",
            ProgramCode = "BAD"
        });

        // Assert
        await action.Should().ThrowAsync<ValidationException>();
    }

    private static StateProgramValidator CreateValidator(
        string stateCode,
        string programCode,
        bool stateExists = true,
        bool programExists = true)
    {
        var stateConfigRepo = new Mock<IStateConfigurationRepository>();
        stateConfigRepo.Setup(r => r.ExistsAsync(It.IsAny<string>())).ReturnsAsync(stateExists);

        var programRepo = new Mock<MAA.Application.Eligibility.Repositories.IMedicaidProgramRepository>();
        if (programExists)
        {
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
        }
        else
        {
            programRepo.Setup(r => r.GetByStateAsync(It.IsAny<string>()))
                .ReturnsAsync(new List<MedicaidProgram>());
        }

        return new StateProgramValidator(stateConfigRepo.Object, programRepo.Object);
    }
}
