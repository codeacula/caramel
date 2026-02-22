namespace Caramel.Domain.Twitch;

/// <summary>
/// Represents a Twitch channel's bot and channel configuration.
/// Contains the numeric Twitch user IDs and login names for the bot and channel(s).
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
