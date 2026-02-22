namespace Caramel.Twitch;

/// <summary>
/// Configuration for the Twitch bot integration.
/// Bot/channel identity (BotUserId, ChannelIds) is stored in the database and
/// managed via the Twitch setup wizard, not in application config.
/// </summary>
public sealed record TwitchConfig
{
  /// <summary>
  /// Twitch application Client ID.
  /// </summary>
  public required string ClientId { get; init; }

  /// <summary>
  /// Twitch application Client Secret.
  /// </summary>
  public required string ClientSecret { get; init; }

  /// <summary>
  /// OAuth access token for the bot account.
  /// Must have scopes: user:bot, user:read:chat, user:write:chat, user:manage:whispers
  /// </summary>
  public required string AccessToken { get; init; }

  /// <summary>
  /// OAuth refresh token for token renewal.
  /// </summary>
  public string? RefreshToken { get; init; }

  /// <summary>
  /// OAuth callback URL for the authorization code flow (e.g., http://localhost:8080/auth/callback).
  /// </summary>
  public required string OAuthCallbackUrl { get; init; }

  /// <summary>
  /// Base64-encoded 32-byte encryption key for securing OAuth state parameters.
  /// Generate with: `openssl rand -base64 32`
  /// </summary>
  public required string EncryptionKey { get; init; }
}
