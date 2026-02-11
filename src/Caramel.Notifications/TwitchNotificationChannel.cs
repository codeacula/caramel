using Caramel.Core.Notifications;
using Caramel.Domain.Common.Enums;

using FluentResults;

namespace Caramel.Notifications;

/// <summary>
/// Sends notifications via Twitch whispers (private messages).
/// Note: This requires the bot to have verified status and appropriate OAuth scopes (user:manage:whispers).
/// </summary>
public sealed class TwitchNotificationChannel : INotificationChannel
{
  private readonly string _botUserId;
  private readonly Func<string, string, string, CancellationToken, Task<bool>> _sendWhisperAsync;

  public NotificationChannelType ChannelType => NotificationChannelType.Twitch;

  /// <summary>
  /// Initializes a new instance of TwitchNotificationChannel.
  /// </summary>
  /// <param name="botUserId">The Twitch user ID of the bot account.</param>
  /// <param name="sendWhisperAsync">Delegate to send whispers (injected from TwitchLib client).</param>
  public TwitchNotificationChannel(string botUserId, Func<string, string, string, CancellationToken, Task<bool>> sendWhisperAsync)
  {
    _botUserId = botUserId ?? throw new ArgumentNullException(nameof(botUserId));
    _sendWhisperAsync = sendWhisperAsync ?? throw new ArgumentNullException(nameof(sendWhisperAsync));
  }

  public async Task<Result> SendAsync(string identifier, Notification notification, CancellationToken cancellationToken = default)
  {
    try
    {
      if (string.IsNullOrWhiteSpace(identifier))
      {
        return Result.Fail("Invalid Twitch recipient user ID");
      }

      if (string.IsNullOrWhiteSpace(notification.Content))
      {
        return Result.Fail("Notification content cannot be empty");
      }

      // Send whisper from bot to recipient
      var success = await _sendWhisperAsync(_botUserId, identifier, notification.Content, cancellationToken);

      if (!success)
      {
        return Result.Fail("Failed to send Twitch whisper (bot may lack verified status or scopes)");
      }

      return Result.Ok();
    }
    catch (Exception ex)
    {
      return Result.Fail($"Failed to send Twitch notification: {ex.Message}");
    }
  }
}

