using MAA.Application.Services;

namespace MAA.Infrastructure.Security;

/// <summary>
/// Stub implementation of IEncryptionService for Phase 4 US2.
/// Full implementation will be completed in Phase 6 US4 (T031-T033).
/// Currently returns placeholder encrypted values to enable compilation/testing.
/// TODO: Replace with Azure Key Vault integration in US4.
/// </summary>
public class EncryptionService : IEncryptionService
{
    private const string STUB_WARNING = "STUB: Encryption not yet implemented. Using placeholder. Implement in US4 (T031).";

    /// <summary>
    /// Stub: Encrypts plaintext (currently returns Base64 placeholder).
    /// </summary>
    public Task<string> EncryptAsync(string plaintext, int keyVersion, CancellationToken cancellationToken = default)
    {
        // TODO US4 (T031): Implement actual AES-256-GCM encryption with Azure Key Vault
        var placeholder = Convert.ToBase64String(
            System.Text.Encoding.UTF8.GetBytes($"ENCRYPTED[{plaintext}]|v{keyVersion}"));
        return Task.FromResult(placeholder);
    }

    /// <summary>
    /// Stub: Decrypts ciphertext (currently parses placeholder format).
    /// </summary>
    public Task<string> DecryptAsync(string ciphertext, int keyVersion, CancellationToken cancellationToken = default)
    {
        // TODO US4 (T031): Implement actual decryption
        try
        {
            var decoded = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(ciphertext));
            if (decoded.StartsWith("ENCRYPTED[") && decoded.Contains("]|"))
            {
                var startIndex = "ENCRYPTED[".Length;
                var endIndex = decoded.IndexOf("]|");
                var plaintext = decoded.Substring(startIndex, endIndex - startIndex);
                return Task.FromResult(plaintext);
            }
        }
        catch
        {
            // If not placeholder format, just return as-is (for test compatibility)
        }

        return Task.FromResult(ciphertext);
    }

    /// <summary>
    /// Stub: Generates deterministic hash (currently SHA256 without HMAC).
    /// </summary>
    public Task<string> HashAsync(string plaintext, int keyVersion, CancellationToken cancellationToken = default)
    {
        // TODO US4 (T031): Implement HMAC-SHA256 with key from Key Vault
        using var sha256 = System.Security.Cryptography.SHA256.Create();
        var hashBytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(plaintext + keyVersion));
        var hex = Convert.ToHexString(hashBytes);
        return Task.FromResult(hex);
    }

    /// <summary>
    /// Stub: Validates hash (re-computes and compares).
    /// </summary>
    public async Task<bool> ValidateHashAsync(
        string plaintext,
        string hash,
        int keyVersion,
        CancellationToken cancellationToken = default)
    {
        var computedHash = await HashAsync(plaintext, keyVersion, cancellationToken);
        return string.Equals(computedHash, hash, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Stub: Returns hardcoded key version 1.
    /// </summary>
    public Task<int> GetCurrentKeyVersionAsync(CancellationToken cancellationToken = default)
    {
        // TODO US4 (T032): Query encryption_keys table for active key version
        return Task.FromResult(1);
    }
}
