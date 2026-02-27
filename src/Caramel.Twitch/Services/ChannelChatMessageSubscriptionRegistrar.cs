namespace Caramel.Twitch.Services;

internal sealed class ChannelChatMessageSubscriptionRegistrar(
  IEventSubSubscriptionClient subscriptionClient) : IEventSubSubscriptionRegistrar
{
  public async Task RegisterAsync(EventSubSubscriptionRegistrationContext context, CancellationToken cancellationToken)
  {
    foreach (var channelUserId in context.ChannelUserIds)
    {
      await subscriptionClient.CreateSubscriptionAsync(
        context.HttpClient,
        context.SessionId,
        "channel.chat.message",
        "1",
        new Dictionary<string, string>
        {
          { "broadcaster_user_id", channelUserId },
          { "user_id", context.BotUserId },
        },
        cancellationToken);
    }
  }
}
