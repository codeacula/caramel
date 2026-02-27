namespace Caramel.Twitch.Services;

internal sealed class ChannelPointRedeemSubscriptionRegistrar(
  IEventSubSubscriptionClient subscriptionClient,
  ILogger<ChannelPointRedeemSubscriptionRegistrar> logger) : IEventSubSubscriptionRegistrar
{
  public async Task RegisterAsync(EventSubSubscriptionRegistrationContext context, CancellationToken cancellationToken)
  {
    // Channel points redemptions require broadcaster token
    var accessToken = context.BroadcasterAccessToken;

    // Skip if broadcaster token not available
    if (string.IsNullOrEmpty(accessToken))
    {
      ChannelPointRedeemSubscriptionRegistrarLogs.SkippingNoBroadcasterToken(logger);
      return;
    }

    foreach (var channelUserId in context.ChannelUserIds)
    {
      try
      {
        var httpClient = context.HttpClient;
        httpClient.DefaultRequestHeaders.Remove("Authorization");
        httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {accessToken}");

        await subscriptionClient.CreateSubscriptionAsync(
          httpClient,
          context.SessionId,
          "channel.channel_points_custom_reward_redemption.add",
          "1",
          new Dictionary<string, string>
          {
            { "broadcaster_user_id", channelUserId },
          },
          cancellationToken);
      }
      catch (OperationCanceledException)
      {
        throw;
      }
      catch (Exception ex)
      {
        ChannelPointRedeemSubscriptionRegistrarLogs.RegistrationError(logger, channelUserId, ex.Message);
      }
    }
  }
}

internal static partial class ChannelPointRedeemSubscriptionRegistrarLogs
{
  [LoggerMessage(Level = LogLevel.Warning, Message = "Skipping channel points redemption subscription registration: broadcaster token not available")]
  public static partial void SkippingNoBroadcasterToken(ILogger logger);

  [LoggerMessage(Level = LogLevel.Error, Message = "Failed to register channel points redemption subscription for channel {ChannelUserId}: {Error}")]
  public static partial void RegistrationError(ILogger logger, string channelUserId, string error);
}
