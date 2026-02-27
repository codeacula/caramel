using Caramel.Core.Notifications;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

using NetCord;
using NetCord.Rest;

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

  public static IServiceCollection AddNotificationsWithChannels(this IServiceCollection services, string discordToken)
  {
    if (string.IsNullOrWhiteSpace(discordToken))
    {
      throw new ArgumentException("Discord token must be configured", nameof(discordToken));
    }

    _ = services.AddSingleton(new RestClient(new BotToken(discordToken)));
    return services.AddNotificationsWithChannels();
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


  public static IServiceCollection AddTwitchNotificationChannel(
    this IServiceCollection services,
    Func<IServiceProvider, string?> botUserIdFactory,
    Func<IServiceProvider, Func<string, string, string, CancellationToken, Task<bool>>> whisperSendFactory)
  {
    _ = services.AddScoped<INotificationChannel>(sp =>
    {
      var botUserId = botUserIdFactory(sp) ?? string.Empty;
      var sendDelegate = whisperSendFactory(sp);
      return new TwitchNotificationChannel(botUserId, sendDelegate);
    });
    return services;
  }
}

