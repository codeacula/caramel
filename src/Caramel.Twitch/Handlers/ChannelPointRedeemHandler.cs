namespace Caramel.Twitch.Handlers;

public sealed class ChannelPointRedeemHandler(
  ITwitchChatBroadcaster broadcaster,
  ILogger<ChannelPointRedeemHandler> logger)
{
  public async Task HandleAsync(
    string redemptionId,
    string broadcasterUserId,
    string broadcasterLogin,
    string redeemerUserId,
    string redeemerLogin,
    string redeemerDisplayName,
    string rewardId,
    string rewardTitle,
    int rewardCost,
    string userInput,
    DateTimeOffset redeemedAt,
    CancellationToken cancellationToken = default)
  {
    try
    {
      ChannelPointRedeemLogs.RedeemReceived(logger, redeemerLogin, rewardTitle);

      await broadcaster.PublishRedeemAsync(
        redemptionId,
        broadcasterUserId,
        broadcasterLogin,
        redeemerUserId,
        redeemerLogin,
        redeemerDisplayName,
        rewardId,
        rewardTitle,
        rewardCost,
        userInput,
        redeemedAt,
        cancellationToken);
    }
    catch (Exception ex)
    {
      ChannelPointRedeemLogs.RedeemHandlerFailed(logger, redeemerLogin, ex.Message);
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
