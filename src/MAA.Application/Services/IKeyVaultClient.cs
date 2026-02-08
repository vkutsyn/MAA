namespace MAA.Application.Services;

/// <summary>
/// Service interface for Azure Key Vault operations.
/// Manages encryption key retrieval, rotation, and caching.
/// </summary>
public interface IKeyVaultClient
{
    /// <summary>
    /// Retrieves an encryption key from Azure Key Vault.
    /// Results are cached for 5 minutes to reduce Key Vault API calls.
    /// </summary>
    /// <param name="keyVersion">Key version to retrieve</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Key material (base64-encoded)</returns>
    Task<string> GetKeyAsync(int keyVersion, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new key in Azure Key Vault (manual trigger).
    /// Used during key rotation process.
    /// </summary>
    /// <param name="algorithm">Encryption algorithm (e.g., "AES-256")</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>New key ID</returns>
    Task<string> RotateKeyAsync(string algorithm, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists all active encryption keys in Key Vault.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of key IDs</returns>
    Task<IEnumerable<string>> ListKeysAsync(CancellationToken cancellationToken = default);
}
