namespace Caramel.Database.Twitch.Events;

/// <summary>
/// Event fired when Twitch bot and channel configuration is first created.
/// </summary>
public sealed record TwitchSetupCreatedEvent : BaseEvent
{
  /// <summary>
  /// Numeric Twitch user ID of the bot.
  /// </summary>
  public required string BotUserId { get; init; }

  /// <summary>
  /// Login name of the bot.
  /// </summary>
  public required string BotLogin { get; init; }

  /// <summary>
  /// List of channels to configure (each with UserId and Login).
  /// </summary>
  public required List<TwitchChannelData> Channels { get; init; }
}

/// <summary>
/// Event fired when Twitch configuration is updated.
/// </summary>
public sealed record TwitchSetupUpdatedEvent : BaseEvent
{
  /// <summary>
  /// Updated bot user ID.
  /// </summary>
  public required string BotUserId { get; init; }

  /// <summary>
  /// Updated bot login name.
  /// </summary>
  public required string BotLogin { get; init; }

  /// <summary>
  /// Updated channel list.
  /// </summary>
  public required List<TwitchChannelData> Channels { get; init; }
}

/// <summary>
/// Event fired when bot account OAuth tokens are obtained or updated.
/// </summary>
public sealed record TwitchBotTokensUpdatedEvent : BaseEvent
{
  /// <summary>
  /// Numeric Twitch user ID of the bot account.
  /// </summary>
  public required string BotUserId { get; init; }

  /// <summary>
  /// Login name of the bot account.
  /// </summary>
  public required string BotLogin { get; init; }

  /// <summary>
  /// Encrypted OAuth access token.
  /// </summary>
  public required string AccessToken { get; init; }

  /// <summary>
  /// Encrypted OAuth refresh token, if available.
  /// </summary>
  public string? RefreshToken { get; init; }

  /// <summary>
  /// UTC timestamp when the access token expires.
  /// </summary>
  public required DateTime ExpiresAt { get; init; }
}

/// <summary>
/// Event fired when broadcaster account OAuth tokens are obtained or updated.
/// </summary>
public sealed record TwitchBroadcasterTokensUpdatedEvent : BaseEvent
{
  /// <summary>
  /// Numeric Twitch user ID of the broadcaster account.
  /// </summary>
  public required string BroadcasterUserId { get; init; }

  /// <summary>
  /// Login name of the broadcaster account.
  /// </summary>
  public required string BroadcasterLogin { get; init; }

  /// <summary>
  /// Encrypted OAuth access token.
  /// </summary>
  public required string AccessToken { get; init; }

  /// <summary>
  /// Encrypted OAuth refresh token, if available.
  /// </summary>
  public string? RefreshToken { get; init; }

  /// <summary>
  /// UTC timestamp when the access token expires.
  /// </summary>
  public required DateTime ExpiresAt { get; init; }
}

/// <summary>
/// Channel data structure for serialization in events.
/// </summary>
public sealed record TwitchChannelData
{
  /// <summary>
  /// Numeric Twitch user ID of the channel (broadcaster).
  /// </summary>
  public required string UserId { get; init; }

  /// <summary>
  /// Login name of the channel.
  /// </summary>
  public required string Login { get; init; }
}
