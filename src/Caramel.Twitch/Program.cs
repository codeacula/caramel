using Caramel.Core.Configuration;
using Caramel.Core.Security;
using Caramel.Database;
using Caramel.Notifications;
using Caramel.Twitch;
using Caramel.Twitch.Auth;
using Microsoft.AspNetCore.DataProtection;

var builder = WebApplication.CreateBuilder(args);

// Register MVC controllers
_ = builder.Services.AddControllers();

// Configuration
_ = builder.Configuration.AddEnvironmentVariables();
_ = builder.Configuration.AddUserSecrets<ICaramelTwitch>(optional: true);

// Logging
_ = builder.Services.AddLogging(config =>
{
  _ = config.AddConsole(options => options.FormatterName = "simple");
  _ = config.SetMinimumLevel(builder.Environment.IsDevelopment() ? LogLevel.Debug : LogLevel.Information);
});

// Register Redis, cache, gRPC, database, and Twitch-specific services
string redisConnectionString = builder.Configuration.GetConnectionString("Redis") ?? throw new InvalidOperationException("Redis connection string is missing.");
_ = builder.Services
  .AddCaramelOptions(builder.Configuration)
  .AddCacheServices(redisConnectionString)
  .AddDatabaseServices(builder.Configuration)
  .AddGrpcClientServices()
  .AddTwitchServices();

// Register ASP.NET Core Data Protection API for token encryption
_ = builder.Services.AddDataProtection();

// Register a named HttpClient for Twitch Helix API calls
_ = builder.Services.AddHttpClient("TwitchHelix");

// Get TwitchConfig for OAuth setup
var twitchConfig = builder.Configuration.GetSection(nameof(TwitchConfig)).Get<TwitchConfig>() ?? throw new InvalidOperationException("TwitchConfig is missing");

// Register token encryption service (required for token persistence)
_ = builder.Services.AddSingleton<ITokenEncryptionService>(sp =>
{
  var dataProtectionProvider = sp.GetRequiredService<IDataProtectionProvider>();
  return new TokenEncryptionService(dataProtectionProvider);
});

// Register OAuth and token management (dual OAuth: bot + broadcaster)
_ = builder.Services.AddSingleton<DualOAuthStateManager>();
_ = builder.Services.AddSingleton<IDualOAuthTokenManager, DualOAuthTokenManager>();

// Register Twitch notification channel using ITwitchSetupState for dynamic botUserId resolution.
// The factory resolves at dispatch time so a missing setup degrades gracefully.
_ = builder.Services.AddTwitchNotificationChannel(
  botUserIdFactory: sp =>
  {
    var state = sp.GetRequiredService<ITwitchSetupState>();
    return state.Current?.BotUserId;
  },
  whisperSendFactory: sp =>
  {
    var svc = sp.GetRequiredService<ITwitchWhisperService>();
    return (botId, recipientId, message, ct) => svc.SendWhisperAsync(botId, recipientId, message, ct);
  });

// Register EventSub lifecycle service (EventSubWebsocketClient already registered by AddTwitchServices)
_ = builder.Services.AddHostedService<EventSubLifecycleService>();

// Build application
var app = builder.Build();

_ = app.MapControllers();

// Health check endpoint
app.MapGet("/health", () => Results.Ok("OK"));

// Log startup info
var logger = app.Services.GetRequiredService<ILogger<Program>>();
CaramelTwitchProgramLogs.StartingService(logger, 5146);

// Run host
await app.RunAsync();

// ---------------------------------------------------------------------------

/// <summary>
/// Structured logging for Caramel.Twitch Program.
/// </summary>
internal static partial class CaramelTwitchProgramLogs
{
  [LoggerMessage(Level = LogLevel.Information, Message = "Caramel.Twitch starting on port {Port}")]
  public static partial void StartingService(ILogger logger, int port);

  [LoggerMessage(Level = LogLevel.Information, Message = "Service cancellation requested")]
  public static partial void ServiceCancelled(ILogger logger);

  [LoggerMessage(Level = LogLevel.Error, Message = "Service failed: {Error}")]
  public static partial void ServiceFailed(ILogger logger, string error);

  [LoggerMessage(Level = LogLevel.Information, Message = "Disconnecting EventSub WebSocket")]
  public static partial void DisconnectingEventSub(ILogger logger);

  [LoggerMessage(Level = LogLevel.Warning, Message = "OAuth state parameter validation failed - possible CSRF attempt")]
  public static partial void OAuthStateMismatch(ILogger logger);

  [LoggerMessage(Level = LogLevel.Error, Message = "OAuth token exchange failed with status {StatusCode}: {Error}")]
  public static partial void OAuthTokenExchangeFailed(ILogger logger, int statusCode, string error);

  [LoggerMessage(Level = LogLevel.Information, Message = "OAuth authentication successful - bot tokens updated")]
  public static partial void OAuthSucceeded(ILogger logger);

  [LoggerMessage(Level = LogLevel.Information, Message = "OAuth setup auto-configured for user '{Login}'")]
  public static partial void OAuthSetupConfigured(ILogger logger, string login);

  [LoggerMessage(Level = LogLevel.Warning, Message = "OAuth setup auto-configuration failed: {Error}")]
  public static partial void OAuthSetupFailed(ILogger logger, string error);

  [LoggerMessage(Level = LogLevel.Error, Message = "OAuth callback endpoint error: {Error}")]
  public static partial void OAuthCallbackError(ILogger logger, string error);

  [LoggerMessage(Level = LogLevel.Information, Message = "EventSub WebSocket service starting")]
  public static partial void EventSubStarting(ILogger logger);

  [LoggerMessage(Level = LogLevel.Information, Message = "Attempting to connect EventSub WebSocket")]
  public static partial void EventSubConnecting(ILogger logger);

  [LoggerMessage(Level = LogLevel.Information, Message = "EventSub WebSocket connected - creating subscriptions")]
  public static partial void EventSubConnected(ILogger logger);

  [LoggerMessage(Level = LogLevel.Information, Message = "EventSub WebSocket disconnected")]
  public static partial void EventSubDisconnected(ILogger logger);

  [LoggerMessage(Level = LogLevel.Information, Message = "EventSub subscribed to {EventType} for channel {ChannelId}")]
  public static partial void EventSubSubscribed(ILogger logger, string eventType, string channelId);

  [LoggerMessage(Level = LogLevel.Warning, Message = "EventSub subscription failed for {EventType} on channel {ChannelId}: {Error}")]
  public static partial void EventSubSubscriptionFailed(ILogger logger, string eventType, string channelId, string error);

  [LoggerMessage(Level = LogLevel.Error, Message = "EventSub subscription setup failed: {Error}")]
  public static partial void EventSubSubscriptionSetupFailed(ILogger logger, string error);

  [LoggerMessage(Level = LogLevel.Information, Message = "EventSub waiting for OAuth tokens - visit GET /auth/twitch/login")]
  public static partial void EventSubWaitingForOAuth(ILogger logger);

  [LoggerMessage(Level = LogLevel.Information, Message = "EventSub waiting for Twitch setup - visit POST /twitch/setup to configure")]
  public static partial void EventSubWaitingForSetup(ILogger logger);

  [LoggerMessage(Level = LogLevel.Information, Message = "EventSub loaded Twitch setup for bot '{BotLogin}'")]
  public static partial void EventSubSetupLoaded(ILogger logger, string botLogin);

  [LoggerMessage(Level = LogLevel.Error, Message = "EventSub connection error: {Error} - will retry")]
  public static partial void EventSubConnectionError(ILogger logger, string error);

  [LoggerMessage(Level = LogLevel.Warning, Message = "EventSub connect failed after prior success; attempting reconnect")]
  public static partial void EventSubConnectFailedAttemptingReconnect(ILogger logger);

  [LoggerMessage(Level = LogLevel.Information, Message = "EventSub WebSocket reconnected successfully")]
  public static partial void EventSubReconnected(ILogger logger);
}
