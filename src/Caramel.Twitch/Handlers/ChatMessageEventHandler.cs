using Caramel.Twitch.Extensions;
using Caramel.Twitch.Services;

namespace Caramel.Twitch.Handlers;

/// <summary>
/// Handles incoming chat messages from Twitch EventSub channel.chat.message events.
/// </summary>
public sealed class ChatMessageEventHandler(
  ICaramelServiceClient caramelServiceClient,
  IPersonCache personCache,
  ITwitchChatBroadcaster broadcaster,
  ITwitchSetupState setupState,
  ILogger<ChatMessageEventHandler> logger)
{
  private const string BotCommandPrefix = "!caramel";
  private const string MentionPrefix = "@caramel";

  /// <summary>
  /// Processes an incoming chat message from a Twitch channel.
  /// Broadcasts the message to the Redis pub/sub channel for UI display before
  /// applying any bot-directed filtering.
  /// </summary>
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
        CaramelTwitchLogs.AccessCheckFailed(logger, chatterLogin, accessResult.Errors.First().Message);
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

      // Try to parse as quick command (e.g., !caramel todo buy milk)
      var commandResult = TryParseQuickCommand(messageText, out var commandType, out var commandContent);
      if (commandResult)
      {
        await HandleQuickCommandAsync(commandType, commandContent, platformId, cancellationToken);
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

  private bool TryParseQuickCommand(string messageText, out string commandType, out string commandContent)
  {
    commandType = string.Empty;
    commandContent = string.Empty;

    var normalized = messageText.AsSpan().Trim();

    if (normalized.StartsWith(BotCommandPrefix.AsSpan(), StringComparison.OrdinalIgnoreCase))
    {
      normalized = normalized[BotCommandPrefix.Length..].Trim();
    }
    else if (normalized.Contains(MentionPrefix.AsSpan(), StringComparison.OrdinalIgnoreCase))
    {
      var idx = normalized.IndexOf(MentionPrefix.AsSpan(), StringComparison.OrdinalIgnoreCase);
      normalized = normalized[(idx + MentionPrefix.Length)..].Trim();
    }

    if (normalized.StartsWith("todo", StringComparison.OrdinalIgnoreCase) || normalized.StartsWith("task", StringComparison.OrdinalIgnoreCase))
    {
      var spaceIdx = normalized.IndexOf(' ');
      if (spaceIdx > 0)
      {
        commandType = "todo";
        commandContent = normalized[(spaceIdx + 1)..].Trim().ToString();
        return true;
      }
    }
    else if (normalized.StartsWith("remind", StringComparison.OrdinalIgnoreCase))
    {
      var spaceIdx = normalized.IndexOf(' ');
      if (spaceIdx > 0)
      {
        commandType = "remind";
        commandContent = normalized[(spaceIdx + 1)..].Trim().ToString();
        return true;
      }
    }

    return false;
  }

  private async Task HandleQuickCommandAsync(
    string commandType,
    string commandContent,
    PlatformId platformId,
    CancellationToken cancellationToken)
  {
    if (commandType == "todo")
    {
      var request = new CreateToDoRequest
      {
        PlatformId = platformId,
        Title = "From Twitch",
        Description = commandContent
      };

      var result = await caramelServiceClient.CreateToDoAsync(request, cancellationToken);
      if (result.IsSuccess)
      {
        CaramelTwitchLogs.ToDoCreatedViaChat(logger, platformId.Username, commandContent);
      }
      else
      {
        CaramelTwitchLogs.ToDoCreationFailed(logger, platformId.Username, result.Errors.First().Message);
      }
    }
    else if (commandType == "remind")
    {
      var request = new CreateReminderRequest
      {
        PlatformId = platformId,
        Message = commandContent,
        ReminderTime = DateTime.UtcNow.AddMinutes(1).ToString("O")
      };

      var result = await caramelServiceClient.CreateReminderAsync(request, cancellationToken);
      if (result.IsSuccess)
      {
        CaramelTwitchLogs.ReminderCreatedViaChat(logger, platformId.Username, commandContent);
      }
      else
      {
        CaramelTwitchLogs.ReminderCreationFailed(logger, platformId.Username, result.Errors.First().Message);
      }
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
      CaramelTwitchLogs.MessageProcessingFailed(logger, platformId.Username, result.Errors.First().Message);
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

  [LoggerMessage(Level = LogLevel.Information, Message = "Todo created via chat for {Username}: {Description}")]
  public static partial void ToDoCreatedViaChat(ILogger logger, string username, string description);

  [LoggerMessage(Level = LogLevel.Warning, Message = "Todo creation failed for {Username}: {Error}")]
  public static partial void ToDoCreationFailed(ILogger logger, string username, string error);

  [LoggerMessage(Level = LogLevel.Information, Message = "Reminder created via chat for {Username}: {Message}")]
  public static partial void ReminderCreatedViaChat(ILogger logger, string username, string message);

  [LoggerMessage(Level = LogLevel.Warning, Message = "Reminder creation failed for {Username}: {Error}")]
  public static partial void ReminderCreationFailed(ILogger logger, string username, string error);

  [LoggerMessage(Level = LogLevel.Information, Message = "Message processed for {Username}")]
  public static partial void MessageProcessed(ILogger logger, string username);

  [LoggerMessage(Level = LogLevel.Warning, Message = "Message processing failed for {Username}: {Error}")]
  public static partial void MessageProcessingFailed(ILogger logger, string username, string error);

  [LoggerMessage(Level = LogLevel.Error, Message = "Chat message handler failed for {Username}: {Error}")]
  public static partial void ChatMessageHandlerFailed(ILogger logger, string username, string error);
}
