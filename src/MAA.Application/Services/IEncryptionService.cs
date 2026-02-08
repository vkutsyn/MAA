namespace MAA.Application.Services;

/// <summary>
/// Service interface for encryption operations.
/// Supports both randomized encryption (for PII) and deterministic hashing (for SSN lookup).
/// </summary>
public interface IEncryptionService
{
    /// <summary>
    /// Encrypts plaintext using randomized encryption (AES-256-GCM).
    /// Same plaintext produces different ciphertext each time (prevents pattern attacks).
    /// </summary>
    /// <param name="plaintext">Plain text to encrypt</param>
    /// <param name="keyVersion">Encryption key version to use</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Base64-encoded ciphertext</returns>
    Task<string> EncryptAsync(
        string plaintext, 
        int keyVersion, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Decrypts ciphertext using specified key version.
    /// </summary>
    /// <param name="ciphertext">Base64-encoded ciphertext</param>
    /// <param name="keyVersion">Encryption key version used for encryption</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Decrypted plaintext</returns>
    Task<string> DecryptAsync(
        string ciphertext, 
        int keyVersion, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates deterministic hash using HMAC-SHA256.
    /// Same plaintext always produces same hash (enables exact-match queries).
    /// Used for SSN lookup without full decryption.
    /// </summary>
    /// <param name="plaintext">Plain text to hash</param>
    /// <param name="keyVersion">Key version to use</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Hex-encoded hash</returns>
    Task<string> HashAsync(
        string plaintext, 
        int keyVersion, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates that plaintext matches a deterministic hash.
    /// </summary>
    /// <param name="plaintext">Plain text to validate</param>
    /// <param name="hash">Hex-encoded hash to compare against</param>
    /// <param name="keyVersion">Key version used to generate hash</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if hash matches</returns>
    Task<bool> ValidateHashAsync(
        string plaintext, 
        string hash, 
        int keyVersion, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current active encryption key version.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Current key version</returns>
    Task<int> GetCurrentKeyVersionAsync(CancellationToken cancellationToken = default);
}
