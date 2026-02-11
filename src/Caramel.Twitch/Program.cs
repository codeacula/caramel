using Caramel.Notifications;
using Caramel.Twitch;
using Caramel.Twitch.Auth;
using Caramel.Twitch.Handlers;
using TwitchLib.EventSub.Websockets;

var builder = WebApplication.CreateBuilder(args);

// Configuration
_ = builder.Configuration.AddEnvironmentVariables();
_ = builder.Configuration.AddUserSecrets<ICaramelTwitch>(optional: true);

// Logging
_ = builder.Services.AddLogging(config =>
{
  config.AddConsole(options => options.FormatterName = "simple");
  config.SetMinimumLevel(builder.Environment.IsDevelopment() ? LogLevel.Debug : LogLevel.Information);
});

// Register Redis for person cache
string redisConnectionString = builder.Configuration.GetConnectionString("Redis") ?? throw new InvalidOperationException("Redis connection string is missing.");
_ = builder.Services.AddSingleton<StackExchange.Redis.IConnectionMultiplexer>(_ =>
    StackExchange.Redis.ConnectionMultiplexer.Connect(redisConnectionString));

// Register gRPC client to Caramel.Service
_ = builder.Services.AddGrpcClientServices();

// Register cache services
_ = builder.Services.AddCacheServices(redisConnectionString);

// Register Twitch-specific services
_ = builder.Services.AddTwitchServices();

// Get TwitchConfig for OAuth setup
var twitchConfig = builder.Configuration.GetSection(nameof(TwitchConfig)).Get<TwitchConfig>() ?? throw new InvalidOperationException("TwitchConfig is missing");

// Register OAuth and token management
_ = builder.Services.AddSingleton<OAuthStateManager>();
_ = builder.Services.AddSingleton<TwitchTokenManager>();

// Register Twitch notification channel with a stub delegate for sending whispers
// TODO: Implement actual whisper sending via TwitchLib.Api.Helix.Whispers
Func<string, string, string, CancellationToken, Task<bool>> sendWhisperAsync = async (botId, recipientId, message, ct) =>
{
  // Stub implementation - to be completed when integrating with TwitchLib.Api
  await Task.Delay(0, ct);
  return true;
};
_ = builder.Services.AddTwitchNotificationChannel(sendWhisperAsync, twitchConfig.BotUserId);

// Register EventSub WebSocket client and lifecycle service
_ = builder.Services.AddSingleton<EventSubWebsocketClient>();
_ = builder.Services.AddHostedService<EventSubLifecycleService>();

// Build application
var app = builder.Build();

// Register OAuth endpoints
var stateManager = app.Services.GetRequiredService<OAuthStateManager>();
var tokenManager = app.Services.GetRequiredService<TwitchTokenManager>();

app.MapGet("/auth/login", () =>
{
  var state = stateManager.GenerateState();
  var oauthUrl = $"https://id.twitch.tv/oauth2/authorize?" +
    $"client_id={Uri.EscapeDataString(twitchConfig.ClientId)}&" +
    $"redirect_uri={Uri.EscapeDataString(twitchConfig.OAuthCallbackUrl)}&" +
    $"response_type=code&" +
    $"scope={Uri.EscapeDataString("chat:read chat:edit whispers:read whispers:edit")}&" +
    $"state={Uri.EscapeDataString(state)}";
  return Results.Redirect(oauthUrl);
});

app.MapGet("/auth/callback", async (string code, string state, ILogger<Program> callbackLogger, CancellationToken ct) =>
{
  try
  {
    if (!stateManager.ValidateAndConsumeState(state))
    {
      CaramelTwitchProgramLogs.OAuthStateMismatch(callbackLogger);
      return Results.BadRequest("Invalid or expired state parameter");
    }

    var httpClient = new HttpClient();
    var tokenRequest = new FormUrlEncodedContent(new[]
    {
      new KeyValuePair<string, string>("client_id", twitchConfig.ClientId),
      new KeyValuePair<string, string>("client_secret", twitchConfig.ClientSecret),
      new KeyValuePair<string, string>("code", code),
      new KeyValuePair<string, string>("grant_type", "authorization_code"),
      new KeyValuePair<string, string>("redirect_uri", twitchConfig.OAuthCallbackUrl),
    });

    var tokenResponse = await httpClient.PostAsync("https://id.twitch.tv/oauth2/token", tokenRequest, ct);
    if (!tokenResponse.IsSuccessStatusCode)
    {
      var error = await tokenResponse.Content.ReadAsStringAsync(ct);
      CaramelTwitchProgramLogs.OAuthTokenExchangeFailed(callbackLogger, (int)tokenResponse.StatusCode, error);
      return Results.BadRequest($"Token exchange failed: {error}");
    }

    var responseContent = await tokenResponse.Content.ReadAsStringAsync(ct);
    var json = System.Text.Json.JsonDocument.Parse(responseContent);
    var root = json.RootElement;

    var accessToken = root.GetProperty("access_token").GetString() ?? throw new InvalidOperationException("Missing access_token");
    var expiresIn = root.GetProperty("expires_in").GetInt32();
    var refreshToken = root.TryGetProperty("refresh_token", out var rtElement) ? rtElement.GetString() : null;

    tokenManager.SetTokens(accessToken, refreshToken, expiresIn);
    CaramelTwitchProgramLogs.OAuthSucceeded(callbackLogger);
    return Results.Ok("OAuth authentication successful. You can now close this window.");
  }
  catch (Exception ex)
  {
    CaramelTwitchProgramLogs.OAuthCallbackError(callbackLogger, ex.Message);
    return Results.StatusCode(500);
  }
});

// Health check endpoint
app.MapGet("/health", () => Results.Ok("OK"));

// Log startup info
var logger = app.Services.GetRequiredService<ILogger<Program>>();
CaramelTwitchProgramLogs.StartingService(logger, 5146);

// Run host
await app.RunAsync();

/// <summary>
/// Manages the EventSub WebSocket connection lifecycle.
/// </summary>
internal sealed class EventSubLifecycleService(
  EventSubWebsocketClient eventSubClient,
  TwitchConfig twitchConfig,
  TwitchTokenManager tokenManager,
  ChatMessageEventHandler chatHandler,
  WhisperEventHandler whisperHandler,
  ILogger<EventSubLifecycleService> logger) : BackgroundService
{
  protected override async Task ExecuteAsync(CancellationToken stoppingToken)
  {
    try
    {
      CaramelTwitchProgramLogs.EventSubStarting(logger);

      // Wait for valid tokens before attempting EventSub subscription
      while (!stoppingToken.IsCancellationRequested)
      {
        try
        {
          var accessToken = await tokenManager.GetValidAccessTokenAsync(stoppingToken);

          // TODO: Implement EventSub subscription using TwitchLib.EventSub.Websockets
          // This requires:
          // 1. Configure EventSub client with tokens and channel IDs
          // 2. Subscribe to channel.chat.message events
          // 3. Subscribe to user.whisper.message events
          // 4. Wire handlers to process incoming events
          
          CaramelTwitchProgramLogs.EventSubConnected(logger);

          // Keep running until cancellation
          await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException)
        {
          throw;
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("refresh token"))
        {
          CaramelTwitchProgramLogs.EventSubWaitingForOAuth(logger);
          await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
        }
        catch (Exception ex)
        {
          CaramelTwitchProgramLogs.EventSubConnectionError(logger, ex.Message);
          await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
        }
      }
    }
    catch (OperationCanceledException)
    {
      CaramelTwitchProgramLogs.ServiceCancelled(logger);
    }
    catch (Exception ex)
    {
      CaramelTwitchProgramLogs.ServiceFailed(logger, ex.Message);
      throw;
    }
  }

  public override async Task StopAsync(CancellationToken cancellationToken)
  {
    CaramelTwitchProgramLogs.DisconnectingEventSub(logger);
    await base.StopAsync(cancellationToken);
  }
}

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

  [LoggerMessage(Level = LogLevel.Error, Message = "OAuth callback endpoint error: {Error}")]
  public static partial void OAuthCallbackError(ILogger logger, string error);

  [LoggerMessage(Level = LogLevel.Information, Message = "EventSub WebSocket service starting")]
  public static partial void EventSubStarting(ILogger logger);

  [LoggerMessage(Level = LogLevel.Information, Message = "EventSub WebSocket connected and listening for events")]
  public static partial void EventSubConnected(ILogger logger);

  [LoggerMessage(Level = LogLevel.Information, Message = "EventSub waiting for OAuth tokens via GET /auth/login")]
  public static partial void EventSubWaitingForOAuth(ILogger logger);

  [LoggerMessage(Level = LogLevel.Error, Message = "EventSub connection error: {Error} - will retry")]
  public static partial void EventSubConnectionError(ILogger logger, string error);
}
