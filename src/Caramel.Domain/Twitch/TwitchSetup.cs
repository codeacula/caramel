namespace Caramel.Domain.Twitch;

/// <summary>
/// Represents a Twitch channel's bot and channel configuration with dual OAuth tokens.
/// Contains the numeric Twitch user IDs and login names for the bot and channel(s),
/// as well as OAuth tokens (encrypted) for both bot and broadcaster accounts.
/// </summary>
public sealed record TwitchSetup
{
  /// <summary>
  /// Numeric Twitch user ID of the bot account.
  /// </summary>
  public required string BotUserId { get; init; }

  /// <summary>
  /// Login name (username) of the bot account.
  /// </summary>
  public required string BotLogin { get; init; }

  /// <summary>
  /// List of configured channels (each with numeric ID and login name).
  /// </summary>
  public required IReadOnlyList<TwitchChannel> Channels { get; init; }

  /// <summary>
  /// UTC timestamp when the configuration was created.
  /// </summary>
  public required DateTimeOffset ConfiguredOn { get; init; }

  /// <summary>
  /// UTC timestamp when the configuration was last updated.
  /// </summary>
  public required DateTimeOffset UpdatedOn { get; init; }

  /// <summary>
  /// OAuth tokens for the bot account (encrypted in database).
  /// Null if the bot account has not yet been authenticated.
  /// </summary>
  public TwitchAccountTokens? BotTokens { get; init; }

  /// <summary>
  /// OAuth tokens for the broadcaster account (encrypted in database).
  /// Null if the broadcaster account has not yet been authenticated.
  /// </summary>
  public TwitchAccountTokens? BroadcasterTokens { get; init; }
}

/// <summary>
/// Represents a single Twitch channel configuration.
/// </summary>
public sealed record TwitchChannel
{
  /// <summary>
  /// Numeric Twitch user ID (broadcaster ID).
  /// </summary>
  public required string UserId { get; init; }

  /// <summary>
  /// Login name (username) of the channel.
  /// </summary>
  public required string Login { get; init; }
}
