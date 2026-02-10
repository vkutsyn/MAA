using FluentAssertions;
using MAA.Domain.Wizard;
using Xunit;

namespace MAA.Tests.Unit.Wizard;

/// <summary>
/// Unit tests for StepAnswer validation.
/// </summary>
[Trait("Category", "Unit")]
public class StepAnswerValidatorTests
{
    [Fact]
    public void Validate_WithEmptyStepId_ShouldThrowException()
    {
        // Arrange
        var answer = new StepAnswer
        {
            Id = Guid.NewGuid(),
            SessionId = Guid.NewGuid(),
            StepId = "",
            SchemaVersion = "v1",
            AnswerData = "{}",
            Status = AnswerStatus.Submitted,
            SubmittedAt = DateTime.UtcNow
        };

        // Act
        var action = () => answer.Validate();

        // Assert
        action.Should().Throw<InvalidOperationException>()
            .WithMessage("StepId is required");
    }

    [Fact]
    public void Validate_WithEmptySchemaVersion_ShouldThrowException()
    {
        // Arrange
        var answer = new StepAnswer
        {
            Id = Guid.NewGuid(),
            SessionId = Guid.NewGuid(),
            StepId = "household-size",
            SchemaVersion = "",
            AnswerData = "{}",
            Status = AnswerStatus.Submitted,
            SubmittedAt = DateTime.UtcNow
        };

        // Act
        var action = () => answer.Validate();

        // Assert
        action.Should().Throw<InvalidOperationException>()
            .WithMessage("SchemaVersion is required");
    }
}
