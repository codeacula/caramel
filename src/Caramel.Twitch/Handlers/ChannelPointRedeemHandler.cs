using Caramel.Twitch.Extensions;

namespace Caramel.Twitch.Handlers;

public sealed class ChannelPointRedeemHandler(
  ICaramelServiceClient caramelServiceClient,
  ITwitchChatBroadcaster broadcaster,
  TwitchConfig twitchConfig,
  ILogger<ChannelPointRedeemHandler> logger) : INotificationHandler<ChannelPointsCustomRewardRedeemed>
{
  private readonly Guid? _messageTheAiRewardId = Guid.TryParse(twitchConfig.MessageTheAiRewardId, out var rewardId)
    ? rewardId
    : null;

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

      if (IsMessageTheAiRedeem(notification.RewardId))
      {
        await HandleMessageTheAiRedeemAsync(notification, cancellationToken);
      }
    }
    catch (Exception ex)
    {
      ChannelPointRedeemLogs.RedeemHandlerFailed(logger, notification.RedeemerLogin, ex.Message);
    }
  }

  private async Task HandleMessageTheAiRedeemAsync(
    ChannelPointsCustomRewardRedeemed notification,
    CancellationToken cancellationToken)
  {
    if (string.IsNullOrWhiteSpace(notification.UserInput))
    {
      ChannelPointRedeemLogs.MessageTheAiInputMissing(logger, notification.RedeemerLogin);
      return;
    }

    var platformId = TwitchPlatformExtension.GetTwitchPlatformId(notification.RedeemerLogin, notification.RedeemerUserId);

    var request = new AskTheOrbRequest
    {
      Platform = platformId.Platform,
      PlatformUserId = platformId.PlatformUserId,
      Username = platformId.Username,
      Content = notification.UserInput
    };

    var result = await caramelServiceClient.AskTheOrbAsync(request, cancellationToken);
    if (result.IsFailed)
    {
      ChannelPointRedeemLogs.MessageTheAiRequestFailed(logger, notification.RedeemerLogin, result.Errors[0].Message);
    }
  }

  private bool IsMessageTheAiRedeem(string rewardId)
  {
    return _messageTheAiRewardId.HasValue
      && Guid.TryParse(rewardId, out var redeemedRewardId)
      && redeemedRewardId == _messageTheAiRewardId.Value;
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

  [LoggerMessage(Level = LogLevel.Debug, Message = "Message The AI redeem from {Username} had no input. Skipping AskTheOrb call.")]
  public static partial void MessageTheAiInputMissing(ILogger logger, string username);

  [LoggerMessage(Level = LogLevel.Warning, Message = "Message The AI request failed for {Username}: {Error}")]
  public static partial void MessageTheAiRequestFailed(ILogger logger, string username, string error);
}
