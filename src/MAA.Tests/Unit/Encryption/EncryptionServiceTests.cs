using FluentAssertions;
using MAA.Application.Services;
using MAA.Infrastructure.Security;
using Moq;
using Xunit;

namespace MAA.Tests.Unit.Encryption;

/// <summary>
/// Unit tests for EncryptionService.
/// Tests US4: Sensitive Data Encryption requirements.
/// Validates randomized encryption, deterministic hashing, and key versioning.
/// CONST-I: Testable in isolation with mock KeyVaultClient.
/// </summary>
[Trait("Category", "Unit")]
public class EncryptionServiceTests
{
    private readonly Mock<IKeyVaultClient> _keyVaultClientMock;
    private readonly EncryptionService _encryptionService;

    public EncryptionServiceTests()
    {
        _keyVaultClientMock = new Mock<IKeyVaultClient>();
        
        // Setup mock to return test encryption key (base64-encoded 256-bit key)
        var keyBytes = new byte[32]; // 256-bit key for AES-256
        var keyBase64 = Convert.ToBase64String(keyBytes);
        
        _keyVaultClientMock
            .Setup(x => x.GetKeyAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(keyBase64);

        _encryptionService = new EncryptionService(_keyVaultClientMock.Object);
    }

    #region EncryptAsync Tests

    /// <summary>
    /// US4 Acceptance Scenario 1: Randomized encryption - same plaintext produces different ciphertext.
    /// </summary>
    [Fact]
    public async Task EncryptAsync_SamePlaintext_ProducesDifferentCiphertext()
    {
        // Arrange
        var plaintext = "2100";
        var keyVersion = 1;

        // Act
        var ciphertext1 = await _encryptionService.EncryptAsync(plaintext, keyVersion);
        var ciphertext2 = await _encryptionService.EncryptAsync(plaintext, keyVersion);

        // Assert
        ciphertext1.Should().NotBeNullOrEmpty("encryption should produce output");
        ciphertext2.Should().NotBeNullOrEmpty("encryption should produce output");
        ciphertext1.Should().NotBe(ciphertext2,
            "randomized encryption should produce different ciphertext for same plaintext");
    }

    /// <summary>
    /// US4: Encrypted value is unreadable (not plaintext).
    /// </summary>
    [Fact]
    public async Task EncryptAsync_ProducesUnreadableCiphertext()
    {
        // Arrange
        var plaintext = "2100";
        var keyVersion = 1;

        // Act
        var ciphertext = await _encryptionService.EncryptAsync(plaintext, keyVersion);

        // Assert
        ciphertext.Should().NotContain(plaintext,
            "ciphertext should not contain plaintext value");
        
        // Should be Base64 encoded
        var isBase64 = IsBase64String(ciphertext);
        isBase64.Should().BeTrue("ciphertext should be Base64 encoded");
    }

    /// <summary>
    /// Empty plaintext throws exception.
    /// </summary>
    [Fact]
    public async Task EncryptAsync_EmptyPlaintext_ThrowsException()
    {
        // Arrange
        var plaintext = "";
        var keyVersion = 1;

        // Act
        Func<Task> act = async () => await _encryptionService.EncryptAsync(plaintext, keyVersion);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>("empty plaintext is invalid");
    }

    /// <summary>
    /// Invalid key version throws exception.
    /// </summary>
    [Fact]
    public async Task EncryptAsync_InvalidKeyVersion_ThrowsException()
    {
        // Arrange
        var plaintext = "2100";
        var keyVersion = 0;

        // Act
        Func<Task> act = async () => await _encryptionService.EncryptAsync(plaintext, keyVersion);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>("invalid key version should be rejected");
    }

    #endregion

    #region DecryptAsync Tests

    /// <summary>
    /// US4 Acceptance Scenario 3: Decryption returns original plaintext value.
    /// </summary>
    [Fact]
    public async Task DecryptAsync_ReturnsOriginalPlaintext()
    {
        // Arrange
        var plaintext = "2100";
        var keyVersion = 1;
        var ciphertext = await _encryptionService.EncryptAsync(plaintext, keyVersion);

        // Act
        var decrypted = await _encryptionService.DecryptAsync(ciphertext, keyVersion);

        // Assert
        decrypted.Should().Be(plaintext,
            "decryption should return original plaintext");
    }

    /// <summary>
    /// US4: Encryption/decryption roundtrip preserves data integrity.
    /// </summary>
    [Theory]
    [InlineData("2100")]
    [InlineData("123-45-6789")]
    [InlineData("true")]
    [InlineData("50000.50")]
    public async Task DecryptAsync_RoundTrip_PreservesData(string plaintext)
    {
        // Arrange
        var keyVersion = 1;
        var ciphertext = await _encryptionService.EncryptAsync(plaintext, keyVersion);

        // Act
        var decrypted = await _encryptionService.DecryptAsync(ciphertext, keyVersion);

        // Assert
        decrypted.Should().Be(plaintext,
            "roundtrip encryption/decryption should preserve data");
    }

    /// <summary>
    /// Wrong key version fails decryption.
    /// </summary>
    [Fact]
    public async Task DecryptAsync_WrongKeyVersion_ThrowsException()
    {
        // Arrange
        var plaintext = "2100";
        var keyVersion1 = 1;
        var keyVersion2 = 2;
        var ciphertext = await _encryptionService.EncryptAsync(plaintext, keyVersion1);

        // Setup different key for version 2 (base64-encoded)
        var differentKeyBytes = new byte[32] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31, 32 };
        var differentKeyBase64 = Convert.ToBase64String(differentKeyBytes);
        
        _keyVaultClientMock
            .Setup(x => x.GetKeyAsync(keyVersion2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(differentKeyBase64);

        // Act
        Func<Task> act = async () => await _encryptionService.DecryptAsync(ciphertext, keyVersion2);

        // Assert
        await act.Should().ThrowAsync<Exception>("decryption with wrong key should fail");
    }

    #endregion

    #region HashAsync Tests

    /// <summary>
    /// US4: Deterministic hash - same plaintext produces same hash.
    /// </summary>
    [Fact]
    public async Task HashAsync_SamePlaintext_ProducesSameHash()
    {
        // Arrange
        var plaintext = "123-45-6789";
        var keyVersion = 1;

        // Act
        var hash1 = await _encryptionService.HashAsync(plaintext, keyVersion);
        var hash2 = await _encryptionService.HashAsync(plaintext, keyVersion);

        // Assert
        hash1.Should().NotBeNullOrEmpty("hash should be generated");
        hash2.Should().NotBeNullOrEmpty("hash should be generated");
        hash1.Should().Be(hash2,
            "deterministic hash should produce same output for same plaintext");
    }

    /// <summary>
    /// US4: Different plaintext produces different hash.
    /// </summary>
    [Fact]
    public async Task HashAsync_DifferentPlaintext_ProducesDifferentHash()
    {
        // Arrange
        var plaintext1 = "123-45-6789";
        var plaintext2 = "987-65-4321";
        var keyVersion = 1;

        // Act
        var hash1 = await _encryptionService.HashAsync(plaintext1, keyVersion);
        var hash2 = await _encryptionService.HashAsync(plaintext2, keyVersion);

        // Assert
        hash1.Should().NotBe(hash2,
            "different plaintext should produce different hashes");
    }

    /// <summary>
    /// Hash output is hexadecimal string.
    /// </summary>
    [Fact]
    public async Task HashAsync_ProducesHexString()
    {
        // Arrange
        var plaintext = "123-45-6789";
        var keyVersion = 1;

        // Act
        var hash = await _encryptionService.HashAsync(plaintext, keyVersion);

        // Assert
        hash.Should().MatchRegex("^[0-9A-Fa-f]+$",
            "hash should be hexadecimal string");
        hash.Length.Should().BeGreaterThan(0);
    }

    #endregion

    #region ValidateHashAsync Tests

    /// <summary>
    /// US4: Hash validation succeeds for matching plaintext.
    /// </summary>
    [Fact]
    public async Task ValidateHashAsync_MatchingPlaintext_ReturnsTrue()
    {
        // Arrange
        var plaintext = "123-45-6789";
        var keyVersion = 1;
        var hash = await _encryptionService.HashAsync(plaintext, keyVersion);

        // Act
        var isValid = await _encryptionService.ValidateHashAsync(plaintext, hash, keyVersion);

        // Assert
        isValid.Should().BeTrue("validation should succeed for matching plaintext and hash");
    }

    /// <summary>
    /// US4: Hash validation fails for non-matching plaintext.
    /// </summary>
    [Fact]
    public async Task ValidateHashAsync_NonMatchingPlaintext_ReturnsFalse()
    {
        // Arrange
        var plaintext1 = "123-45-6789";
        var plaintext2 = "987-65-4321";
        var keyVersion = 1;
        var hash = await _encryptionService.HashAsync(plaintext1, keyVersion);

        // Act
        var isValid = await _encryptionService.ValidateHashAsync(plaintext2, hash, keyVersion);

        // Assert
        isValid.Should().BeFalse("validation should fail for non-matching plaintext");
    }

    #endregion

    #region GetCurrentKeyVersionAsync Tests

    /// <summary>
    /// US4: Gets current active encryption key version.
    /// </summary>
    [Fact]
    public async Task GetCurrentKeyVersionAsync_ReturnsActiveKeyVersion()
    {
        // Arrange
        _keyVaultClientMock
            .Setup(x => x.GetCurrentKeyVersionAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var keyVersion = await _encryptionService.GetCurrentKeyVersionAsync();

        // Assert
        keyVersion.Should().Be(1, "should return active key version");
    }

    #endregion

    #region Helper Methods

    private static bool IsBase64String(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return false;

        try
        {
            Convert.FromBase64String(value);
            return true;
        }
        catch
        {
            return false;
        }
    }

    #endregion
}
