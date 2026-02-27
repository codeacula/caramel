namespace Caramel.Core.Security;

/// <summary>
/// Service for encrypting and decrypting OAuth tokens using ASP.NET Core Data Protection API.
/// </summary>
public interface ITokenEncryptionService
{
    /// <summary>
    /// Encrypts a plaintext token string.
    /// </summary>
    /// <param name="plaintext">The unencrypted token value</param>
    /// <returns>The encrypted (ciphertext) token</returns>
    /// <exception cref="InvalidOperationException">If encryption fails due to missing or misconfigured Data Protection</exception>
    string Encrypt(string plaintext);

    /// <summary>
    /// Attempts to decrypt a ciphertext token string.
    /// </summary>
    /// <param name="ciphertext">The encrypted token value</param>
    /// <returns>The decrypted plaintext token, or null if decryption fails (corrupted ciphertext, wrong key, etc.)</returns>
    string? TryDecrypt(string ciphertext);
}
