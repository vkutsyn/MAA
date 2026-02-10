using FluentAssertions;
using MAA.Domain.Sessions;
using Xunit;

namespace MAA.Tests.Unit;

/// <summary>
/// Unit tests for EncryptionKey domain entity validation and key rotation logic.
/// Tests cover: validation rules, key versioning, activation/deactivation, expiration.
/// </summary>
[Trait("Category", "Unit")]
public class EncryptionKeyTests
{
    [Fact]
    public void NewEncryptionKey_ShouldHaveDefaultValues()
    {
        // Arrange & Act
        var key = new EncryptionKey();

        // Assert
        key.Id.Should().Be(Guid.Empty);
        key.KeyVersion.Should().Be(0);
        key.KeyIdVault.Should().BeEmpty();
        key.Algorithm.Should().BeEmpty();
        key.IsActive.Should().BeFalse();
        key.RotatedAt.Should().BeNull();
        key.ExpiresAt.Should().BeNull();
        key.Metadata.Should().BeNull();
    }

    [Fact]
    public void EncryptionKey_ShouldAllowSettingAllProperties()
    {
        // Arrange
        var keyId = Guid.NewGuid();
        var now = DateTime.UtcNow;

        // Act
        var key = new EncryptionKey
        {
            Id = keyId,
            KeyVersion = 1,
            KeyIdVault = "maa-key-v001",
            Algorithm = "AES-256-GCM",
            IsActive = true,
            CreatedAt = now,
            ExpiresAt = now.AddYears(1),
            Metadata = "{\"reason\": \"initial_key\"}"
        };

        // Assert
        key.Id.Should().Be(keyId);
        key.KeyVersion.Should().Be(1);
        key.KeyIdVault.Should().Be("maa-key-v001");
        key.Algorithm.Should().Be("AES-256-GCM");
        key.IsActive.Should().BeTrue();
        key.CreatedAt.Should().Be(now);
        key.ExpiresAt.Should().Be(now.AddYears(1));
    }

    #region Validation Tests

    [Fact]
    public void Validate_WithEmptyId_ShouldThrowException()
    {
        // Arrange
        var key = new EncryptionKey
        {
            Id = Guid.Empty,
            KeyVersion = 1,
            KeyIdVault = "maa-key-v001",
            Algorithm = "AES-256-GCM",
            IsActive = true
        };

        // Act
        var action = () => key.Validate();

        // Assert
        action.Should().Throw<InvalidOperationException>()
            .WithMessage("EncryptionKey ID cannot be empty");
    }

    [Fact]
    public void Validate_WithZeroKeyVersion_ShouldThrowException()
    {
        // Arrange
        var key = new EncryptionKey
        {
            Id = Guid.NewGuid(),
            KeyVersion = 0,
            KeyIdVault = "maa-key-v001",
            Algorithm = "AES-256-GCM",
            IsActive = true
        };

        // Act
        var action = () => key.Validate();

        // Assert
        action.Should().Throw<InvalidOperationException>()
            .WithMessage("KeyVersion must be >= 1");
    }

    [Fact]
    public void Validate_WithNegativeKeyVersion_ShouldThrowException()
    {
        // Arrange
        var key = new EncryptionKey
        {
            Id = Guid.NewGuid(),
            KeyVersion = -1,
            KeyIdVault = "maa-key-v001",
            Algorithm = "AES-256-GCM",
            IsActive = true
        };

        // Act
        var action = () => key.Validate();

        // Assert
        action.Should().Throw<InvalidOperationException>()
            .WithMessage("KeyVersion must be >= 1");
    }

    [Fact]
    public void Validate_WithEmptyKeyIdVault_ShouldThrowException()
    {
        // Arrange
        var key = new EncryptionKey
        {
            Id = Guid.NewGuid(),
            KeyVersion = 1,
            KeyIdVault = "",
            Algorithm = "AES-256-GCM",
            IsActive = true
        };

        // Act
        var action = () => key.Validate();

        // Assert
        action.Should().Throw<InvalidOperationException>()
            .WithMessage("KeyIdVault is required");
    }

    [Fact]
    public void Validate_WithWhitespaceKeyIdVault_ShouldThrowException()
    {
        // Arrange
        var key = new EncryptionKey
        {
            Id = Guid.NewGuid(),
            KeyVersion = 1,
            KeyIdVault = "   ",
            Algorithm = "AES-256-GCM",
            IsActive = true
        };

        // Act
        var action = () => key.Validate();

        // Assert
        action.Should().Throw<InvalidOperationException>()
            .WithMessage("KeyIdVault is required");
    }

    [Fact]
    public void Validate_WithEmptyAlgorithm_ShouldThrowException()
    {
        // Arrange
        var key = new EncryptionKey
        {
            Id = Guid.NewGuid(),
            KeyVersion = 1,
            KeyIdVault = "maa-key-v001",
            Algorithm = "",
            IsActive = true
        };

        // Act
        var action = () => key.Validate();

        // Assert
        action.Should().Throw<InvalidOperationException>()
            .WithMessage("Algorithm is required");
    }

    [Fact]
    public void Validate_WithPastExpiresAt_ShouldThrowException()
    {
        // Arrange
        var key = new EncryptionKey
        {
            Id = Guid.NewGuid(),
            KeyVersion = 1,
            KeyIdVault = "maa-key-v001",
            Algorithm = "AES-256-GCM",
            IsActive = true,
            ExpiresAt = DateTime.UtcNow.AddDays(-1) // Expired yesterday
        };

        // Act
        var action = () => key.Validate();

        // Assert
        action.Should().Throw<InvalidOperationException>()
            .WithMessage("ExpiresAt must be in the future if set");
    }

    [Fact]
    public void Validate_WithFutureExpiresAt_ShouldNotThrow()
    {
        // Arrange
        var key = new EncryptionKey
        {
            Id = Guid.NewGuid(),
            KeyVersion = 1,
            KeyIdVault = "maa-key-v001",
            Algorithm = "AES-256-GCM",
            IsActive = true,
            ExpiresAt = DateTime.UtcNow.AddYears(1)
        };

        // Act
        var action = () => key.Validate();

        // Assert
        action.Should().NotThrow();
    }

    [Fact]
    public void Validate_WithNullExpiresAt_ShouldNotThrow()
    {
        // Arrange
        var key = new EncryptionKey
        {
            Id = Guid.NewGuid(),
            KeyVersion = 1,
            KeyIdVault = "maa-key-v001",
            Algorithm = "AES-256-GCM",
            IsActive = true,
            ExpiresAt = null
        };

        // Act
        var action = () => key.Validate();

        // Assert
        action.Should().NotThrow();
    }

    #endregion

    #region IsValidForUse Tests

    [Fact]
    public void IsValidForUse_WhenActiveAndNotExpired_ShouldReturnTrue()
    {
        // Arrange
        var key = new EncryptionKey
        {
            Id = Guid.NewGuid(),
            KeyVersion = 1,
            KeyIdVault = "maa-key-v001",
            Algorithm = "AES-256-GCM",
            IsActive = true,
            ExpiresAt = DateTime.UtcNow.AddYears(1)
        };

        // Act
        var result = key.IsValidForUse();

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsValidForUse_WhenActiveWithNoExpiration_ShouldReturnTrue()
    {
        // Arrange
        var key = new EncryptionKey
        {
            Id = Guid.NewGuid(),
            KeyVersion = 1,
            KeyIdVault = "maa-key-v001",
            Algorithm = "AES-256-GCM",
            IsActive = true,
            ExpiresAt = null
        };

        // Act
        var result = key.IsValidForUse();

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsValidForUse_WhenInactive_ShouldReturnFalse()
    {
        // Arrange
        var key = new EncryptionKey
        {
            Id = Guid.NewGuid(),
            KeyVersion = 1,
            KeyIdVault = "maa-key-v001",
            Algorithm = "AES-256-GCM",
            IsActive = false,
            ExpiresAt = DateTime.UtcNow.AddYears(1)
        };

        // Act
        var result = key.IsValidForUse();

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsValidForUse_WhenExpired_ShouldReturnFalse()
    {
        // Arrange
        var key = new EncryptionKey
        {
            Id = Guid.NewGuid(),
            KeyVersion = 1,
            KeyIdVault = "maa-key-v001",
            Algorithm = "AES-256-GCM",
            IsActive = true,
            ExpiresAt = DateTime.UtcNow.AddSeconds(-1) // Expired 1 second ago
        };

        // Act
        var result = key.IsValidForUse();

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsValidForUse_WhenInactiveAndExpired_ShouldReturnFalse()
    {
        // Arrange
        var key = new EncryptionKey
        {
            Id = Guid.NewGuid(),
            KeyVersion = 1,
            KeyIdVault = "maa-key-v001",
            Algorithm = "AES-256-GCM",
            IsActive = false,
            ExpiresAt = DateTime.UtcNow.AddDays(-1)
        };

        // Act
        var result = key.IsValidForUse();

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region Deactivate Tests

    [Fact]
    public void Deactivate_ShouldSetIsActiveFalse()
    {
        // Arrange
        var key = new EncryptionKey
        {
            Id = Guid.NewGuid(),
            KeyVersion = 1,
            KeyIdVault = "maa-key-v001",
            Algorithm = "AES-256-GCM",
            IsActive = true
        };

        // Act
        key.Deactivate();

        // Assert
        key.IsActive.Should().BeFalse();
    }

    [Fact]
    public void Deactivate_ShouldSetRotatedAtToCurrentTime()
    {
        // Arrange
        var key = new EncryptionKey
        {
            Id = Guid.NewGuid(),
            KeyVersion = 1,
            KeyIdVault = "maa-key-v001",
            Algorithm = "AES-256-GCM",
            IsActive = true,
            RotatedAt = null
        };
        var beforeDeactivate = DateTime.UtcNow;

        // Act
        key.Deactivate();

        // Assert
        var afterDeactivate = DateTime.UtcNow;
        key.RotatedAt.Should().NotBeNull();
        key.RotatedAt.Should().BeOnOrAfter(beforeDeactivate);
        key.RotatedAt.Should().BeOnOrBefore(afterDeactivate);
    }

    [Fact]
    public void Deactivate_CalledMultipleTimes_ShouldUpdateRotatedAt()
    {
        // Arrange
        var key = new EncryptionKey
        {
            Id = Guid.NewGuid(),
            KeyVersion = 1,
            KeyIdVault = "maa-key-v001",
            Algorithm = "AES-256-GCM",
            IsActive = true
        };

        // Act
        key.Deactivate();
        var firstRotatedAt = key.RotatedAt;

        System.Threading.Thread.Sleep(10); // Ensure time difference

        key.IsActive = true; // Reactivate
        key.Deactivate(); // Deactivate again
        var secondRotatedAt = key.RotatedAt;

        // Assert
        secondRotatedAt.Should().BeAfter(firstRotatedAt!.Value);
    }

    #endregion

    #region Key Versioning Tests

    [Theory]
    [InlineData(1)]
    [InlineData(5)]
    [InlineData(100)]
    public void EncryptionKey_ShouldSupportMultipleVersions(int version)
    {
        // Arrange & Act
        var key = new EncryptionKey
        {
            Id = Guid.NewGuid(),
            KeyVersion = version,
            KeyIdVault = $"maa-key-v{version:D3}",
            Algorithm = "AES-256-GCM",
            IsActive = version == 100 // Only latest is active
        };

        // Act
        var action = () => key.Validate();

        // Assert
        action.Should().NotThrow();
        key.KeyVersion.Should().Be(version);
    }

    #endregion

    #region Algorithm Support Tests

    [Theory]
    [InlineData("AES-256-GCM")]
    [InlineData("HMAC-SHA256")]
    [InlineData("AES-128-CBC")]
    [InlineData("ChaCha20-Poly1305")]
    public void EncryptionKey_ShouldSupportVariousAlgorithms(string algorithm)
    {
        // Arrange & Act
        var key = new EncryptionKey
        {
            Id = Guid.NewGuid(),
            KeyVersion = 1,
            KeyIdVault = "maa-key-v001",
            Algorithm = algorithm,
            IsActive = true
        };

        // Act
        var action = () => key.Validate();

        // Assert
        action.Should().NotThrow();
        key.Algorithm.Should().Be(algorithm);
    }

    #endregion

    #region Complete Valid Scenarios

    [Fact]
    public void Validate_CompleteValidActiveKey_ShouldPass()
    {
        // Arrange
        var key = new EncryptionKey
        {
            Id = Guid.NewGuid(),
            KeyVersion = 1,
            KeyIdVault = "maa-key-v001",
            Algorithm = "AES-256-GCM",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddYears(1),
            Metadata = "{\"purpose\": \"session_encryption\"}"
        };

        // Act
        var action = () => key.Validate();

        // Assert
        action.Should().NotThrow();
    }

    [Fact]
    public void Validate_CompleteValidRotatedKey_ShouldPass()
    {
        // Arrange
        var key = new EncryptionKey
        {
            Id = Guid.NewGuid(),
            KeyVersion = 1,
            KeyIdVault = "maa-key-v001",
            Algorithm = "AES-256-GCM",
            IsActive = false,
            CreatedAt = DateTime.UtcNow.AddYears(-1),
            RotatedAt = DateTime.UtcNow.AddDays(-30),
            ExpiresAt = DateTime.UtcNow.AddYears(1),
            Metadata = "{\"rotation_reason\": \"scheduled_rotation\"}"
        };

        // Act
        var action = () => key.Validate();

        // Assert
        action.Should().NotThrow();
    }

    #endregion

    #region Key Rotation Scenario Tests

    [Fact]
    public void KeyRotationScenario_ShouldWorkCorrectly()
    {
        // Arrange - Create initial active key
        var oldKey = new EncryptionKey
        {
            Id = Guid.NewGuid(),
            KeyVersion = 1,
            KeyIdVault = "maa-key-v001",
            Algorithm = "AES-256-GCM",
            IsActive = true,
            CreatedAt = DateTime.UtcNow.AddMonths(-6)
        };

        // Act - Rotate to new key
        oldKey.Deactivate();

        var newKey = new EncryptionKey
        {
            Id = Guid.NewGuid(),
            KeyVersion = 2,
            KeyIdVault = "maa-key-v002",
            Algorithm = "AES-256-GCM",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        // Assert
        oldKey.IsActive.Should().BeFalse();
        oldKey.RotatedAt.Should().NotBeNull();
        oldKey.IsValidForUse().Should().BeFalse();

        newKey.IsActive.Should().BeTrue();
        newKey.RotatedAt.Should().BeNull();
        newKey.IsValidForUse().Should().BeTrue();
        newKey.KeyVersion.Should().Be(oldKey.KeyVersion + 1);
    }

    #endregion
}
