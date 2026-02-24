namespace Caramel.Core.Twitch;

/// <summary>
/// Represents an incoming Twitch channel point custom reward redemption published via Redis pub/sub.
/// Used as the shared contract between Caramel.Twitch (publisher) and Caramel.API (subscriber).
/// </summary>
public sealed record TwitchChannelPointRedeem
{
  /// <summary>
  /// Unique ID of the redemption.
  /// </summary>
  public required string RedemptionId { get; init; }

  /// <summary>
  /// Login name of the channel (broadcaster).
  /// </summary>
  public required string BroadcasterLogin { get; init; }

  /// <summary>
  /// Twitch user ID of the channel (broadcaster).
  /// </summary>
  public required string BroadcasterUserId { get; init; }

  /// <summary>
  /// Twitch user ID of the viewer who redeemed.
  /// </summary>
  public required string RedeemerUserId { get; init; }

  /// <summary>
  /// Login name of the viewer who redeemed.
  /// </summary>
  public required string RedeemerLogin { get; init; }

  /// <summary>
  /// Display name of the viewer who redeemed.
  /// </summary>
  public required string RedeemerDisplayName { get; init; }

  /// <summary>
  /// Unique ID of the custom reward that was redeemed.
  /// </summary>
  public required string RewardId { get; init; }

  /// <summary>
  /// Title of the custom reward.
  /// </summary>
  public required string RewardTitle { get; init; }

  /// <summary>
  /// Channel point cost of the reward.
  /// </summary>
  public required int RewardCost { get; init; }

  /// <summary>
  /// Optional text input provided by the viewer, or empty string if not required.
  /// </summary>
  public required string UserInput { get; init; }

  /// <summary>
  /// UTC timestamp when the reward was redeemed.
  /// </summary>
  public required DateTimeOffset RedeemedAt { get; init; }
}
