using Caramel.Twitch.Extensions;

namespace Caramel.Twitch.Handlers;

public sealed class ChannelPointRedeemHandler(
  ICaramelServiceClient caramelServiceClient,
  ITwitchChatBroadcaster broadcaster,
  ITwitchChatClient chatClient,
  TwitchConfig twitchConfig,
  ILogger<ChannelPointRedeemHandler> logger) : INotificationHandler<ChannelPointsCustomRewardRedeemed>
{
  private const int MaxMessageLength = 500;

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
      return;
    }

    if (string.IsNullOrWhiteSpace(result.Value))
    {
      return;
    }

    var message = FormatChatMessage(notification.RedeemerLogin, result.Value);
    var sendResult = await chatClient.SendChatMessageAsync(message, cancellationToken);
    if (sendResult.IsFailed)
    {
      ChannelPointRedeemLogs.MessageTheAiResponseSendFailed(logger, notification.RedeemerLogin, sendResult.Errors[0].Message);
    }
    else
    {
      ChannelPointRedeemLogs.MessageTheAiResponseSent(logger, notification.RedeemerLogin);
    }
  }

  internal static string FormatChatMessage(string username, string response)
  {
    var prefix = $"@{username} ";
    var maxResponseLength = MaxMessageLength - prefix.Length;
    var truncatedResponse = response.Length > maxResponseLength
      ? response[..maxResponseLength]
      : response;
    return prefix + truncatedResponse;
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

  [LoggerMessage(Level = LogLevel.Information, Message = "Message The AI response sent to chat for {Username}")]
  public static partial void MessageTheAiResponseSent(ILogger logger, string username);

  [LoggerMessage(Level = LogLevel.Warning, Message = "Failed to send Message The AI response to chat for {Username}: {Error}")]
  public static partial void MessageTheAiResponseSendFailed(ILogger logger, string username, string error);
}
