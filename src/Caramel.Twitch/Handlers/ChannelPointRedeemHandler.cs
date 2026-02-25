namespace Caramel.Twitch.Handlers;

public sealed class ChannelPointRedeemHandler(
  ITwitchChatBroadcaster broadcaster,
  ILogger<ChannelPointRedeemHandler> logger) : INotificationHandler<ChannelPointsCustomRewardRedeemed>
{
  public async Task Handle(ChannelPointsCustomRewardRedeemed notification, CancellationToken cancellationToken)
  {
    try
    {
      ChannelPointRedeemLogs.RedeemReceived(logger, notification.RedeemerLogin, notification.RewardTitle);

      await broadcaster.PublishRedeemAsync(
        notification.RedemptionId,
        notification.BroadcasterUserId,
        notification.BroadcasterLogin,
        notification.RedeemerUserId,
        notification.RedeemerLogin,
        notification.RedeemerDisplayName,
        notification.RewardId,
        notification.RewardTitle,
        notification.RewardCost,
        notification.UserInput,
        notification.RedeemedAt,
        cancellationToken);
    }
    catch (Exception ex)
    {
      ChannelPointRedeemLogs.RedeemHandlerFailed(logger, notification.RedeemerLogin, ex.Message);
    }
  }
}

/// <summary>
/// Structured logging for <see cref="ChannelPointRedeemHandler"/>.
/// </summary>
internal static partial class ChannelPointRedeemLogs
{
  [LoggerMessage(Level = LogLevel.Debug, Message = "Channel point redeem received: {Username} redeemed '{RewardTitle}'")]
  public static partial void RedeemReceived(ILogger logger, string username, string rewardTitle);

  [LoggerMessage(Level = LogLevel.Error, Message = "Channel point redeem handler failed for {Username}: {Error}")]
  public static partial void RedeemHandlerFailed(ILogger logger, string username, string error);
}
