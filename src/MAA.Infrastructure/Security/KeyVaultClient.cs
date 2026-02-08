using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using MAA.Application.Services;
using MAA.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;

namespace MAA.Infrastructure.Security;

/// <summary>
/// Azure Key Vault client implementation for encryption key management.
/// Implements 5-minute caching per research.md R3 recommendations.
/// Supports key versioning and rotation without decrypting existing sessions.
/// CONST-I: Resilient with fallback to cached keys on Key Vault unavailability.
/// </summary>
public class KeyVaultClient : IKeyVaultClient
{
    private readonly SecretClient _secretClient;
    private readonly IMemoryCache _cache;
    private readonly SessionContext _context;
    private const int CACHE_TTL_MINUTES = 5;
    private const string CACHE_KEY_PREFIX = "encryption_key_v";

    public KeyVaultClient(
        IConfiguration configuration,
        IMemoryCache cache,
        SessionContext context)
    {
        _cache = cache;
        _context = context;

        var vaultUri = configuration["Azure:KeyVault:Uri"]
            ?? throw new InvalidOperationException("Azure Key Vault URI not configured");

        // Use DefaultAzureCredential for flexible authentication
        // Supports: Managed Identity (production), Azure CLI (local dev), Environment variables
        var credential = new DefaultAzureCredential();
        _secretClient = new SecretClient(new Uri(vaultUri), credential);
    }

    /// <summary>
    /// Retrieves encryption key material for the specified version.
    /// Implements 5-minute caching per R3 recommendations.
    /// </summary>
    public async Task<string> GetKeyAsync(int keyVersion, CancellationToken cancellationToken = default)
    {
        if (keyVersion <= 0)
            throw new ArgumentException("Key version must be positive", nameof(keyVersion));

        var cacheKey = $"{CACHE_KEY_PREFIX}{keyVersion}";

        // Try cache first
        if (_cache.TryGetValue(cacheKey, out string? cachedKey) && cachedKey != null)
            return cachedKey;

        try
        {
            // Retrieve from Key Vault
            var encryptionKey = await _context.EncryptionKeys
                .Where(k => k.KeyVersion == keyVersion)
                .FirstOrDefaultAsync(cancellationToken)
                ?? throw new InvalidOperationException($"Encryption key version {keyVersion} not found in database");

            var secret = await _secretClient.GetSecretAsync(encryptionKey.KeyIdVault, cancellationToken: cancellationToken);
            var keyMaterial = secret.Value.Value;

            // Cache for 5 minutes
            _cache.Set(cacheKey, keyMaterial, new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(CACHE_TTL_MINUTES)
            });

            return keyMaterial;
        }
        catch (Exception ex)
        {
            // Fallback: try to use cached key if Key Vault is unavailable
            if (_cache.TryGetValue(cacheKey, out string? fallbackKey) && fallbackKey != null)
            {
                // Log warning but continue with cached key
                // TODO: Add Application Insights logging in future
                return fallbackKey;
            }

            throw new InvalidOperationException(
                $"Failed to retrieve encryption key version {keyVersion} from Key Vault and no cached fallback available",
                ex);
        }
    }

    /// <summary>
    /// Gets the current active encryption key version from the database.
    /// </summary>
    public async Task<int> GetCurrentKeyVersionAsync(CancellationToken cancellationToken = default)
    {
        var activeKey = await _context.EncryptionKeys
            .Where(k => k.IsActive)
            .OrderByDescending(k => k.KeyVersion)
            .FirstOrDefaultAsync(cancellationToken);

        if (activeKey == null)
            throw new InvalidOperationException("No active encryption key found in database");

        return activeKey.KeyVersion;
    }

    /// <summary>
    /// Creates a new encryption key in Azure Key Vault during key rotation.
    /// Returns the Key Vault secret name (key ID).
    /// </summary>
    public async Task<string> RotateKeyAsync(string algorithm, CancellationToken cancellationToken = default)
    {
        // Get the next key version number
        var maxKeyVersion = await _context.EncryptionKeys
            .MaxAsync(k => (int?)k.KeyVersion, cancellationToken) ?? 0;
        var newKeyVersion = maxKeyVersion + 1;

        // Generate new 256-bit key material
        var keyBytes = new byte[32]; // 256 bits
        using (var rng = System.Security.Cryptography.RandomNumberGenerator.Create())
        {
            rng.GetBytes(keyBytes);
        }
        var keyMaterial = Convert.ToBase64String(keyBytes);

        // Store in Key Vault
        var keyName = $"maa-key-v{newKeyVersion}";
        await _secretClient.SetSecretAsync(keyName, keyMaterial, cancellationToken);

        // Create database record
        var encryptionKey = new Domain.Sessions.EncryptionKey
        {
            Id = Guid.NewGuid(),
            KeyVersion = newKeyVersion,
            KeyIdVault = keyName,
            Algorithm = algorithm,
            IsActive = false, // Manual activation required
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddYears(1)
        };

        _context.EncryptionKeys.Add(encryptionKey);
        await _context.SaveChangesAsync(cancellationToken);

        return keyName;
    }

    /// <summary>
    /// Lists all encryption keys in Key Vault.
    /// </summary>
    public async Task<IEnumerable<string>> ListKeysAsync(CancellationToken cancellationToken = default)
    {
        var keys = new List<string>();
        await foreach (var secretProperties in _secretClient.GetPropertiesOfSecretsAsync(cancellationToken))
        {
            if (secretProperties.Name.StartsWith("maa-key-v"))
            {
                keys.Add(secretProperties.Name);
            }
        }
        return keys;
    }
}
