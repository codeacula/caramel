using Caramel.Twitch.Extensions;

namespace Caramel.Twitch.Handlers;

public sealed class ChatMessageHandler(
  ICaramelServiceClient caramelServiceClient,
  IPersonCache personCache,
  ITwitchChatBroadcaster broadcaster,
  ITwitchSetupState setupState,
  ILogger<ChatMessageHandler> logger)
{
  private const string BotCommandPrefix = "!caramel";
  private const string MentionPrefix = "@caramel";

  public async Task HandleAsync(
    string broadcasterUserId,
    string broadcasterLogin,
    string chatterUserId,
    string chatterLogin,
    string chatterDisplayName,
    string messageId,
    string messageText,
    string color,
    CancellationToken cancellationToken = default)
  {
    try
    {
      CaramelTwitchLogs.ChatMessageReceived(logger, chatterLogin);

      // Broadcast every message to Redis so the UI can display it regardless of
      // whether it is directed at the bot.
      await broadcaster.PublishAsync(
        messageId,
        broadcasterUserId,
        broadcasterLogin,
        chatterUserId,
        chatterLogin,
        chatterDisplayName,
        messageText,
        color,
        cancellationToken);

      // Ignore messages from the bot itself
      var setup = setupState.Current;
      if (setup is not null && chatterUserId == setup.BotUserId)
      {
        return;
      }

      var platformId = TwitchPlatformExtension.GetTwitchPlatformId(chatterLogin, chatterUserId);

      // Check access via cache (fast path)
      var accessResult = await personCache.GetAccessAsync(platformId);
      if (accessResult.IsFailed)
      {
        CaramelTwitchLogs.AccessCheckFailed(logger, chatterLogin, accessResult.Errors[0].Message);
        return;
      }

      if (accessResult.Value is false)
      {
        CaramelTwitchLogs.AccessDenied(logger, chatterLogin);
        return;
      }

      // Check if message is directed at the bot
      var isDirectedAtBot = messageText.StartsWith(BotCommandPrefix, StringComparison.OrdinalIgnoreCase)
                            || messageText.Contains(MentionPrefix, StringComparison.OrdinalIgnoreCase);

      if (!isDirectedAtBot)
      {
        return;
      }

      // Otherwise, send as a general message to the AI
      await HandleGeneralMessageAsync(messageText, platformId, cancellationToken);
    }
    catch (Exception ex)
    {
      CaramelTwitchLogs.ChatMessageHandlerFailed(logger, chatterLogin, ex.Message);
    }
  }

  private async Task HandleGeneralMessageAsync(
    string messageText,
    PlatformId platformId,
    CancellationToken cancellationToken)
  {
    var request = new ProcessMessageRequest
    {
      PlatformUserId = platformId.PlatformUserId,
      Platform = platformId.Platform,
      Username = platformId.Username,
      Content = messageText
    };

    var result = await caramelServiceClient.SendMessageAsync(request, cancellationToken);
    if (result.IsFailed)
    {
      CaramelTwitchLogs.MessageProcessingFailed(logger, platformId.Username, result.Errors[0].Message);
    }
  }
}

/// <summary>
/// Structured logging for Caramel.Twitch chat message handler.
/// </summary>
public static partial class CaramelTwitchLogs
{
  [LoggerMessage(Level = LogLevel.Debug, Message = "Chat message handler invoked for user {Username}")]
  public static partial void ChatMessageReceived(ILogger logger, string username);

  [LoggerMessage(Level = LogLevel.Warning, Message = "Access check failed for {Username}: {Error}")]
  public static partial void AccessCheckFailed(ILogger logger, string username, string error);

  [LoggerMessage(Level = LogLevel.Information, Message = "Access denied for Twitch user {Username}")]
  public static partial void AccessDenied(ILogger logger, string username);

  [LoggerMessage(Level = LogLevel.Warning, Message = "Message processing failed for {Username}: {Error}")]
  public static partial void MessageProcessingFailed(ILogger logger, string username, string error);

  [LoggerMessage(Level = LogLevel.Error, Message = "Chat message handler failed for {Username}: {Error}")]
  public static partial void ChatMessageHandlerFailed(ILogger logger, string username, string error);
}
