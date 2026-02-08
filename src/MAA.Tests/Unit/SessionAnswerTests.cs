using FluentAssertions;
using MAA.Domain.Sessions;
using Xunit;

namespace MAA.Tests.Unit;

/// <summary>
/// Unit tests for SessionAnswer domain entity validation and business logic.
/// Tests cover: validation rules, PII handling, encryption requirements.
/// </summary>
[Trait("Category", "Unit")]
public class SessionAnswerTests
{
    [Fact]
    public void NewSessionAnswer_ShouldHaveDefaultValues()
    {
        // Arrange & Act
        var answer = new SessionAnswer();

        // Assert
        answer.Id.Should().Be(Guid.Empty);
        answer.SessionId.Should().Be(Guid.Empty);
        answer.FieldKey.Should().BeEmpty();
        answer.FieldType.Should().BeEmpty();
        answer.AnswerPlain.Should().BeNull();
        answer.AnswerEncrypted.Should().BeNull();
        answer.AnswerHash.Should().BeNull();
        answer.KeyVersion.Should().Be(0);
        answer.IsPii.Should().BeFalse();
        answer.Version.Should().Be(0);
    }

    [Fact]
    public void SessionAnswer_ShouldAllowSettingAllProperties()
    {
        // Arrange
        var answerId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        var now = DateTime.UtcNow;

        // Act
        var answer = new SessionAnswer
        {
            Id = answerId,
            SessionId = sessionId,
            FieldKey = "income_annual_2025",
            FieldType = "currency",
            AnswerPlain = "45000",
            KeyVersion = 1,
            IsPii = false,
            CreatedAt = now,
            UpdatedAt = now,
            Version = 1
        };

        // Assert
        answer.Id.Should().Be(answerId);
        answer.SessionId.Should().Be(sessionId);
        answer.FieldKey.Should().Be("income_annual_2025");
        answer.FieldType.Should().Be("currency");
        answer.AnswerPlain.Should().Be("45000");
        answer.KeyVersion.Should().Be(1);
        answer.IsPii.Should().BeFalse();
        answer.Version.Should().Be(1);
    }

    #region Validation Tests

    [Fact]
    public void Validate_WithEmptyId_ShouldThrowException()
    {
        // Arrange
        var answer = new SessionAnswer
        {
            Id = Guid.Empty,
            SessionId = Guid.NewGuid(),
            FieldKey = "field1",
            FieldType = "string",
            AnswerPlain = "value",
            IsPii = false
        };

        // Act
        var action = () => answer.Validate();

        // Assert
        action.Should().Throw<InvalidOperationException>()
            .WithMessage("SessionAnswer ID cannot be empty");
    }

    [Fact]
    public void Validate_WithEmptySessionId_ShouldThrowException()
    {
        // Arrange
        var answer = new SessionAnswer
        {
            Id = Guid.NewGuid(),
            SessionId = Guid.Empty,
            FieldKey = "field1",
            FieldType = "string",
            AnswerPlain = "value",
            IsPii = false
        };

        // Act
        var action = () => answer.Validate();

        // Assert
        action.Should().Throw<InvalidOperationException>()
            .WithMessage("SessionId cannot be empty");
    }

    [Fact]
    public void Validate_WithEmptyFieldKey_ShouldThrowException()
    {
        // Arrange
        var answer = new SessionAnswer
        {
            Id = Guid.NewGuid(),
            SessionId = Guid.NewGuid(),
            FieldKey = "",
            FieldType = "string",
            AnswerPlain = "value",
            IsPii = false
        };

        // Act
        var action = () => answer.Validate();

        // Assert
        action.Should().Throw<InvalidOperationException>()
            .WithMessage("FieldKey is required");
    }

    [Fact]
    public void Validate_WithWhitespaceFieldKey_ShouldThrowException()
    {
        // Arrange
        var answer = new SessionAnswer
        {
            Id = Guid.NewGuid(),
            SessionId = Guid.NewGuid(),
            FieldKey = "   ",
            FieldType = "string",
            AnswerPlain = "value",
            IsPii = false
        };

        // Act
        var action = () => answer.Validate();

        // Assert
        action.Should().Throw<InvalidOperationException>()
            .WithMessage("FieldKey is required");
    }

    [Fact]
    public void Validate_WithEmptyFieldType_ShouldThrowException()
    {
        // Arrange
        var answer = new SessionAnswer
        {
            Id = Guid.NewGuid(),
            SessionId = Guid.NewGuid(),
            FieldKey = "field1",
            FieldType = "",
            AnswerPlain = "value",
            IsPii = false
        };

        // Act
        var action = () => answer.Validate();

        // Assert
        action.Should().Throw<InvalidOperationException>()
            .WithMessage("FieldType is required");
    }

    [Fact]
    public void Validate_WithNegativeVersion_ShouldThrowException()
    {
        // Arrange
        var answer = new SessionAnswer
        {
            Id = Guid.NewGuid(),
            SessionId = Guid.NewGuid(),
            FieldKey = "field1",
            FieldType = "string",
            AnswerPlain = "value",
            IsPii = false,
            Version = -1
        };

        // Act
        var action = () => answer.Validate();

        // Assert
        action.Should().Throw<InvalidOperationException>()
            .WithMessage("Version cannot be negative");
    }

    #endregion

    #region PII Handling Tests

    [Fact]
    public void Validate_PiiField_WithMissingEncryptedValue_ShouldThrowException()
    {
        // Arrange
        var answer = new SessionAnswer
        {
            Id = Guid.NewGuid(),
            SessionId = Guid.NewGuid(),
            FieldKey = "ssn",
            FieldType = "string",
            IsPii = true,
            AnswerEncrypted = null, // Missing encrypted value
            KeyVersion = 1
        };

        // Act
        var action = () => answer.Validate();

        // Assert
        action.Should().Throw<InvalidOperationException>()
            .WithMessage("PII fields must have AnswerEncrypted");
    }

    [Fact]
    public void Validate_PiiField_WithEmptyEncryptedValue_ShouldThrowException()
    {
        // Arrange
        var answer = new SessionAnswer
        {
            Id = Guid.NewGuid(),
            SessionId = Guid.NewGuid(),
            FieldKey = "ssn",
            FieldType = "string",
            IsPii = true,
            AnswerEncrypted = "   ", // Whitespace only
            KeyVersion = 1
        };

        // Act
        var action = () => answer.Validate();

        // Assert
        action.Should().Throw<InvalidOperationException>()
            .WithMessage("PII fields must have AnswerEncrypted");
    }

    [Fact]
    public void Validate_PiiField_WithValidEncryptedValue_ShouldNotThrow()
    {
        // Arrange
        var answer = new SessionAnswer
        {
            Id = Guid.NewGuid(),
            SessionId = Guid.NewGuid(),
            FieldKey = "ssn",
            FieldType = "string",
            IsPii = true,
            AnswerEncrypted = "base64encodedciphertext==",
            KeyVersion = 1
        };

        // Act
        var action = () => answer.Validate();

        // Assert
        action.Should().NotThrow();
    }

    [Fact]
    public void Validate_NonPiiField_WithMissingPlainValue_ShouldThrowException()
    {
        // Arrange
        var answer = new SessionAnswer
        {
            Id = Guid.NewGuid(),
            SessionId = Guid.NewGuid(),
            FieldKey = "income",
            FieldType = "currency",
            IsPii = false,
            AnswerPlain = null, // Missing plain value
            KeyVersion = 1
        };

        // Act
        var action = () => answer.Validate();

        // Assert
        action.Should().Throw<InvalidOperationException>()
            .WithMessage("Non-PII fields must have AnswerPlain");
    }

    [Fact]
    public void Validate_NonPiiField_WithEmptyPlainValue_ShouldThrowException()
    {
        // Arrange
        var answer = new SessionAnswer
        {
            Id = Guid.NewGuid(),
            SessionId = Guid.NewGuid(),
            FieldKey = "income",
            FieldType = "currency",
            IsPii = false,
            AnswerPlain = "   ", // Whitespace only
            KeyVersion = 1
        };

        // Act
        var action = () => answer.Validate();

        // Assert
        action.Should().Throw<InvalidOperationException>()
            .WithMessage("Non-PII fields must have AnswerPlain");
    }

    [Fact]
    public void Validate_NonPiiField_WithValidPlainValue_ShouldNotThrow()
    {
        // Arrange
        var answer = new SessionAnswer
        {
            Id = Guid.NewGuid(),
            SessionId = Guid.NewGuid(),
            FieldKey = "income",
            FieldType = "currency",
            IsPii = false,
            AnswerPlain = "50000",
            KeyVersion = 1
        };

        // Act
        var action = () => answer.Validate();

        // Assert
        action.Should().NotThrow();
    }

    #endregion

    #region Field Type Tests

    [Theory]
    [InlineData("currency", "45000.50")]
    [InlineData("integer", "42")]
    [InlineData("string", "some text")]
    [InlineData("boolean", "true")]
    [InlineData("date", "2025-02-08")]
    public void SessionAnswer_ShouldSupportVariousFieldTypes(string fieldType, string plainValue)
    {
        // Arrange & Act
        var answer = new SessionAnswer
        {
            Id = Guid.NewGuid(),
            SessionId = Guid.NewGuid(),
            FieldKey = $"field_{fieldType}",
            FieldType = fieldType,
            AnswerPlain = plainValue,
            IsPii = false,
            KeyVersion = 1
        };

        // Act
        var action = () => answer.Validate();

        // Assert
        action.Should().NotThrow();
        answer.FieldType.Should().Be(fieldType);
        answer.AnswerPlain.Should().Be(plainValue);
    }

    #endregion

    #region Key Version Tests

    [Fact]
    public void SessionAnswer_ShouldTrackEncryptionKeyVersion()
    {
        // Arrange
        var answer = new SessionAnswer
        {
            Id = Guid.NewGuid(),
            SessionId = Guid.NewGuid(),
            FieldKey = "ssn",
            FieldType = "string",
            IsPii = true,
            AnswerEncrypted = "encrypted_value",
            KeyVersion = 5 // Key rotation scenario
        };

        // Act
        var action = () => answer.Validate();

        // Assert
        action.Should().NotThrow();
        answer.KeyVersion.Should().Be(5);
    }

    [Fact]
    public void SessionAnswer_ShouldSupportOptionalAnswerHash()
    {
        // Arrange
        var answer = new SessionAnswer
        {
            Id = Guid.NewGuid(),
            SessionId = Guid.NewGuid(),
            FieldKey = "ssn",
            FieldType = "string",
            IsPii = true,
            AnswerEncrypted = "encrypted_value",
            AnswerHash = "deterministic_hash_for_search",
            KeyVersion = 1
        };

        // Act
        var action = () => answer.Validate();

        // Assert
        action.Should().NotThrow();
        answer.AnswerHash.Should().Be("deterministic_hash_for_search");
    }

    #endregion

    #region Complete Valid Scenarios

    [Fact]
    public void Validate_CompleteValidNonPiiAnswer_ShouldPass()
    {
        // Arrange
        var answer = new SessionAnswer
        {
            Id = Guid.NewGuid(),
            SessionId = Guid.NewGuid(),
            FieldKey = "household_size",
            FieldType = "integer",
            AnswerPlain = "4",
            IsPii = false,
            KeyVersion = 1,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            Version = 1
        };

        // Act
        var action = () => answer.Validate();

        // Assert
        action.Should().NotThrow();
    }

    [Fact]
    public void Validate_CompleteValidPiiAnswer_ShouldPass()
    {
        // Arrange
        var answer = new SessionAnswer
        {
            Id = Guid.NewGuid(),
            SessionId = Guid.NewGuid(),
            FieldKey = "ssn",
            FieldType = "string",
            AnswerEncrypted = "base64_encrypted_ssn==",
            AnswerHash = "sha256_hash_for_lookup",
            IsPii = true,
            KeyVersion = 1,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            Version = 1
        };

        // Act
        var action = () => answer.Validate();

        // Assert
        action.Should().NotThrow();
    }

    #endregion
}
