using Caramel.Twitch.Extensions;

namespace Caramel.Twitch.Handlers;

public sealed class WhisperHandler(
  ICaramelServiceClient caramelServiceClient,
  IPersonCache personCache,
  ILogger<WhisperHandler> logger) : INotificationHandler<UserWhisperMessageReceived>
{
  public async Task Handle(UserWhisperMessageReceived notification, CancellationToken cancellationToken)
  {
    try
    {
      var platformId = TwitchPlatformExtension.GetTwitchPlatformId(notification.FromUserLogin, notification.FromUserId);

      // Check access via cache (fast path)
      var accessResult = await personCache.GetAccessAsync(platformId);
      if (accessResult.IsFailed)
      {
        CaramelTwitchWhisperLogs.AccessCheckFailed(logger, notification.FromUserLogin, accessResult.Errors[0].Message);
        return;
      }

      if (accessResult.Value is false)
      {
        CaramelTwitchWhisperLogs.AccessDenied(logger, notification.FromUserLogin);
        return;
      }

      // Send whisper as a message to the AI
      var request = new ProcessMessageRequest
      {
        Platform = Platform.Twitch,
        PlatformUserId = platformId.PlatformUserId,
        Username = platformId.Username,
        Content = notification.MessageText
      };

      var result = await caramelServiceClient.SendMessageAsync(request, cancellationToken);
      if (result.IsSuccess)
      {
        CaramelTwitchWhisperLogs.WhisperProcessed(logger, notification.FromUserLogin);
      }
      else
      {
        CaramelTwitchWhisperLogs.WhisperProcessingFailed(logger, notification.FromUserLogin, result.Errors[0].Message);
      }
    }
    catch (OperationCanceledException)
    {
      throw;
    }
    catch (InvalidOperationException ex)
    {
      CaramelTwitchWhisperLogs.WhisperHandlerFailed(logger, notification.FromUserLogin, $"Invalid state: {ex.Message}");
    }
    catch (Exception ex)
    {
      CaramelTwitchWhisperLogs.WhisperHandlerFailed(logger, notification.FromUserLogin, ex.Message);
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
