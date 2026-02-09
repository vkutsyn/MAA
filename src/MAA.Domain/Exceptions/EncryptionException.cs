namespace MAA.Domain.Exceptions;

/// <summary>
/// Exception thrown when encryption or decryption operations fail.
/// Indicates cryptographic operation errors, key issues, or invalid ciphertext.
/// </summary>
public class EncryptionException : Exception
{
    /// <summary>
    /// Gets the key version that was used (if applicable).
    /// </summary>
    public int? KeyVersion { get; }

    /// <summary>
    /// Gets the operation type that failed (Encrypt, Decrypt, Hash, etc.).
    /// </summary>
    public string Operation { get; }

    /// <summary>
    /// Creates a new encryption exception.
    /// </summary>
    /// <param name="message">Error message</param>
    /// <param name="operation">Type of operation that failed</param>
    /// <param name="innerException">Inner exception if any</param>
    public EncryptionException(string message, string operation = "Unknown", Exception? innerException = null)
        : base(message, innerException)
    {
        Operation = operation;
    }

    /// <summary>
    /// Creates a new encryption exception with key version information.
    /// </summary>
    /// <param name="message">Error message</param>
    /// <param name="operation">Type of operation that failed</param>
    /// <param name="keyVersion">Key version that was used</param>
    /// <param name="innerException">Inner exception if any</param>
    public EncryptionException(string message, string operation, int keyVersion, Exception? innerException = null)
        : base(message, innerException)
    {
        Operation = operation;
        KeyVersion = keyVersion;
    }
}
