using MAA.Application.Services;
using MAA.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using System.Security.Cryptography;

namespace MAA.Infrastructure.Security;

/// <summary>
/// Local development implementation of IKeyVaultClient that uses a key from configuration.
/// Bypasses Azure Key Vault for local development environments.
/// DO NOT USE IN PRODUCTION - keys should always be in Azure Key Vault for production.
/// </summary>
public class LocalKeyVaultClient : IKeyVaultClient
{
    private readonly IMemoryCache _cache;
    private readonly SessionContext _context;
    private readonly string _localKey;
    private const int CACHE_TTL_MINUTES = 5;
    private const string CACHE_KEY_PREFIX = "encryption_key_v";

    public LocalKeyVaultClient(
        IConfiguration configuration,
        IMemoryCache cache,
        SessionContext context)
    {
        _cache = cache;
        _context = context;

        // Try to get local key from configuration
        _localKey = configuration["Azure:KeyVault:LocalDevelopmentKey"] 
            ?? GenerateRandomKey();
    }

    /// <summary>
    /// Generates a random 256-bit key for development if none is configured.
    /// </summary>
    private static string GenerateRandomKey()
    {
        var keyBytes = new byte[32]; // 256 bits
        RandomNumberGenerator.Fill(keyBytes);
        return Convert.ToBase64String(keyBytes);
    }

    /// <summary>
    /// Retrieves encryption key material for the specified version.
    /// In local mode, always returns the same local development key.
    /// </summary>
    public Task<string> GetKeyAsync(int keyVersion, CancellationToken cancellationToken = default)
    {
        if (keyVersion <= 0)
            throw new ArgumentException("Key version must be positive", nameof(keyVersion));

        var cacheKey = $"{CACHE_KEY_PREFIX}{keyVersion}";

        // Try cache first
        if (_cache.TryGetValue(cacheKey, out string? cachedKey) && cachedKey != null)
            return Task.FromResult(cachedKey);

        // Cache the local key
        _cache.Set(cacheKey, _localKey, new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(CACHE_TTL_MINUTES)
        });

        return Task.FromResult(_localKey);
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
    /// Creates a new encryption key during key rotation.
    /// In local mode, this is not supported.
    /// </summary>
    public Task<string> RotateKeyAsync(string algorithm, CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException("Key rotation is not supported in local development mode. Use Azure Key Vault in production.");
    }

    /// <summary>
    /// Lists all encryption keys.
    /// In local mode, returns only the configured local key.
    /// </summary>
    public Task<IEnumerable<string>> ListKeysAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IEnumerable<string>>(new[] { "local-dev-key-v1" });
    }
}
