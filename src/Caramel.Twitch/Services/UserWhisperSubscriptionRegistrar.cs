namespace Caramel.Twitch.Services;

internal sealed class UserWhisperSubscriptionRegistrar(
  IEventSubSubscriptionClient subscriptionClient,
  ILogger<UserWhisperSubscriptionRegistrar> logger) : IEventSubSubscriptionRegistrar
{
  public async Task RegisterAsync(EventSubSubscriptionRegistrationContext context, CancellationToken cancellationToken)
  {
    try
    {
      // Whispers require bot token
      var accessToken = context.BotAccessToken;

      var httpClient = context.HttpClient;
      httpClient.DefaultRequestHeaders.Remove("Authorization");
      httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {accessToken}");

      await subscriptionClient.CreateSubscriptionAsync(
        httpClient,
        context.SessionId,
        "user.whisper.message",
        "1",
        new Dictionary<string, string>
        {
          { "user_id", context.BotUserId },
        },
        cancellationToken);
    }
    catch (OperationCanceledException)
    {
      throw;
    }
    catch (Exception ex)
    {
      UserWhisperSubscriptionRegistrarLogs.RegistrationError(logger, ex.Message);
    }
  }
}

internal static partial class UserWhisperSubscriptionRegistrarLogs
{
  [LoggerMessage(Level = LogLevel.Error, Message = "Failed to register user whisper subscription: {Error}")]
  public static partial void RegistrationError(ILogger logger, string error);
}
