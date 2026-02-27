namespace Caramel.Twitch.Services;

internal sealed class ChannelChatMessageSubscriptionRegistrar(
  IEventSubSubscriptionClient subscriptionClient,
  ILogger<ChannelChatMessageSubscriptionRegistrar> logger) : IEventSubSubscriptionRegistrar
{
  public async Task RegisterAsync(EventSubSubscriptionRegistrationContext context, CancellationToken cancellationToken)
  {
    // Chat messages require bot token
    var accessToken = context.BotAccessToken;
    
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
          "channel.chat.message",
          "1",
          new Dictionary<string, string>
          {
            { "broadcaster_user_id", channelUserId },
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
        ChannelChatMessageSubscriptionRegistrarLogs.RegistrationError(logger, channelUserId, ex.Message);
      }
    }
  }
}

internal static partial class ChannelChatMessageSubscriptionRegistrarLogs
{
  [LoggerMessage(Level = LogLevel.Error, Message = "Failed to register channel chat message subscription for channel {ChannelUserId}: {Error}")]
  public static partial void RegistrationError(ILogger logger, string channelUserId, string error);
}
