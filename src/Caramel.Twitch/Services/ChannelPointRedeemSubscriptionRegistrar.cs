namespace Caramel.Twitch.Services;

internal sealed class ChannelPointRedeemSubscriptionRegistrar(
  IEventSubSubscriptionClient subscriptionClient) : IEventSubSubscriptionRegistrar
{
  public async Task RegisterAsync(EventSubSubscriptionRegistrationContext context, CancellationToken cancellationToken)
  {
    foreach (var channelUserId in context.ChannelUserIds)
    {
      await subscriptionClient.CreateSubscriptionAsync(
        context.HttpClient,
        context.SessionId,
        "channel.channel_points_custom_reward_redemption.add",
        "1",
        new Dictionary<string, string>
        {
          { "broadcaster_user_id", channelUserId },
        },
        cancellationToken);
    }
  }
}
