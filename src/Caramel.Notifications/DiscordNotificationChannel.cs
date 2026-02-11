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
    catch (Exception ex)
    {
      return Result.Fail($"Failed to send Discord notification: {ex.Message}");
    }
  }
}
