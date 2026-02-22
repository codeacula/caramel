using System.Text.Json;

using Caramel.Notifications;
using Caramel.Twitch;
using Caramel.Twitch.Auth;
using Caramel.Twitch.Handlers;
using Caramel.Twitch.Services;

using TwitchLib.EventSub.Core.EventArgs.Channel;
using TwitchLib.EventSub.Core.EventArgs.User;
using TwitchLib.EventSub.Websockets;
using TwitchLib.EventSub.Websockets.Core.EventArgs;

var builder = WebApplication.CreateBuilder(args);

// Register MVC controllers
_ = builder.Services.AddControllers();

// Configuration
_ = builder.Configuration.AddEnvironmentVariables();
_ = builder.Configuration.AddUserSecrets<ICaramelTwitch>(optional: true);

// Logging
_ = builder.Services.AddLogging(config =>
{
  config.AddConsole(options => options.FormatterName = "simple");
  config.SetMinimumLevel(builder.Environment.IsDevelopment() ? LogLevel.Debug : LogLevel.Information);
});

// Register Redis, cache, gRPC, and Twitch-specific services
string redisConnectionString = builder.Configuration.GetConnectionString("Redis") ?? throw new InvalidOperationException("Redis connection string is missing.");
_ = builder.Services
  .AddCacheServices(redisConnectionString)
  .AddGrpcClientServices()
  .AddTwitchServices();

// Register a named HttpClient for Twitch Helix API calls
_ = builder.Services.AddHttpClient("TwitchHelix");

// Get TwitchConfig for OAuth setup
var twitchConfig = builder.Configuration.GetSection(nameof(TwitchConfig)).Get<TwitchConfig>() ?? throw new InvalidOperationException("TwitchConfig is missing");

// Register OAuth and token management
_ = builder.Services.AddSingleton<OAuthStateManager>();
_ = builder.Services.AddSingleton<TwitchTokenManager>();

// Register Twitch notification channel — sends whispers via the Helix API.
// The factory overload resolves ITwitchWhisperService lazily from the scoped DI
// container each time a notification is dispatched, avoiding the pre-build ordering issue.
_ = builder.Services.AddTwitchNotificationChannel(
  whisperSendFactory: sp =>
  {
    var svc = sp.GetRequiredService<ITwitchWhisperService>();
    return (botId, recipientId, message, ct) => svc.SendWhisperAsync(botId, recipientId, message, ct);
  },
  botUserId: twitchConfig.BotUserId);

// Register EventSub lifecycle service (EventSubWebsocketClient already registered by AddTwitchServices)
_ = builder.Services.AddHostedService<EventSubLifecycleService>();

// Build application
var app = builder.Build();

_ = app.MapControllers();

// Register OAuth endpoints
var stateManager = app.Services.GetRequiredService<OAuthStateManager>();
var tokenManager = app.Services.GetRequiredService<TwitchTokenManager>();

app.MapGet("/auth/login", () =>
{
  var state = stateManager.GenerateState();

  // Request scopes for chat read/write, whispers, and moderation actions
  const string scopes = "chat:read chat:edit whispers:read whispers:edit moderator:manage:banned_users moderator:manage:chat_messages channel:moderate user:bot user:read:chat user:write:chat user:manage:whispers";

  var oauthUrl = $"https://id.twitch.tv/oauth2/authorize?" +
    $"client_id={Uri.EscapeDataString(twitchConfig.ClientId)}&" +
    $"redirect_uri={Uri.EscapeDataString(twitchConfig.OAuthCallbackUrl)}&" +
    $"response_type=code&" +
    $"scope={Uri.EscapeDataString(scopes)}&" +
    $"state={Uri.EscapeDataString(state)}";
  return Results.Redirect(oauthUrl);
});

app.MapGet("/auth/callback", async (string code, string state, IHttpClientFactory httpClientFactory, ILogger<Program> callbackLogger, CancellationToken ct) =>
{
  try
  {
    if (!stateManager.ValidateAndConsumeState(state))
    {
      CaramelTwitchProgramLogs.OAuthStateMismatch(callbackLogger);
      return Results.BadRequest("Invalid or expired state parameter");
    }

    using var httpClient = httpClientFactory.CreateClient("TwitchHelix");
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
    var json = JsonDocument.Parse(responseContent);
    var root = json.RootElement;

    var accessToken = root.GetProperty("access_token").GetString() ?? throw new InvalidOperationException("Missing access_token");
    var expiresIn = root.GetProperty("expires_in").GetInt32();
    var refreshToken = root.TryGetProperty("refresh_token", out var rtElement) ? rtElement.GetString() : null;

    tokenManager.SetTokens(accessToken, refreshToken, expiresIn);
    CaramelTwitchProgramLogs.OAuthSucceeded(callbackLogger);

    return Results.Content("""
      <!doctype html>
      <html><head><title>Caramel - Authorized</title></head>
      <body style="font-family:sans-serif;text-align:center;padding:4rem">
        <h1>Twitch authorized successfully</h1>
        <p>You can close this window and return to Caramel.</p>
      </body></html>
      """, "text/html");
  }
  catch (Exception ex)
  {
    CaramelTwitchProgramLogs.OAuthCallbackError(callbackLogger, ex.Message);
    return Results.StatusCode(500);
  }
});

// Auth status endpoint - allows the UI and other services to check token state
app.MapGet("/auth/status", () =>
{
  var hasToken = tokenManager.CanRefresh() || tokenManager.GetCurrentAccessToken() is { Length: > 0 };
  return Results.Ok(new { authorized = hasToken });
});

// Health check endpoint
app.MapGet("/health", () => Results.Ok("OK"));

// Log startup info
var logger = app.Services.GetRequiredService<ILogger<Program>>();
CaramelTwitchProgramLogs.StartingService(logger, 5146);

// Run host
await app.RunAsync();

// ---------------------------------------------------------------------------

/// <summary>
/// Manages the EventSub WebSocket connection lifecycle.
/// Waits for valid OAuth tokens then connects to Twitch EventSub, subscribes to
/// channel.chat.message and user.whisper.message events, and wires the handlers.
/// Event handlers are registered exactly once to prevent duplicate invocations.
/// Uses a TaskCompletionSource to detect disconnects and re-enter the connect loop.
/// </summary>
internal sealed class EventSubLifecycleService(
  EventSubWebsocketClient eventSubClient,
  TwitchConfig twitchConfig,
  TwitchTokenManager tokenManager,
  ITwitchUserResolver userResolver,
  ChatMessageEventHandler chatHandler,
  WhisperEventHandler whisperHandler,
  IHttpClientFactory httpClientFactory,
  ILogger<EventSubLifecycleService> logger) : BackgroundService
{
  private TaskCompletionSource _disconnectTcs = new(TaskCreationOptions.RunContinuationsAsynchronously);
  private bool _handlersWired;

  protected override async Task ExecuteAsync(CancellationToken stoppingToken)
  {
    try
    {
      CaramelTwitchProgramLogs.EventSubStarting(logger);

      // Wire event handlers exactly once to prevent duplicate invocations on reconnect
      WireEventHandlers();

      while (!stoppingToken.IsCancellationRequested)
      {
        try
        {
          _ = await tokenManager.GetValidAccessTokenAsync(stoppingToken);

          // Reset the disconnect signal before each connection attempt
          _disconnectTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

          CaramelTwitchProgramLogs.EventSubConnecting(logger);
          await eventSubClient.ConnectAsync();

          // Wait until either the host is stopping or the WebSocket disconnects
          using var cts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
          var cancelTask = Task.Delay(Timeout.Infinite, cts.Token);
          var completedTask = await Task.WhenAny(_disconnectTcs.Task, cancelTask);

          if (completedTask == cancelTask)
          {
            // Host is shutting down
            break;
          }

          // WebSocket disconnected - log and retry after a brief delay
          CaramelTwitchProgramLogs.EventSubConnectionError(logger, "WebSocket disconnected, reconnecting...");
          await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
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

  /// <summary>
  /// Registers event handlers on the EventSub client exactly once.
  /// </summary>
  private void WireEventHandlers()
  {
    if (_handlersWired)
    {
      return;
    }

    eventSubClient.WebsocketConnected += OnWebsocketConnectedAsync;
    eventSubClient.WebsocketDisconnected += OnWebsocketDisconnectedAsync;
    eventSubClient.ChannelChatMessage += OnChannelChatMessageAsync;
    eventSubClient.UserWhisperMessage += OnUserWhisperMessageAsync;
    _handlersWired = true;
  }

  private async Task OnWebsocketConnectedAsync(object? sender, WebsocketConnectedArgs args)
  {
    if (args.IsRequestedReconnect)
    {
      return;
    }

    CaramelTwitchProgramLogs.EventSubConnected(logger);

    try
    {
      var accessToken = await tokenManager.GetValidAccessTokenAsync();

      // Resolve channel login names/IDs to numeric IDs required by the Helix API.
      var rawChannelIds = twitchConfig.ChannelIds
        .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
      var channelIds = await userResolver.ResolveUserIdsAsync(rawChannelIds);

      // Resolve bot user ID (may be a username in config)
      var botUserId = await userResolver.ResolveUserIdAsync(twitchConfig.BotUserId);

      using var httpClient = httpClientFactory.CreateClient("TwitchHelix");
      httpClient.DefaultRequestHeaders.Add("Client-Id", twitchConfig.ClientId);
      httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {accessToken}");

      foreach (var channelId in channelIds)
      {
        // Subscribe to channel chat messages
        await CreateEventSubSubscriptionAsync(httpClient, "channel.chat.message", "1", new Dictionary<string, string>
        {
          { "broadcaster_user_id", channelId },
          { "user_id", botUserId },
        });
      }

      // Subscribe to incoming whispers directed at the bot user
      await CreateEventSubSubscriptionAsync(httpClient, "user.whisper.message", "1", new Dictionary<string, string>
      {
        { "user_id", botUserId },
      });
    }
    catch (Exception ex)
    {
      CaramelTwitchProgramLogs.EventSubSubscriptionSetupFailed(logger, ex.Message);
    }
  }

  private async Task CreateEventSubSubscriptionAsync(
    HttpClient httpClient,
    string eventType,
    string version,
    Dictionary<string, string> condition)
  {
    var conditionDisplay = string.Join(", ", condition.Select(kvp => $"{kvp.Key}={kvp.Value}"));

    try
    {
      var body = new
      {
        type = eventType,
        version,
        condition,
        transport = new
        {
          method = "websocket",
          session_id = eventSubClient.SessionId,
        },
      };

      var json = JsonSerializer.Serialize(body);
      using var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
      var response = await httpClient.PostAsync("https://api.twitch.tv/helix/eventsub/subscriptions", content);

      if (response.IsSuccessStatusCode)
      {
        CaramelTwitchProgramLogs.EventSubSubscribed(logger, eventType, conditionDisplay);
      }
      else
      {
        var errorBody = await response.Content.ReadAsStringAsync();
        CaramelTwitchProgramLogs.EventSubSubscriptionFailed(logger, eventType, conditionDisplay, $"{(int)response.StatusCode}: {errorBody}");
      }
    }
    catch (Exception ex)
    {
      CaramelTwitchProgramLogs.EventSubSubscriptionFailed(logger, eventType, conditionDisplay, ex.Message);
    }
  }

  private Task OnWebsocketDisconnectedAsync(object? sender, WebsocketDisconnectedArgs args)
  {
    CaramelTwitchProgramLogs.EventSubDisconnected(logger);
    // Signal the connect loop to re-enter
    _ = _disconnectTcs.TrySetResult();
    return Task.CompletedTask;
  }

  private async Task OnChannelChatMessageAsync(object? sender, ChannelChatMessageArgs args)
  {
    var evt = args.Payload.Event;
    await chatHandler.HandleAsync(
      evt.BroadcasterUserId,
      evt.BroadcasterUserLogin,
      evt.ChatterUserId,
      evt.ChatterUserLogin,
      evt.ChatterUserName,
      evt.MessageId,
      evt.Message.Text,
      evt.Color,
      CancellationToken.None);
  }

  private async Task OnUserWhisperMessageAsync(object? sender, UserWhisperMessageArgs args)
  {
    var evt = args.Payload.Event;
    await whisperHandler.HandleAsync(
      evt.FromUserId,
      evt.FromUserLogin,
      evt.Whisper.Text,
      CancellationToken.None);
  }

  public override async Task StopAsync(CancellationToken cancellationToken)
  {
    CaramelTwitchProgramLogs.DisconnectingEventSub(logger);
    await base.StopAsync(cancellationToken);
  }
}

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

  [LoggerMessage(Level = LogLevel.Information, Message = "EventSub waiting for OAuth tokens - visit GET /auth/login")]
  public static partial void EventSubWaitingForOAuth(ILogger logger);

  [LoggerMessage(Level = LogLevel.Error, Message = "EventSub connection error: {Error} - will retry")]
  public static partial void EventSubConnectionError(ILogger logger, string error);
}
