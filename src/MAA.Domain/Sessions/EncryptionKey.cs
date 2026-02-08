using System.ComponentModel.DataAnnotations;

namespace MAA.Domain.Sessions;

/// <summary>
/// Represents an encryption key used for session data encryption.
/// Supports key versioning for rolling key rotation without data loss.
/// </summary>
public class EncryptionKey
{
    /// <summary>
    /// Unique encryption key identifier (GUID).
    /// </summary>
    [Key]
    public Guid Id { get; set; }

    /// <summary>
    /// Key version number (incremented on each rotation).
    /// Used to track which key encrypted specific data.
    /// </summary>
    [Required]
    public int KeyVersion { get; set; }

    /// <summary>
    /// Reference to Azure Key Vault key ID or identifier.
    /// Format: "maa-key-v001", "maa-key-v002", etc.
    /// </summary>
    [Required]
    [MaxLength(200)]
    public string KeyIdVault { get; set; } = string.Empty;

    /// <summary>
    /// Encryption algorithm (e.g., "AES-256-GCM", "HMAC-SHA256").
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string Algorithm { get; set; } = string.Empty;

    /// <summary>
    /// Whether this key is currently active for new encryptions.
    /// Only one key per algorithm should be active at a time.
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// Key creation timestamp.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// When key was rotated out (deactivated).
    /// Null if still active.
    /// </summary>
    public DateTime? RotatedAt { get; set; }

    /// <summary>
    /// When key expires and should no longer be used for decryption.
    /// Null if no expiration.
    /// </summary>
    public DateTime? ExpiresAt { get; set; }

    /// <summary>
    /// JSONB storage for additional key metadata (policy, rotation reason, etc.).
    /// </summary>
    public string? Metadata { get; set; }

    /// <summary>
    /// Domain validation: Ensures key configuration is valid.
    /// </summary>
    public void Validate()
    {
        if (Id == Guid.Empty)
            throw new InvalidOperationException("EncryptionKey ID cannot be empty");

        if (KeyVersion < 1)
            throw new InvalidOperationException("KeyVersion must be >= 1");

        if (string.IsNullOrWhiteSpace(KeyIdVault))
            throw new InvalidOperationException("KeyIdVault is required");

        if (string.IsNullOrWhiteSpace(Algorithm))
            throw new InvalidOperationException("Algorithm is required");

        if (ExpiresAt.HasValue && ExpiresAt.Value <= DateTime.UtcNow)
            throw new InvalidOperationException("ExpiresAt must be in the future if set");
    }

    /// <summary>
    /// Checks if key is currently valid for use.
    /// </summary>
    public bool IsValidForUse()
    {
        if (!IsActive)
            return false;

        if (ExpiresAt.HasValue && ExpiresAt.Value <= DateTime.UtcNow)
            return false;

        return true;
    }

    /// <summary>
    /// Deactivates the key (for rotation).
    /// </summary>
    public void Deactivate()
    {
        IsActive = false;
        RotatedAt = DateTime.UtcNow;
    }
}
