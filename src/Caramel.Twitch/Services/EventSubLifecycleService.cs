

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
  IEnumerable<IEventSubSubscriptionRegistrar> subscriptionRegistrars,
  IMediator mediator,
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
        if (!await TryConnectEventSubAsync(stoppingToken))
        {
          await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
          continue;
        }

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

  private async Task<bool> TryConnectEventSubAsync(CancellationToken stoppingToken)
  {
    if (stoppingToken.IsCancellationRequested)
    {
      return false;
    }

    var connected = await eventSubClient.ConnectAsync();
    if (connected)
    {
      return true;
    }

    CaramelTwitchProgramLogs.EventSubConnectionError(logger, "EventSub ConnectAsync returned false. Trying ReconnectAsync.");

    var reconnected = await eventSubClient.ReconnectAsync();
    if (reconnected)
    {
      return true;
    }

    CaramelTwitchProgramLogs.EventSubConnectionError(logger, "EventSub ReconnectAsync returned false.");
    return false;
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
      var registrationContext = new EventSubSubscriptionRegistrationContext(
        HttpClient: httpClientFactory.CreateClient("TwitchHelix"),
        SessionId: eventSubClient.SessionId,
        BotUserId: setup.BotUserId,
        ChannelUserIds: [.. setup.Channels.Select(c => c.UserId)]);

      using var httpClient = registrationContext.HttpClient;
      httpClient.DefaultRequestHeaders.Add("Client-Id", twitchConfig.ClientId);
      httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {accessToken}");

      foreach (var registrar in subscriptionRegistrars.OrderBy(static x => x.GetType().Name, StringComparer.Ordinal))
      {
        await registrar.RegisterAsync(registrationContext, CancellationToken.None);
      }
    }
    catch (Exception ex)
    {
      CaramelTwitchProgramLogs.EventSubSubscriptionSetupFailed(logger, ex.Message);
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
    await mediator.Publish(new ChannelChatMessageReceived(
      evt.BroadcasterUserId,
      evt.BroadcasterUserLogin,
      evt.ChatterUserId,
      evt.ChatterUserLogin,
      evt.ChatterUserName,
      evt.MessageId,
      evt.Message.Text,
      evt.Color ?? string.Empty), CancellationToken.None);
  }

  private async Task OnUserWhisperMessageAsync(object? sender, UserWhisperMessageArgs args)
  {
    var evt = args.Payload.Event;
    await mediator.Publish(new UserWhisperMessageReceived(
      evt.FromUserId,
      evt.FromUserLogin,
      evt.Whisper.Text), CancellationToken.None);
  }

  private async Task OnChannelPointsCustomRewardRedemptionAddAsync(object? sender, ChannelPointsCustomRewardRedemptionArgs args)
  {
    var evt = args.Payload.Event;
    await mediator.Publish(new ChannelPointsCustomRewardRedeemed(
      evt.Id,
      evt.BroadcasterUserId,
      evt.BroadcasterUserLogin,
      evt.UserId,
      evt.UserLogin,
      evt.UserName,
      evt.Reward.Id,
      evt.Reward.Title,
      evt.Reward.Cost,
      evt.UserInput ?? string.Empty,
      evt.RedeemedAt), CancellationToken.None);
  }

  public override async Task StopAsync(CancellationToken cancellationToken)
  {
    CaramelTwitchProgramLogs.DisconnectingEventSub(logger);
    await base.StopAsync(cancellationToken);
  }
}
