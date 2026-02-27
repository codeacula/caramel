namespace Caramel.Twitch.Services;

internal sealed class UserWhisperSubscriptionRegistrar(
  IEventSubSubscriptionClient subscriptionClient) : IEventSubSubscriptionRegistrar
{
  public async Task RegisterAsync(EventSubSubscriptionRegistrationContext context, CancellationToken cancellationToken)
  {
    await subscriptionClient.CreateSubscriptionAsync(
      context.HttpClient,
      context.SessionId,
      "user.whisper.message",
      "1",
      new Dictionary<string, string>
      {
        { "user_id", context.BotUserId },
      },
      cancellationToken);
  }
}
