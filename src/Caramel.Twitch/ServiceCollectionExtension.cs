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

    // Register in-memory Twitch setup state (loaded from DB at runtime)
    _ = services.AddSingleton<ITwitchSetupState, TwitchSetupState>();

    // Register EventSub WebSocket client
    _ = services.AddSingleton<EventSubWebsocketClient>();

    // Register chat broadcaster for Redis pub/sub
    _ = services.AddSingleton<ITwitchChatBroadcaster, TwitchChatBroadcaster>();

    // Register Twitch API utilities
    _ = services.AddSingleton<ITwitchUserResolver, TwitchUserResolver>();
    _ = services.AddSingleton<ITwitchWhisperService, TwitchWhisperService>();

    // Register handlers as singletons (they're called from event handlers)
    _ = services.AddSingleton<ChatMessageHandler>();
    _ = services.AddSingleton<WhisperHandler>();
    _ = services.AddSingleton<ChannelPointRedeemHandler>();

    return services;
  }
}
