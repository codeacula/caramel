namespace Caramel.Twitch;

/// <summary>
/// Configuration for the Twitch bot integration.
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
  /// Twitch user ID of the bot account.
  /// </summary>
  public required string BotUserId { get; init; }

  /// <summary>
  /// Comma-separated list of Twitch channel IDs to join and listen to.
  /// </summary>
  public required string ChannelIds { get; init; }

  /// <summary>
  /// OAuth callback URL for the authorization code flow (e.g., http://localhost:5146/auth/callback).
  /// </summary>
  public required string OAuthCallbackUrl { get; init; }

  /// <summary>
  /// Base64-encoded 32-byte encryption key for securing OAuth state parameters.
  /// Generate with: `openssl rand -base64 32`
  /// </summary>
  public required string EncryptionKey { get; init; }
}
