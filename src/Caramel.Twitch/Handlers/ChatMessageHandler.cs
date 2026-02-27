using Caramel.Twitch.Extensions;

namespace Caramel.Twitch.Handlers;

public sealed class ChatMessageHandler(
  ICaramelServiceClient caramelServiceClient,
  IPersonCache personCache,
  ITwitchChatBroadcaster broadcaster,
  ITwitchSetupState setupState,
  ITwitchChatClient chatClient,
  ILogger<ChatMessageHandler> logger) : INotificationHandler<ChannelChatMessageReceived>
{
  private const string BotCommandPrefix = "!caramel";
  private const string MentionPrefix = "@caramel";
  private const int MaxMessageLength = 500;

  public async Task Handle(ChannelChatMessageReceived notification, CancellationToken cancellationToken)
  {
    try
    {
      CaramelTwitchLogs.ChatMessageReceived(logger, notification.ChatterUserLogin);

      // Broadcast every message to Redis so the UI can display it regardless of
      // whether it is directed at the bot.
      await broadcaster.PublishAsync(
        notification.MessageId,
        notification.BroadcasterUserId,
        notification.BroadcasterUserLogin,
        notification.ChatterUserId,
        notification.ChatterUserLogin,
        notification.ChatterDisplayName,
        notification.MessageText,
        notification.Color,
        cancellationToken);

      // Ignore messages from the bot itself
      var setup = setupState.Current;
      if (setup is not null && notification.ChatterUserId == setup.BotUserId)
      {
        return;
      }

      var platformId = TwitchPlatformExtension.GetTwitchPlatformId(notification.ChatterUserLogin, notification.ChatterUserId);

      // Check access via cache (fast path)
      var accessResult = await personCache.GetAccessAsync(platformId);
      if (accessResult.IsFailed)
      {
        CaramelTwitchLogs.AccessCheckFailed(logger, notification.ChatterUserLogin, accessResult.Errors[0].Message);
        return;
      }

      if (accessResult.Value is false)
      {
        CaramelTwitchLogs.AccessDenied(logger, notification.ChatterUserLogin);
        return;
      }

      // Check if message is directed at the bot
      var isDirectedAtBot = notification.MessageText.StartsWith(BotCommandPrefix, StringComparison.OrdinalIgnoreCase)
                            || notification.MessageText.Contains(MentionPrefix, StringComparison.OrdinalIgnoreCase);

      if (!isDirectedAtBot)
      {
        return;
      }

      // Otherwise, send as a general message to the AI
      await HandleGeneralMessageAsync(notification.MessageText, notification.ChatterUserLogin, platformId, cancellationToken);
    }
    catch (OperationCanceledException)
    {
      throw;
    }
    catch (InvalidOperationException ex)
    {
      CaramelTwitchLogs.ChatMessageHandlerFailed(logger, notification.ChatterUserLogin, $"Invalid state: {ex.Message}");
    }
    catch (Exception ex)
    {
      CaramelTwitchLogs.ChatMessageHandlerFailed(logger, notification.ChatterUserLogin, ex.Message);
    }
  }

  private async Task HandleGeneralMessageAsync(
    string messageText,
    string chatterLogin,
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
      return;
    }

    if (string.IsNullOrWhiteSpace(result.Value))
    {
      return;
    }

    var message = FormatChatMessage(chatterLogin, result.Value);
    var sendResult = await chatClient.SendChatMessageAsync(message, cancellationToken);
    if (sendResult.IsFailed)
    {
      CaramelTwitchLogs.ChatResponseSendFailed(logger, chatterLogin, sendResult.Errors[0].Message);
    }
    else
    {
      CaramelTwitchLogs.ChatResponseSent(logger, chatterLogin);
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

  [LoggerMessage(Level = LogLevel.Information, Message = "Chat response sent for {Username}")]
  public static partial void ChatResponseSent(ILogger logger, string username);

  [LoggerMessage(Level = LogLevel.Warning, Message = "Failed to send chat response for {Username}: {Error}")]
  public static partial void ChatResponseSendFailed(ILogger logger, string username, string error);
}
