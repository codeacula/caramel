namespace Caramel.Twitch;

public static class ServiceCollectionExtension
{
  public static IServiceCollection AddTwitchServices(this IServiceCollection services)
  {
    _ = services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<ICaramelTwitch>());
    _ = services.AddSingleton<IEventSubSubscriptionClient, EventSubSubscriptionClient>();

    foreach (var registrarType in typeof(ICaramelTwitch).Assembly
               .GetTypes()
               .Where(static type =>
                 type is { IsClass: true, IsAbstract: false } && typeof(IEventSubSubscriptionRegistrar).IsAssignableFrom(type)))
    {
      _ = services.AddSingleton(typeof(IEventSubSubscriptionRegistrar), registrarType);
    }

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
    _ = services.AddSingleton<ITwitchChatClient, TwitchChatClient>();

    // Register ads coordinator for cooldown tracking
    _ = services.AddSingleton<IAdsCoordinator, AdsCoordinator>();

    return services;
  }
}
