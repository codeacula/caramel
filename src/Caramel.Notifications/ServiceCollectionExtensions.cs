using Caramel.Core.Notifications;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Caramel.Notifications;

public static class ServiceCollectionExtensions
{
  public static IServiceCollection AddNotifications(this IServiceCollection services)
  {
    services.TryAddScoped<IPersonNotificationClient, NoOpPersonNotificationClient>();
    return services;
  }

  public static IServiceCollection AddNotificationsWithChannels(this IServiceCollection services)
  {
    _ = services.AddScoped<INotificationChannel, DiscordNotificationChannel>();
    _ = services.AddScoped<IPersonNotificationClient, PersonNotificationClient>();
    return services;
  }

  public static IServiceCollection AddTwitchNotificationChannel(this IServiceCollection services, Func<string, string, string, CancellationToken, Task<bool>> sendWhisperAsync, string botUserId)
  {
    if (string.IsNullOrWhiteSpace(botUserId))
    {
      throw new ArgumentException("Twitch bot user ID must be configured", nameof(botUserId));
    }

    _ = services.AddScoped<INotificationChannel>(_ => new TwitchNotificationChannel(botUserId, sendWhisperAsync));
    return services;
  }
}

