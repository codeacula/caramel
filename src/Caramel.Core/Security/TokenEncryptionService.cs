namespace Caramel.Core.Security;

using Microsoft.AspNetCore.DataProtection;
using System.Security.Cryptography;

/// <summary>
/// Implements token encryption/decryption using ASP.NET Core Data Protection API.
/// Tokens are encrypted with application-level keys persisted to Redis, enabling
/// secure at-rest encryption even if the database is compromised.
/// </summary>
public sealed class TokenEncryptionService(IDataProtectionProvider dataProtectionProvider) : ITokenEncryptionService
{
    private readonly IDataProtector _protector = dataProtectionProvider.CreateProtector(
        "Caramel.TwitchOAuthTokens.v1");

    /// <inheritdoc />
    public string Encrypt(string plaintext)
    {
        if (string.IsNullOrEmpty(plaintext))
            throw new ArgumentException("Plaintext token cannot be null or empty", nameof(plaintext));

        try
        {
            return _protector.Protect(plaintext);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                "Token encryption failed. Ensure Data Protection is properly configured and keys are persisted to Redis.",
                ex);
        }
    }

    /// <inheritdoc />
    public string? TryDecrypt(string ciphertext)
    {
        if (string.IsNullOrEmpty(ciphertext))
            return null;

        try
        {
            return _protector.Unprotect(ciphertext);
        }
        catch (CryptographicException)
        {
            // Corrupted ciphertext, wrong key, or key not available
            // Return null to allow graceful degradation (force re-auth)
            return null;
        }
        catch (Exception)
        {
            // Any other exception (e.g., Data Protection not configured)
            return null;
        }
    }
}
