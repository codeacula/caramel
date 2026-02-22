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

  /// <summary>
  /// Registers a Twitch whisper notification channel that resolves the send delegate lazily
  /// from <paramref name="whisperSendFactory"/> at the time each notification is dispatched.
  /// Use this overload when the whisper sender is itself registered in DI so that the
  /// delegate is always backed by a fully-constructed service instance.
  /// </summary>
  public static IServiceCollection AddTwitchNotificationChannel(this IServiceCollection services, Func<IServiceProvider, Func<string, string, string, CancellationToken, Task<bool>>> whisperSendFactory, string botUserId)
  {
    if (string.IsNullOrWhiteSpace(botUserId))
    {
      throw new ArgumentException("Twitch bot user ID must be configured", nameof(botUserId));
    }

    _ = services.AddScoped<INotificationChannel>(sp =>
    {
      var sendDelegate = whisperSendFactory(sp);
      return new TwitchNotificationChannel(botUserId, sendDelegate);
    });
    return services;
  }
}

