namespace Caramel.Core.Twitch;

/// <summary>
/// Represents an incoming Twitch chat message published via Redis pub/sub.
/// Used as the shared contract between Caramel.Twitch (publisher) and Caramel.API (subscriber).
/// </summary>
public sealed record TwitchChatMessage
{
  /// <summary>
  /// Twitch message ID (unique per message).
  /// </summary>
  public required string MessageId { get; init; }

  /// <summary>
  /// Login name of the channel (broadcaster).
  /// </summary>
  public required string BroadcasterLogin { get; init; }

  /// <summary>
  /// Twitch user ID of the channel (broadcaster).
  /// </summary>
  public required string BroadcasterUserId { get; init; }

  /// <summary>
  /// Login name of the chatter who sent the message.
  /// </summary>
  public required string ChatterLogin { get; init; }

  /// <summary>
  /// Display name of the chatter (may differ in casing from login).
  /// </summary>
  public required string ChatterDisplayName { get; init; }

  /// <summary>
  /// Twitch user ID of the chatter.
  /// </summary>
  public required string ChatterUserId { get; init; }

  /// <summary>
  /// The plain-text content of the message.
  /// </summary>
  public required string MessageText { get; init; }

  /// <summary>
  /// Hex color string for the chatter's username (e.g. "#FF0000"), or empty if unset.
  /// </summary>
  public required string Color { get; init; }

  /// <summary>
  /// UTC timestamp when the message was received.
  /// </summary>
  public required DateTimeOffset Timestamp { get; init; }

  /// <summary>
  /// Redis pub/sub channel name used to publish and subscribe to Twitch chat messages.
  /// </summary>
  public const string RedisChannel = "caramel:twitch:chat";
}
