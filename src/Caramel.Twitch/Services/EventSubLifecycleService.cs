

using Caramel.Twitch.Auth;

using TwitchLib.EventSub.Core.EventArgs.Channel;
using TwitchLib.EventSub.Core.EventArgs.User;

namespace Caramel.Twitch.Services;

internal sealed class EventSubLifecycleService(
  EventSubWebsocketClient eventSubClient,
  TwitchConfig twitchConfig,
  TwitchTokenManager tokenManager,
  ITwitchSetupState setupState,
  ICaramelServiceClient serviceClient,
  ChatMessageHandler chatHandler,
  WhisperHandler whisperHandler,
  ChannelPointRedeemHandler redeemHandler,
  IHttpClientFactory httpClientFactory,
  ILogger<EventSubLifecycleService> logger) : BackgroundService
{
  private TaskCompletionSource _disconnectTcs = new(TaskCreationOptions.RunContinuationsAsynchronously);
  private bool _handlersWired;

  protected override async Task ExecuteAsync(CancellationToken stoppingToken)
  {
    CaramelTwitchProgramLogs.EventSubStarting(logger);

    // Wire event handlers exactly once to prevent duplicate invocations on reconnect
    WireEventHandlers();

    while (!stoppingToken.IsCancellationRequested)
    {
      try
      {
        // Phase 1: Wait for valid OAuth tokens
        _ = await tokenManager.GetValidAccessTokenAsync(stoppingToken);

        // Phase 2: Load setup from DB (retry until configured)
        if (!setupState.IsConfigured)
        {
          var setupResult = await serviceClient.GetTwitchSetupAsync(stoppingToken);
          if (setupResult.IsSuccess && setupResult.Value is not null)
          {
            setupState.Update(setupResult.Value);
            CaramelTwitchProgramLogs.EventSubSetupLoaded(logger, setupResult.Value.BotLogin);
          }
          else
          {
            CaramelTwitchProgramLogs.EventSubWaitingForSetup(logger);
            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            continue;
          }
        }

        // Phase 3: Connect EventSub
        _disconnectTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        CaramelTwitchProgramLogs.EventSubConnecting(logger);
        _ = await eventSubClient.ConnectAsync();

        // Wait until either the host is stopping or the WebSocket disconnects
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
        var cancelTask = Task.Delay(Timeout.Infinite, cts.Token);
        var disconnectTask = _disconnectTcs.Task;
        var completedTask = await Task.WhenAny(disconnectTask, cancelTask);

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
    eventSubClient.ChannelPointsCustomRewardRedemptionAdd += OnChannelPointsCustomRewardRedemptionAddAsync;
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
      var setup = setupState.Current;
      if (setup is null)
      {
        CaramelTwitchProgramLogs.EventSubWaitingForSetup(logger);
        return;
      }

      var accessToken = await tokenManager.GetValidAccessTokenAsync();

      // Numeric IDs are stored in setup — no resolver needed
      var botUserId = setup.BotUserId;
      var channelIds = setup.Channels.Select(c => c.UserId).ToList();

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

        // Subscribe to channel point custom reward redemptions
        await CreateEventSubSubscriptionAsync(httpClient, "channel.channel_points_custom_reward_redemption.add", "1", new Dictionary<string, string>
        {
          { "broadcaster_user_id", channelId },
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

  private async Task OnChannelPointsCustomRewardRedemptionAddAsync(object? sender, ChannelPointsCustomRewardRedemptionArgs args)
  {
    var evt = args.Payload.Event;
    await redeemHandler.HandleAsync(
      evt.Id,
      evt.BroadcasterUserId,
      evt.BroadcasterUserLogin,
      evt.UserId,
      evt.UserLogin,
      evt.UserName,
      evt.Reward.Id,
      evt.Reward.Title,
      evt.Reward.Cost,
      evt.UserInput,
      evt.RedeemedAt,
      CancellationToken.None);
  }

  public override async Task StopAsync(CancellationToken cancellationToken)
  {
    CaramelTwitchProgramLogs.DisconnectingEventSub(logger);
    await base.StopAsync(cancellationToken);
  }
}
