namespace Caramel.Twitch.Services;

/// <summary>
/// Identifies which Twitch account (bot or broadcaster) is required for an EventSub subscription.
/// </summary>
internal enum EventSubSubscriptionAccountType
{
  /// <summary>
  /// Subscription requires bot account token.
  /// Examples: channel.chat.message, user.whisper.message
  /// </summary>
  Bot = 0,

  /// <summary>
  /// Subscription requires broadcaster account token.
  /// Examples: channel.channel_points_custom_reward_redemption.add
  /// </summary>
  Broadcaster = 1,
}

/// <summary>
/// Maps EventSub subscription event types to their required account type.
/// </summary>
internal static class EventSubSubscriptionAccountTypeMap
{
  private static readonly IReadOnlyDictionary<string, EventSubSubscriptionAccountType> SubscriptionTypeMap = new Dictionary<string, EventSubSubscriptionAccountType>
  {
    { "channel.chat.message", EventSubSubscriptionAccountType.Bot },
    { "user.whisper.message", EventSubSubscriptionAccountType.Bot },
    { "channel.channel_points_custom_reward_redemption.add", EventSubSubscriptionAccountType.Broadcaster },
  };

  /// <summary>
  /// Determines which account type is required for the given subscription type.
  /// </summary>
  internal static EventSubSubscriptionAccountType GetAccountType(string subscriptionType)
  {
    return SubscriptionTypeMap.TryGetValue(subscriptionType, out var accountType)
      ? accountType
      : EventSubSubscriptionAccountType.Bot; // Default to bot for safety
  }
}
