namespace Caramel.Domain.Twitch;

/// <summary>
/// Represents OAuth tokens and expiry for a single Twitch account (bot or broadcaster).
/// Tokens are stored encrypted in the database.
/// </summary>
public sealed record TwitchAccountTokens
{
    /// <summary>
    /// Numeric Twitch user ID for this account.
    /// </summary>
    public required string UserId { get; init; }

    /// <summary>
    /// Login name (username) for this account.
    /// </summary>
    public required string Login { get; init; }

    /// <summary>
    /// OAuth access token (encrypted in database).
    /// </summary>
    public required string AccessToken { get; init; }

    /// <summary>
    /// OAuth refresh token, if available (encrypted in database).
    /// May be null if the OAuth flow did not provide a refresh token.
    /// </summary>
    public string? RefreshToken { get; init; }

    /// <summary>
    /// UTC timestamp when the access token expires.
    /// </summary>
    public required DateTime ExpiresAt { get; init; }

    /// <summary>
    /// UTC timestamp when these tokens were last refreshed or obtained.
    /// </summary>
    public required DateTimeOffset LastRefreshedOn { get; init; }
}
