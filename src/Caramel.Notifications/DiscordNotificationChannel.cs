using Caramel.Core.Notifications;
using Caramel.Domain.Common.Enums;

using FluentResults;

using NetCord.Rest;

namespace Caramel.Notifications;

public sealed class DiscordNotificationChannel(RestClient restClient) : INotificationChannel
{
  public NotificationChannelType ChannelType => NotificationChannelType.Discord;

  public async Task<Result> SendAsync(string identifier, Notification notification, CancellationToken cancellationToken = default)
  {
    try
    {
      if (!ulong.TryParse(identifier, out var userId))
      {
        return Result.Fail($"Invalid Discord user ID: {identifier}");
      }

      // Get or create a DM channel with the user
      var dmChannel = await restClient.GetDMChannelAsync(userId, cancellationToken: cancellationToken);

      var messageProperties = new MessageProperties
      {
        Content = notification.Content
      };

      _ = await dmChannel.SendMessageAsync(messageProperties, cancellationToken: cancellationToken);

      return Result.Ok();
    }
    catch (OperationCanceledException)
    {
      throw;
    }
    catch (HttpRequestException ex)
    {
      return Result.Fail($"Discord API network error: {ex.Message}");
    }
    catch (InvalidOperationException ex)
    {
      return Result.Fail($"Invalid Discord operation state: {ex.Message}");
    }
    catch (Exception ex)
    {
      return Result.Fail($"Unexpected error sending Discord notification: {ex.Message}");
    }
  }
}
