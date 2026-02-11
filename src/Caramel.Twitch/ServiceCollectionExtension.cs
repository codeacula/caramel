using TwitchLib.EventSub.Websockets;

namespace Caramel.Twitch;

public static class ServiceCollectionExtension
{
  public static IServiceCollection AddTwitchServices(this IServiceCollection services)
  {
    // Register TwitchConfig from configuration
    _ = services.AddSingleton(serviceProvider =>
    {
      var config = serviceProvider.GetRequiredService<IConfiguration>();
      return config.GetSection(nameof(TwitchConfig)).Get<TwitchConfig>() ?? throw new InvalidOperationException(
        "The configuration section for TwitchConfig is missing."
      );
    });

    // Register EventSub WebSocket client
    // Note: The client requires OAuth setup via SetAccessToken and SetClientId during EventSub subscription
    _ = services.AddSingleton<EventSubWebsocketClient>();

    // Register handlers as singletons (they're called from event handlers)
    _ = services.AddSingleton<ChatMessageEventHandler>();
    _ = services.AddSingleton<WhisperEventHandler>();

    return services;
  }
}
