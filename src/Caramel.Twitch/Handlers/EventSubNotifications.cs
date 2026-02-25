namespace Caramel.Twitch.Handlers;

public sealed record ChannelChatMessageReceived(
  string BroadcasterUserId,
  string BroadcasterUserLogin,
  string ChatterUserId,
  string ChatterUserLogin,
  string ChatterDisplayName,
  string MessageId,
  string MessageText,
  string Color) : INotification;

public sealed record UserWhisperMessageReceived(
  string FromUserId,
  string FromUserLogin,
  string MessageText) : INotification;

public sealed record ChannelPointsCustomRewardRedeemed(
  string RedemptionId,
  string BroadcasterUserId,
  string BroadcasterLogin,
  string RedeemerUserId,
  string RedeemerLogin,
  string RedeemerDisplayName,
  string RewardId,
  string RewardTitle,
  int RewardCost,
  string UserInput,
  DateTimeOffset RedeemedAt) : INotification;
