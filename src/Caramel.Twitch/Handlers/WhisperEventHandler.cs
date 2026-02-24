using Caramel.Twitch.Extensions;

namespace Caramel.Twitch.Handlers;

/// <summary>
/// Handles incoming whispers (direct messages) from Twitch EventSub user.whisper.message events.
/// </summary>
/// <param name="caramelServiceClient"></param>
/// <param name="personCache"></param>
/// <param name="logger"></param>
public sealed class WhisperEventHandler(
  ICaramelServiceClient caramelServiceClient,
  IPersonCache personCache,
  ILogger<WhisperEventHandler> logger)
{
  /// <summary>
  /// Processes an incoming whisper from a Twitch user.
  /// </summary>
  /// <param name="fromUserId"></param>
  /// <param name="fromUserLogin"></param>
  /// <param name="messageText"></param>
  /// <param name="cancellationToken"></param>
  public async Task HandleAsync(
    string fromUserId,
    string fromUserLogin,
    string messageText,
    CancellationToken cancellationToken = default)
  {
    try
    {
      var platformId = TwitchPlatformExtension.GetTwitchPlatformId(fromUserLogin, fromUserId);

      // Check access via cache (fast path)
      var accessResult = await personCache.GetAccessAsync(platformId);
      if (accessResult.IsFailed)
      {
        CaramelTwitchWhisperLogs.AccessCheckFailed(logger, fromUserLogin, accessResult.Errors[0].Message);
        return;
      }

      if (accessResult.Value is false)
      {
        CaramelTwitchWhisperLogs.AccessDenied(logger, fromUserLogin);
        return;
      }

      // Send whisper as a message to the AI
      var request = new ProcessMessageRequest
      {
        Platform = Platform.Twitch,
        PlatformUserId = platformId.PlatformUserId,
        Username = platformId.Username,
        Content = messageText
      };

      var result = await caramelServiceClient.SendMessageAsync(request, cancellationToken);
      if (result.IsSuccess)
      {
        CaramelTwitchWhisperLogs.WhisperProcessed(logger, fromUserLogin);
      }
      else
      {
        CaramelTwitchWhisperLogs.WhisperProcessingFailed(logger, fromUserLogin, result.Errors[0].Message);
      }
    }
    catch (Exception ex)
    {
      CaramelTwitchWhisperLogs.WhisperHandlerFailed(logger, fromUserLogin, ex.Message);
    }
  }
}

/// <summary>
/// Structured logging for Caramel.Twitch whisper handler.
/// </summary>
public static partial class CaramelTwitchWhisperLogs
{
  [LoggerMessage(Level = LogLevel.Debug, Message = "Whisper handler invoked for user {Username}")]
  public static partial void WhisperReceived(ILogger logger, string username);

  [LoggerMessage(Level = LogLevel.Warning, Message = "Access check failed for {Username}: {Error}")]
  public static partial void AccessCheckFailed(ILogger logger, string username, string error);

  [LoggerMessage(Level = LogLevel.Information, Message = "Access denied for Twitch user {Username}")]
  public static partial void AccessDenied(ILogger logger, string username);

  [LoggerMessage(Level = LogLevel.Information, Message = "Whisper processed for {Username}")]
  public static partial void WhisperProcessed(ILogger logger, string username);

  [LoggerMessage(Level = LogLevel.Warning, Message = "Whisper processing failed for {Username}: {Error}")]
  public static partial void WhisperProcessingFailed(ILogger logger, string username, string error);

  [LoggerMessage(Level = LogLevel.Error, Message = "Whisper handler failed for {Username}: {Error}")]
  public static partial void WhisperHandlerFailed(ILogger logger, string username, string error);
}
