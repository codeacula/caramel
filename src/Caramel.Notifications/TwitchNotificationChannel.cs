using Caramel.Core.Notifications;
using Caramel.Domain.Common.Enums;

using FluentResults;

namespace Caramel.Notifications;

/// <summary>
/// Sends notifications via Twitch whispers (private messages).
/// Note: This requires the bot to have verified status and appropriate OAuth scopes (user:manage:whispers).
/// </summary>
/// <remarks>
/// Initializes a new instance of TwitchNotificationChannel.
/// </remarks>
/// <param name="botUserId">The Twitch user ID of the bot account.</param>
/// <param name="sendWhisperAsync">Delegate to send whispers (injected from TwitchLib client).</param>
public sealed class TwitchNotificationChannel(string botUserId, Func<string, string, string, CancellationToken, Task<bool>> sendWhisperAsync) : INotificationChannel
{
  private readonly string _botUserId = botUserId ?? string.Empty;
  private readonly Func<string, string, string, CancellationToken, Task<bool>> _sendWhisperAsync = sendWhisperAsync ?? throw new ArgumentNullException(nameof(sendWhisperAsync));

  public NotificationChannelType ChannelType => NotificationChannelType.Twitch;

  public async Task<Result> SendAsync(string identifier, Notification notification, CancellationToken cancellationToken = default)
  {
    try
    {
      if (string.IsNullOrWhiteSpace(_botUserId))
      {
        return Result.Fail("Twitch bot is not configured yet");
      }

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

      return !success ? Result.Fail("Failed to send Twitch whisper (bot may lack verified status or scopes)") : Result.Ok();
    }
    catch (Exception ex)
    {
      return Result.Fail($"Failed to send Twitch notification: {ex.Message}");
    }
  }
}

