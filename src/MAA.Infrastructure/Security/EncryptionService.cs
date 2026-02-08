using MAA.Application.Services;
using System.Security.Cryptography;
using System.Text;

namespace MAA.Infrastructure.Security;

/// <summary>
/// Production implementation of IEncryptionService with Azure Key Vault integration.
/// Uses AES-256-GCM for randomized encryption and HMAC-SHA256 for deterministic hashing.
/// Implements US4: Sensitive Data Encryption requirements.
/// CONST-I: Resilient with proper error handling and key version tracking.
/// </summary>
public class EncryptionService : IEncryptionService
{
    private readonly IKeyVaultClient _keyVaultClient;

    // AES-256-GCM constants
    private const int NONCE_SIZE_BYTES = 12; // 96 bits (recommended for GCM)
    private const int TAG_SIZE_BYTES = 16; // 128 bits (authentication tag)
    private const int KEY_SIZE_BYTES = 32; // 256 bits

    public EncryptionService(IKeyVaultClient keyVaultClient)
    {
        _keyVaultClient = keyVaultClient;
    }

    /// <summary>
    /// Encrypts plaintext using AES-256-GCM with random nonce.
    /// Same plaintext produces different ciphertext each time (prevents pattern attacks).
    /// Format: Base64(nonce + ciphertext + tag)
    /// </summary>
    public async Task<string> EncryptAsync(string plaintext, int keyVersion, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(plaintext))
            throw new ArgumentException("Plaintext cannot be null or empty", nameof(plaintext));

        if (keyVersion <= 0)
            throw new ArgumentException("Key version must be positive", nameof(keyVersion));

        try
        {
            // Get encryption key from Key Vault
            var keyMaterial = await _keyVaultClient.GetKeyAsync(keyVersion, cancellationToken);
            var keyBytes = Convert.FromBase64String(keyMaterial);

            if (keyBytes.Length != KEY_SIZE_BYTES)
                throw new InvalidOperationException($"Invalid key size. Expected {KEY_SIZE_BYTES} bytes, got {keyBytes.Length} bytes");

            // Generate random nonce
            var nonce = new byte[NONCE_SIZE_BYTES];
            RandomNumberGenerator.Fill(nonce);

            // Create ciphertext buffer (ciphertext + tag)
            var plaintextBytes = Encoding.UTF8.GetBytes(plaintext);
            var ciphertext = new byte[plaintextBytes.Length];
            var tag = new byte[TAG_SIZE_BYTES];

            // Encrypt using AES-256-GCM
            using var aesGcm = new AesGcm(keyBytes, TAG_SIZE_BYTES);
            aesGcm.Encrypt(nonce, plaintextBytes, ciphertext, tag);

            // Combine: nonce + ciphertext + tag
            var combined = new byte[NONCE_SIZE_BYTES + ciphertext.Length + TAG_SIZE_BYTES];
            Buffer.BlockCopy(nonce, 0, combined, 0, NONCE_SIZE_BYTES);
            Buffer.BlockCopy(ciphertext, 0, combined, NONCE_SIZE_BYTES, ciphertext.Length);
            Buffer.BlockCopy(tag, 0, combined, NONCE_SIZE_BYTES + ciphertext.Length, TAG_SIZE_BYTES);

            return Convert.ToBase64String(combined);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Encryption failed for key version {keyVersion}", ex);
        }
    }

    /// <summary>
    /// Decrypts ciphertext using AES-256-GCM.
    /// Expects format: Base64(nonce + ciphertext + tag)
    /// </summary>
    public async Task<string> DecryptAsync(string ciphertext, int keyVersion, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(ciphertext))
            throw new ArgumentException("Ciphertext cannot be null or empty", nameof(ciphertext));

        if (keyVersion <= 0)
            throw new ArgumentException("Key version must be positive", nameof(keyVersion));

        try
        {
            // Get encryption key from Key Vault
            var keyMaterial = await _keyVaultClient.GetKeyAsync(keyVersion, cancellationToken);
            var keyBytes = Convert.FromBase64String(keyMaterial);

            if (keyBytes.Length != KEY_SIZE_BYTES)
                throw new InvalidOperationException($"Invalid key size. Expected {KEY_SIZE_BYTES} bytes, got {keyBytes.Length} bytes");

            // Decode Base64
            var combined = Convert.FromBase64String(ciphertext);

            if (combined.Length < NONCE_SIZE_BYTES + TAG_SIZE_BYTES)
                throw new InvalidOperationException("Ciphertext is too short to contain nonce and tag");

            // Extract: nonce + ciphertext + tag
            var nonce = new byte[NONCE_SIZE_BYTES];
            var tag = new byte[TAG_SIZE_BYTES];
            var ciphertextBytes = new byte[combined.Length - NONCE_SIZE_BYTES - TAG_SIZE_BYTES];

            Buffer.BlockCopy(combined, 0, nonce, 0, NONCE_SIZE_BYTES);
            Buffer.BlockCopy(combined, NONCE_SIZE_BYTES, ciphertextBytes, 0, ciphertextBytes.Length);
            Buffer.BlockCopy(combined, NONCE_SIZE_BYTES + ciphertextBytes.Length, tag, 0, TAG_SIZE_BYTES);

            // Decrypt using AES-256-GCM
            var plaintext = new byte[ciphertextBytes.Length];
            using var aesGcm = new AesGcm(keyBytes, TAG_SIZE_BYTES);
            aesGcm.Decrypt(nonce, ciphertextBytes, tag, plaintext);

            return Encoding.UTF8.GetString(plaintext);
        }
        catch (CryptographicException ex)
        {
            throw new InvalidOperationException("Decryption failed. Ciphertext may be corrupted or tampered with.", ex);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Decryption failed for key version {keyVersion}", ex);
        }
    }

    /// <summary>
    /// Generates deterministic hash using HMAC-SHA256.
    /// Same plaintext always produces same hash (enables exact-match queries).
    /// </summary>
    public async Task<string> HashAsync(string plaintext, int keyVersion, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(plaintext))
            throw new ArgumentException("Plaintext cannot be null or empty", nameof(plaintext));

        if (keyVersion <= 0)
            throw new ArgumentException("Key version must be positive", nameof(keyVersion));

        try
        {
            // Get encryption key from Key Vault
            var keyMaterial = await _keyVaultClient.GetKeyAsync(keyVersion, cancellationToken);
            var keyBytes = Convert.FromBase64String(keyMaterial);

            // Compute HMAC-SHA256
            var plaintextBytes = Encoding.UTF8.GetBytes(plaintext);
            using var hmac = new HMACSHA256(keyBytes);
            var hashBytes = hmac.ComputeHash(plaintextBytes);

            // Return hexadecimal string
            return Convert.ToHexString(hashBytes);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Hashing failed for key version {keyVersion}", ex);
        }
    }

    /// <summary>
    /// Validates that plaintext matches a deterministic hash.
    /// </summary>
    public async Task<bool> ValidateHashAsync(
        string plaintext,
        string hash,
        int keyVersion,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(hash))
            return false;

        try
        {
            var computedHash = await HashAsync(plaintext, keyVersion, cancellationToken);
            return string.Equals(computedHash, hash, StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Gets the current active encryption key version from Key Vault client.
    /// </summary>
    public async Task<int> GetCurrentKeyVersionAsync(CancellationToken cancellationToken = default)
    {
        return await _keyVaultClient.GetCurrentKeyVersionAsync(cancellationToken);
    }
}
