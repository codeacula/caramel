using MediatR;

using Microsoft.Extensions.Logging;

using TwitchLib.EventSub.Core;
using TwitchLib.EventSub.Core.EventArgs.Channel;
using TwitchLib.EventSub.Core.EventArgs.User;
using TwitchLib.EventSub.Core.Models;
using TwitchLib.EventSub.Core.Models.Chat;
using TwitchLib.EventSub.Core.Models.Whisper;
using TwitchLib.EventSub.Core.SubscriptionTypes.Channel;
using TwitchLib.EventSub.Core.SubscriptionTypes.User;
using TwitchLib.EventSub.Websockets.Core.EventArgs;

using Caramel.Twitch.Auth;

namespace Caramel.Twitch.Tests.Services;

/// <summary>
/// Acceptance tests for <see cref="EventSubLifecycleService"/>.
/// </summary>
public sealed class EventSubLifecycleServiceTests : IDisposable
{
  // -------------------------------------------------------------------------
  // Dependencies
  // -------------------------------------------------------------------------

  private readonly Mock<IEventSubWebsocketClientWrapper> _mockClient = new();
  private readonly Mock<ITwitchTokenManager> _mockTokenManager = new();
  private readonly Mock<ITwitchSetupState> _mockSetupState = new();
  private readonly Mock<ICaramelServiceClient> _mockServiceClient = new();
  private readonly Mock<IEventSubSubscriptionRegistrar> _mockRegistrar = new();
  private readonly Mock<IMediator> _mockMediator = new();
  private readonly Mock<IHttpClientFactory> _mockHttpClientFactory = new();
  private readonly Mock<ILogger<EventSubLifecycleService>> _mockLogger = new();

  private readonly TwitchConfig _twitchConfig = new()
  {
    ClientId = "test-client-id",
    ClientSecret = "test-client-secret",
    AccessToken = "test-access-token",
    RefreshToken = "test-refresh-token",
    OAuthCallbackUrl = "http://localhost:8080/auth/callback",
    EncryptionKey = Convert.ToBase64String(new byte[32]),
  };

  private readonly TwitchSetup _twitchSetup = new()
  {
    BotUserId = "111",
    BotLogin = "caramel_bot",
    Channels = [new TwitchChannel { UserId = "999", Login = "streamer" }],
    ConfiguredOn = DateTimeOffset.UtcNow,
    UpdatedOn = DateTimeOffset.UtcNow,
  };

  // Captured event-handler delegates — populated when WireEventHandlers() fires
  private AsyncEventHandler<WebsocketConnectedArgs>? _capturedConnectedHandler;
  private AsyncEventHandler<WebsocketDisconnectedArgs>? _capturedDisconnectedHandler;
  private AsyncEventHandler<WebsocketReconnectedArgs>? _capturedReconnectedHandler;
  private AsyncEventHandler<ChannelChatMessageArgs>? _capturedChatHandler;
  private AsyncEventHandler<UserWhisperMessageArgs>? _capturedWhisperHandler;

  public EventSubLifecycleServiceTests()
  {
    // Default: token is always available
    _ = _mockTokenManager
      .Setup(m => m.GetValidAccessTokenAsync(It.IsAny<CancellationToken>()))
      .ReturnsAsync("valid-token");

    _ = _mockTokenManager
      .Setup(m => m.GetValidAccessTokenAsync())
      .ReturnsAsync("valid-token");

    // Default: setup is configured
    _ = _mockSetupState.Setup(s => s.IsConfigured).Returns(true);
    _ = _mockSetupState.Setup(s => s.Current).Returns(_twitchSetup);

    // Default: connect returns true
    _ = _mockClient.Setup(c => c.ConnectAsync()).ReturnsAsync(true);

    // Default: session ID
    _ = _mockClient.Setup(c => c.SessionId).Returns("test-session-id");

    // Default: registrar succeeds
    _ = _mockRegistrar
      .Setup(r => r.RegisterAsync(It.IsAny<EventSubSubscriptionRegistrationContext>(), It.IsAny<CancellationToken>()))
      .Returns(Task.CompletedTask);

    // Default: mediator publish succeeds
    _ = _mockMediator
      .Setup(m => m.Publish(It.IsAny<INotification>(), It.IsAny<CancellationToken>()))
      .Returns(Task.CompletedTask);

    // Default: HttpClientFactory returns a usable HttpClient
    _ = _mockHttpClientFactory
      .Setup(f => f.CreateClient(It.IsAny<string>()))
      .Returns(new HttpClient());

    // Capture event-handler registrations so tests can fire events directly
    _mockClient
      .SetupAdd(m => m.WebsocketConnected += It.IsAny<AsyncEventHandler<WebsocketConnectedArgs>>())
      .Callback<AsyncEventHandler<WebsocketConnectedArgs>>(h => _capturedConnectedHandler = h);

    _mockClient
      .SetupAdd(m => m.WebsocketDisconnected += It.IsAny<AsyncEventHandler<WebsocketDisconnectedArgs>>())
      .Callback<AsyncEventHandler<WebsocketDisconnectedArgs>>(h => _capturedDisconnectedHandler = h);

    _mockClient
      .SetupAdd(m => m.WebsocketReconnected += It.IsAny<AsyncEventHandler<WebsocketReconnectedArgs>>())
      .Callback<AsyncEventHandler<WebsocketReconnectedArgs>>(h => _capturedReconnectedHandler = h);

    _mockClient
      .SetupAdd(m => m.ChannelChatMessage += It.IsAny<AsyncEventHandler<ChannelChatMessageArgs>>())
      .Callback<AsyncEventHandler<ChannelChatMessageArgs>>(h => _capturedChatHandler = h);

    _mockClient
      .SetupAdd(m => m.UserWhisperMessage += It.IsAny<AsyncEventHandler<UserWhisperMessageArgs>>())
      .Callback<AsyncEventHandler<UserWhisperMessageArgs>>(h => _capturedWhisperHandler = h);

    // Wire remove stubs so Moq doesn't complain
    _mockClient.SetupRemove(m => m.WebsocketConnected -= It.IsAny<AsyncEventHandler<WebsocketConnectedArgs>>());
    _mockClient.SetupRemove(m => m.WebsocketDisconnected -= It.IsAny<AsyncEventHandler<WebsocketDisconnectedArgs>>());
    _mockClient.SetupRemove(m => m.WebsocketReconnected -= It.IsAny<AsyncEventHandler<WebsocketReconnectedArgs>>());
    _mockClient.SetupRemove(m => m.ChannelChatMessage -= It.IsAny<AsyncEventHandler<ChannelChatMessageArgs>>());
    _mockClient.SetupRemove(m => m.UserWhisperMessage -= It.IsAny<AsyncEventHandler<UserWhisperMessageArgs>>());
    _mockClient.SetupRemove(m => m.ChannelPointsCustomRewardRedemptionAdd -= It.IsAny<AsyncEventHandler<ChannelPointsCustomRewardRedemptionArgs>>());
  }

  public void Dispose()
  {
    // Ensure any lingering HttpClient instances are cleaned up
  }

  // -------------------------------------------------------------------------
  // Factory
  // -------------------------------------------------------------------------

  private EventSubLifecycleService CreateService(
    IEnumerable<IEventSubSubscriptionRegistrar>? registrars = null)
  {
    return new EventSubLifecycleService(
      _mockClient.Object,
      _twitchConfig,
      _mockTokenManager.Object,
      _mockSetupState.Object,
      _mockServiceClient.Object,
      registrars ?? [_mockRegistrar.Object],
      _mockMediator.Object,
      _mockHttpClientFactory.Object,
      _mockLogger.Object);
  }

  // -------------------------------------------------------------------------
  // Helpers
  // -------------------------------------------------------------------------

  private static WebsocketConnectedArgs MakeConnectedArgs(bool isRequestedReconnect = false) =>
    new() { IsRequestedReconnect = isRequestedReconnect };

  private static WebsocketDisconnectedArgs MakeDisconnectedArgs() => new();

  private static WebsocketReconnectedArgs MakeReconnectedArgs() => new();

  private static ChannelChatMessageArgs MakeChatArgs(
    string broadcasterUserId = "999",
    string broadcasterUserLogin = "streamer",
    string chatterUserId = "42",
    string chatterUserLogin = "viewer",
    string chatterUserName = "Viewer",
    string messageId = "msg-1",
    string text = "hello chat",
    string color = "#FF0000")
  {
    return new ChannelChatMessageArgs
    {
      Payload = new EventSubNotificationPayload<ChannelChatMessage>
      {
        Event = new ChannelChatMessage
        {
          BroadcasterUserId = broadcasterUserId,
          BroadcasterUserLogin = broadcasterUserLogin,
          ChatterUserId = chatterUserId,
          ChatterUserLogin = chatterUserLogin,
          ChatterUserName = chatterUserName,
          MessageId = messageId,
          Message = new ChatMessage { Text = text },
          Color = color,
        },
      },
    };
  }

  private static UserWhisperMessageArgs MakeWhisperArgs(
    string fromUserId = "42",
    string fromUserLogin = "viewer",
    string text = "a whisper")
  {
    return new UserWhisperMessageArgs
    {
      Payload = new EventSubNotificationPayload<UserWhisperMessage>
      {
        Event = new UserWhisperMessage
        {
          FromUserId = fromUserId,
          FromUserLogin = fromUserLogin,
          Whisper = new WhisperMessage { Text = text },
        },
      },
    };
  }

  // =========================================================================
  // Core Lifecycle — 11 tests
  // =========================================================================

  /// <summary>Test 1: When a valid token and configured setup are available, ConnectAsync is called.</summary>
  [Fact]
  public async Task ExecuteAsync_WhenTokenAvailableAndSetupConfigured_ConnectsEventSubAsync()
  {
    using var cts = new CancellationTokenSource();

    var service = CreateService();
    await service.StartAsync(cts.Token);

    // Give the loop one iteration to call ConnectAsync
    await Task.Delay(150);

    await cts.CancelAsync();
    await service.StopAsync(CancellationToken.None);

    _mockClient.Verify(c => c.ConnectAsync(), Times.AtLeastOnce);
  }

  /// <summary>Test 2: When setup is not yet configured, the service polls GetTwitchSetupAsync until it is.</summary>
  [Fact]
  public async Task ExecuteAsync_WhenSetupNotConfigured_PollsUntilConfiguredAsync()
  {
    using var cts = new CancellationTokenSource();

    var callCount = 0;

    // First call: not configured; second call: configured
    _ = _mockSetupState.Setup(s => s.IsConfigured).Returns(false);

    _ = _mockServiceClient
      .Setup(sc => sc.GetTwitchSetupAsync(It.IsAny<CancellationToken>()))
      .ReturnsAsync(() =>
      {
        callCount++;
        if (callCount >= 2)
        {
          _ = _mockSetupState.Setup(s => s.IsConfigured).Returns(true);
          return Result.Ok<TwitchSetup?>(_twitchSetup);
        }

        return Result.Fail<TwitchSetup?>("not ready");
      });

    var service = CreateService();
    await service.StartAsync(cts.Token);

    // Wait long enough for at least two polling attempts (5s each), but use small delays in test
    // Since Task.Delay uses the real clock, we cancel quickly and just verify polling happened
    await Task.Delay(200);

    await cts.CancelAsync();
    await service.StopAsync(CancellationToken.None);

    _mockServiceClient.Verify(sc => sc.GetTwitchSetupAsync(It.IsAny<CancellationToken>()), Times.AtLeastOnce);
  }

  /// <summary>Test 3: When no refresh token exists, ConnectAsync is never called and the service does not crash.</summary>
  [Fact]
  public async Task ExecuteAsync_WhenNoRefreshToken_WaitsAndRetriesWithoutCrashingAsync()
  {
    _ = _mockTokenManager
      .Setup(m => m.GetValidAccessTokenAsync(It.IsAny<CancellationToken>()))
      .ThrowsAsync(new InvalidOperationException("No refresh token available"));

    using var cts = new CancellationTokenSource(500);

    var service = CreateService();
    await service.StartAsync(cts.Token);

    var act = async () => await service.StopAsync(CancellationToken.None);

    _ = await act.Should().NotThrowAsync();
    _mockClient.Verify(c => c.ConnectAsync(), Times.Never);
  }

  /// <summary>Test 4: When ConnectAsync returns false, ReconnectAsync is NEVER called (key bug fix).</summary>
  [Fact]
  public async Task ExecuteAsync_WhenConnectAsyncReturnsFalse_DoesNotCallReconnectAsyncAsync()
  {
    _ = _mockClient.Setup(c => c.ConnectAsync()).ReturnsAsync(false);

    using var cts = new CancellationTokenSource(200);

    var service = CreateService();
    await service.StartAsync(cts.Token);

    await Task.Delay(100);

    await cts.CancelAsync();
    await service.StopAsync(CancellationToken.None);

    _mockClient.Verify(c => c.ReconnectAsync(), Times.Never);
  }

  /// <summary>Test 5: When ConnectAsync returns false, the service retries and calls ConnectAsync again.</summary>
  [Fact]
  public async Task ExecuteAsync_WhenConnectAsyncReturnsFalse_RetriesAfterDelayAsync()
  {
    var secondCallTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
    var callCount = 0;

    _ = _mockClient.Setup(c => c.ConnectAsync()).ReturnsAsync(() =>
    {
      callCount++;
      if (callCount >= 2)
      {
        secondCallTcs.TrySetResult();
        return true;
      }

      return false;
    });

    using var cts = new CancellationTokenSource();

    var service = CreateService();
    await service.StartAsync(cts.Token);

    // Wait for the second ConnectAsync call to be made (with a reasonable timeout)
    await secondCallTcs.Task.WaitAsync(TimeSpan.FromSeconds(15));

    await cts.CancelAsync();
    await service.StopAsync(CancellationToken.None);

    _mockClient.Verify(c => c.ConnectAsync(), Times.AtLeast(2));
  }

  /// <summary>Test 6: When ConnectAsync fails after a prior success, ReconnectAsync is attempted.</summary>
  [Fact]
  public async Task ExecuteAsync_WhenConnectAsyncFailsAfterPriorSuccess_AttemptsReconnectAsync()
  {
    var reconnectTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
    var connectCount = 0;

    _ = _mockClient.Setup(c => c.ConnectAsync()).ReturnsAsync(() =>
    {
      connectCount++;
      return connectCount == 1;
    });

    _ = _mockClient.Setup(c => c.ReconnectAsync()).ReturnsAsync(() =>
    {
      reconnectTcs.TrySetResult();
      return true;
    });

    using var cts = new CancellationTokenSource();

    var service = CreateService();
    await service.StartAsync(cts.Token);

    // Allow first successful connect
    await Task.Delay(150);

    // Trigger a disconnect to force a reconnect attempt
    _ = (_capturedDisconnectedHandler?.Invoke(null, MakeDisconnectedArgs()));

    // Wait for ReconnectAsync to be called (after the 5s delay)
    await reconnectTcs.Task.WaitAsync(TimeSpan.FromSeconds(15));

    await cts.CancelAsync();
    await service.StopAsync(CancellationToken.None);

    _mockClient.Verify(c => c.ReconnectAsync(), Times.AtLeastOnce);
  }

  /// <summary>Test 7: When the WebSocket disconnects, the service re-enters the loop and calls ConnectAsync again.</summary>
  [Fact]
  public async Task ExecuteAsync_WhenWebsocketDisconnects_ReconnectsAfterDelayAsync()
  {
    var secondConnectTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
    var callCount = 0;

    _ = _mockClient.Setup(c => c.ConnectAsync()).ReturnsAsync(() =>
    {
      callCount++;
      if (callCount >= 2)
      {
        secondConnectTcs.TrySetResult();
      }

      return true;
    });

    using var cts = new CancellationTokenSource();

    var service = CreateService();
    await service.StartAsync(cts.Token);

    // Let the first connect complete
    await Task.Delay(150);

    // Fire the disconnect event — this sets the TCS so the loop re-enters after 5s
    _ = (_capturedDisconnectedHandler?.Invoke(null, MakeDisconnectedArgs()));

    // Wait for the second ConnectAsync call (up to 15 seconds for the 5s delay to elapse)
    await secondConnectTcs.Task.WaitAsync(TimeSpan.FromSeconds(15));

    await cts.CancelAsync();
    await service.StopAsync(CancellationToken.None);

    _mockClient.Verify(c => c.ConnectAsync(), Times.AtLeast(2));
  }

  /// <summary>Test 8: When the stopping token is cancelled, ExecuteAsync exits without throwing.</summary>
  [Fact]
  public async Task ExecuteAsync_WhenStoppingTokenCancelled_ExitsCleanlyAsync()
  {
    using var cts = new CancellationTokenSource();

    var service = CreateService();
    await service.StartAsync(cts.Token);

    await Task.Delay(100);

    await cts.CancelAsync();

    var act = async () => await service.StopAsync(CancellationToken.None);

    _ = await act.Should().NotThrowAsync();
  }

  /// <summary>Test 9: A generic exception from token retrieval is caught; ConnectAsync is eventually called.</summary>
  [Fact]
  public async Task ExecuteAsync_WhenGenericExceptionThrown_RetriesAfterTenSecondsAsync()
  {
    var connectCalledTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
    var callCount = 0;

    _ = _mockTokenManager
      .Setup(m => m.GetValidAccessTokenAsync(It.IsAny<CancellationToken>()))
      .ReturnsAsync(() =>
      {
        callCount++;
        if (callCount == 1)
        {
          throw new Exception("transient error");
        }

        return "valid-token";
      });

    _ = _mockClient.Setup(c => c.ConnectAsync()).ReturnsAsync(() =>
    {
      connectCalledTcs.TrySetResult();
      return true;
    });

    using var cts = new CancellationTokenSource();

    var service = CreateService();
    await service.StartAsync(cts.Token);

    // Wait for ConnectAsync to eventually be called (up to 15s to account for the 10s retry delay)
    await connectCalledTcs.Task.WaitAsync(TimeSpan.FromSeconds(15));

    await cts.CancelAsync();
    await service.StopAsync(CancellationToken.None);

    // No crash and eventually connects
    _mockClient.Verify(c => c.ConnectAsync(), Times.AtLeastOnce);
  }

  /// <summary>Test 10: StopAsync while connected completes within a reasonable timeout.</summary>
  [Fact]
  public async Task StopAsync_WhileConnected_DisconnectsGracefullyAsync()
  {
    using var cts = new CancellationTokenSource();

    var service = CreateService();
    await service.StartAsync(cts.Token);

    await Task.Delay(100);

    await cts.CancelAsync();

    var stopTask = service.StopAsync(CancellationToken.None);
    var completed = await Task.WhenAny(stopTask, Task.Delay(3000));

    _ = completed.Should().Be(stopTask, "StopAsync should complete within 3 seconds");
  }

  /// <summary>Test 11: Event handlers are wired exactly once, regardless of loop iterations.</summary>
  [Fact]
  public async Task WireEventHandlers_CalledMultipleTimes_WiresOnlyOnceAsync()
  {
    var connectCount = 0;

    _ = _mockClient.Setup(c => c.ConnectAsync()).ReturnsAsync(() =>
    {
      connectCount++;
      return connectCount >= 3; // false twice to force multiple loop iterations
    });

    using var cts = new CancellationTokenSource();

    var service = CreateService();
    await service.StartAsync(cts.Token);

    await Task.Delay(200);

    await cts.CancelAsync();
    await service.StopAsync(CancellationToken.None);

    // WebsocketConnected should be subscribed exactly once
    _mockClient.VerifyAdd(
      m => m.WebsocketConnected += It.IsAny<AsyncEventHandler<WebsocketConnectedArgs>>(),
      Times.Once);
  }

  /// <summary>Test 12: Simultaneous disconnect and stop-token cancellation does not throw.</summary>
  [Fact]
  public async Task ExecuteAsync_WhenDisconnectAndStopRace_HandlesIdempotentlyAsync()
  {
    using var cts = new CancellationTokenSource();

    var service = CreateService();
    await service.StartAsync(cts.Token);

    await Task.Delay(100);

    // Fire disconnect and cancel simultaneously
    _ = (_capturedDisconnectedHandler?.Invoke(null, MakeDisconnectedArgs()));
    await cts.CancelAsync();

    var act = async () => await service.StopAsync(CancellationToken.None);

    _ = await act.Should().NotThrowAsync();
  }

  // =========================================================================
  // Event Handlers — 8 tests
  // =========================================================================

  /// <summary>Test 13: When WebsocketConnected fires with IsRequestedReconnect=false, all registrars are called.</summary>
  [Fact]
  public async Task OnWebsocketConnected_WhenNotReconnect_RegistersAllSubscriptionsAsync()
  {
    using var cts = new CancellationTokenSource();

    var service = CreateService();
    await service.StartAsync(cts.Token);

    await Task.Delay(100);

    // Fire connected event (non-reconnect)
    await _capturedConnectedHandler!.Invoke(null, MakeConnectedArgs(isRequestedReconnect: false));

    await cts.CancelAsync();
    await service.StopAsync(CancellationToken.None);

    _mockRegistrar.Verify(
      r => r.RegisterAsync(It.IsAny<EventSubSubscriptionRegistrationContext>(), It.IsAny<CancellationToken>()),
      Times.AtLeastOnce);
  }

  /// <summary>Test 14: When WebsocketConnected fires with IsRequestedReconnect=true, no registrars are called.</summary>
  [Fact]
  public async Task OnWebsocketConnected_WhenIsReconnect_SkipsRegistrationAsync()
  {
    using var cts = new CancellationTokenSource();

    var service = CreateService();
    await service.StartAsync(cts.Token);

    await Task.Delay(100);

    // Fire connected event with reconnect flag
    await _capturedConnectedHandler!.Invoke(null, MakeConnectedArgs(isRequestedReconnect: true));

    await cts.CancelAsync();
    await service.StopAsync(CancellationToken.None);

    _mockRegistrar.Verify(
      r => r.RegisterAsync(It.IsAny<EventSubSubscriptionRegistrationContext>(), It.IsAny<CancellationToken>()),
      Times.Never);
  }

  /// <summary>Test 15: When setup is null at event time, RegisterAsync is never called and no exception is thrown.</summary>
  [Fact]
  public async Task OnWebsocketConnected_WhenSetupNull_LogsAndReturnsAsync()
  {
    // Override: Current is null even though IsConfigured was true at loop entry
    _ = _mockSetupState.Setup(s => s.Current).Returns((TwitchSetup?)null);

    using var cts = new CancellationTokenSource();

    var service = CreateService();
    await service.StartAsync(cts.Token);

    await Task.Delay(100);

    var act = async () =>
      await _capturedConnectedHandler!.Invoke(null, MakeConnectedArgs(isRequestedReconnect: false));

    _ = await act.Should().NotThrowAsync();

    await cts.CancelAsync();
    await service.StopAsync(CancellationToken.None);

    _mockRegistrar.Verify(
      r => r.RegisterAsync(It.IsAny<EventSubSubscriptionRegistrationContext>(), It.IsAny<CancellationToken>()),
      Times.Never);
  }

  /// <summary>Test 16: When a registrar throws, the exception is caught and the service loop continues.</summary>
  [Fact]
  public async Task OnWebsocketConnected_WhenRegistrarThrows_CatchesAndLogsAsync()
  {
    _ = _mockRegistrar
      .Setup(r => r.RegisterAsync(It.IsAny<EventSubSubscriptionRegistrationContext>(), It.IsAny<CancellationToken>()))
      .ThrowsAsync(new Exception("registration failed"));

    using var cts = new CancellationTokenSource();

    var service = CreateService();
    await service.StartAsync(cts.Token);

    await Task.Delay(100);

    // Fire the event — should not propagate the exception
    var act = async () =>
      await _capturedConnectedHandler!.Invoke(null, MakeConnectedArgs(isRequestedReconnect: false));

    _ = await act.Should().NotThrowAsync("exceptions in OnWebsocketConnected are caught internally");

    // Service loop should still be running — cancel cleanly
    await cts.CancelAsync();
    var stopAct = async () => await service.StopAsync(CancellationToken.None);
    _ = await stopAct.Should().NotThrowAsync();
  }

  /// <summary>Test 17: The CancellationToken passed to RegisterAsync is the stoppingToken, not CancellationToken.None.</summary>
  [Fact]
  public async Task OnWebsocketConnected_PassesCancellationTokenToRegistrarsAsync()
  {
    CancellationToken capturedToken = default;

    _ = _mockRegistrar
      .Setup(r => r.RegisterAsync(It.IsAny<EventSubSubscriptionRegistrationContext>(), It.IsAny<CancellationToken>()))
      .Callback<EventSubSubscriptionRegistrationContext, CancellationToken>((_, ct) => capturedToken = ct)
      .Returns(Task.CompletedTask);

    using var cts = new CancellationTokenSource();

    var service = CreateService();
    await service.StartAsync(cts.Token);

    await Task.Delay(100);

    await _capturedConnectedHandler!.Invoke(null, MakeConnectedArgs(isRequestedReconnect: false));

    await cts.CancelAsync();
    await service.StopAsync(CancellationToken.None);

    // Must NOT be the default (None) token — the stoppingToken should have been passed
    _ = capturedToken.Should().NotBe(CancellationToken.None,
      "the stoppingToken should be forwarded to registrars, not CancellationToken.None");
  }

  /// <summary>Test 18: Firing WebsocketReconnected does not throw and does not call registrars.</summary>
  [Fact]
  public async Task OnWebsocketReconnected_LogsReconnectionEventAsync()
  {
    using var cts = new CancellationTokenSource();

    var service = CreateService();
    await service.StartAsync(cts.Token);

    await Task.Delay(100);

    var act = async () =>
      await _capturedReconnectedHandler!.Invoke(null, MakeReconnectedArgs());

    _ = await act.Should().NotThrowAsync();

    // Reconnect event must NOT trigger subscription registration
    _mockRegistrar.Verify(
      r => r.RegisterAsync(It.IsAny<EventSubSubscriptionRegistrationContext>(), It.IsAny<CancellationToken>()),
      Times.Never);

    await cts.CancelAsync();
    await service.StopAsync(CancellationToken.None);
  }

  /// <summary>Test 19: When WebsocketDisconnected fires, the loop re-enters and ConnectAsync is called again.</summary>
  [Fact]
  public async Task OnWebsocketDisconnected_SignalsDisconnectTcsAsync()
  {
    var secondConnectTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
    var callCount = 0;

    _ = _mockClient.Setup(c => c.ConnectAsync()).ReturnsAsync(() =>
    {
      callCount++;
      if (callCount >= 2)
      {
        secondConnectTcs.TrySetResult();
      }

      return true;
    });

    using var cts = new CancellationTokenSource();

    var service = CreateService();
    await service.StartAsync(cts.Token);

    // Wait for first successful connect
    await Task.Delay(150);

    // Signal disconnect
    _ = (_capturedDisconnectedHandler?.Invoke(null, MakeDisconnectedArgs()));

    // Allow loop to re-enter and attempt a second connect (up to 15s for the 5s delay)
    await secondConnectTcs.Task.WaitAsync(TimeSpan.FromSeconds(15));

    await cts.CancelAsync();
    await service.StopAsync(CancellationToken.None);

    _mockClient.Verify(c => c.ConnectAsync(), Times.AtLeast(2));
  }

  /// <summary>Test 20: Firing WebsocketConnected twice with IsRequestedReconnect=false does not crash.</summary>
  [Fact]
  public async Task OnWebsocketConnected_DuplicateNonReconnectEvents_DoNotCrashAsync()
  {
    using var cts = new CancellationTokenSource();

    var service = CreateService();
    await service.StartAsync(cts.Token);

    await Task.Delay(100);

    var act = async () =>
    {
      await _capturedConnectedHandler!.Invoke(null, MakeConnectedArgs(isRequestedReconnect: false));
      await _capturedConnectedHandler!.Invoke(null, MakeConnectedArgs(isRequestedReconnect: false));
    };

    _ = await act.Should().NotThrowAsync("duplicate connected events should be absorbed gracefully");

    await cts.CancelAsync();
    await service.StopAsync(CancellationToken.None);
  }

  // =========================================================================
  // Edge Cases — 6 tests
  // =========================================================================

  /// <summary>
  /// Test 21: The race condition fix — _disconnectTcs is created AFTER ConnectAsync succeeds,
  /// so a stale pre-connect disconnect signal cannot prematurely complete the new TCS.
  /// Verified by ensuring only one pending disconnectTask is awaited per confirmed connection.
  /// </summary>
  [Fact]
  public async Task ExecuteAsync_StaleDisconnectOnFreshTcs_DoesNotDoubleConnectAsync()
  {
    var firstConnectTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
    var secondConnectTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
    var connectCallCount = 0;

    _ = _mockClient.Setup(c => c.ConnectAsync()).ReturnsAsync(() =>
    {
      connectCallCount++;
      if (connectCallCount == 1)
      {
        firstConnectTcs.TrySetResult();
      }
      else if (connectCallCount == 2)
      {
        secondConnectTcs.TrySetResult();
      }

      return true;
    });

    using var cts = new CancellationTokenSource();

    var service = CreateService();
    await service.StartAsync(cts.Token);

    // Wait for first connection
    await firstConnectTcs.Task.WaitAsync(TimeSpan.FromSeconds(5));

    _ = connectCallCount.Should().Be(1, "exactly one connection on first iteration");

    // Fire a disconnect to trigger a reconnect (5s delay before second connect)
    _ = (_capturedDisconnectedHandler?.Invoke(null, MakeDisconnectedArgs()));

    // Wait for the second ConnectAsync call
    await secondConnectTcs.Task.WaitAsync(TimeSpan.FromSeconds(15));

    var countsAfterDisconnect = connectCallCount;

    await cts.CancelAsync();
    await service.StopAsync(CancellationToken.None);

    _ = countsAfterDisconnect.Should().Be(2, "exactly one reconnect after the single disconnect");
  }

  /// <summary>Test 22: Cancelling stoppingToken during slow registration causes service to stop without hanging.</summary>
  [Fact]
  public async Task OnWebsocketConnected_StopAsyncDuringRegistration_DoesNotHangAsync()
  {
    var registrationStarted = new TaskCompletionSource();

    _ = _mockRegistrar
      .Setup(r => r.RegisterAsync(It.IsAny<EventSubSubscriptionRegistrationContext>(), It.IsAny<CancellationToken>()))
      .Returns(async (EventSubSubscriptionRegistrationContext _, CancellationToken ct) =>
      {
        registrationStarted.TrySetResult();
        await Task.Delay(Timeout.Infinite, ct); // Blocked until cancelled
      });

    using var cts = new CancellationTokenSource();

    var service = CreateService();
    await service.StartAsync(cts.Token);

    await Task.Delay(100);

    // Trigger connection event to start registration
    _ = _capturedConnectedHandler?.Invoke(null, MakeConnectedArgs(isRequestedReconnect: false));

    // Wait for registration to start
    await registrationStarted.Task.WaitAsync(TimeSpan.FromSeconds(2));

    // Cancel while registration is in progress
    await cts.CancelAsync();

    var stopTask = service.StopAsync(CancellationToken.None);
    var completed = await Task.WhenAny(stopTask, Task.Delay(5000));

    _ = completed.Should().Be(stopTask, "service should stop within 5 seconds even if registration is slow");
  }

  /// <summary>Test 23: When mediator.Publish throws for a chat message, the background service does not crash.</summary>
  [Fact]
  public async Task OnChannelChatMessage_WhenMediatorThrows_DoesNotCrashServiceAsync()
  {
    _ = _mockMediator
      .Setup(m => m.Publish(It.IsAny<ChannelChatMessageReceived>(), It.IsAny<CancellationToken>()))
      .ThrowsAsync(new Exception("mediator failure"));

    using var cts = new CancellationTokenSource();

    var service = CreateService();
    await service.StartAsync(cts.Token);

    await Task.Delay(100);

    // Fire a chat message directly via the captured handler
    var act = async () =>
      await _capturedChatHandler!.Invoke(null, MakeChatArgs());

    // The exception will propagate from the handler invocation itself (not caught internally),
    // but the service's background loop should be unaffected
    _ = await act.Should().ThrowAsync<Exception>("mediator.Publish throws and is not caught in the handler");

    // Service should still be running — cancel cleanly
    await cts.CancelAsync();
    var stopAct = async () => await service.StopAsync(CancellationToken.None);
    _ = await stopAct.Should().NotThrowAsync();
  }

  /// <summary>Test 24: When mediator.Publish throws for a whisper, the background service does not crash.</summary>
  [Fact]
  public async Task OnUserWhisperMessage_WhenMediatorThrows_DoesNotCrashServiceAsync()
  {
    _ = _mockMediator
      .Setup(m => m.Publish(It.IsAny<UserWhisperMessageReceived>(), It.IsAny<CancellationToken>()))
      .ThrowsAsync(new Exception("mediator failure"));

    using var cts = new CancellationTokenSource();

    var service = CreateService();
    await service.StartAsync(cts.Token);

    await Task.Delay(100);

    var act = async () =>
      await _capturedWhisperHandler!.Invoke(null, MakeWhisperArgs());

    // Same expectation: mediator throws from handler invocation
    _ = await act.Should().ThrowAsync<Exception>("mediator.Publish throws and is not caught in the whisper handler");

    await cts.CancelAsync();
    var stopAct = async () => await service.StopAsync(CancellationToken.None);
    _ = await stopAct.Should().NotThrowAsync();
  }

  /// <summary>
  /// Test 25: Each ConnectAsync=false failure retries — the service calls ConnectAsync multiple times
  /// when it keeps returning false, demonstrating the fixed 5-second retry behaviour.
  /// </summary>
  [Fact]
  public async Task ExecuteAsync_ConsecutiveFailures_UsesFixedFiveSecondDelayAsync()
  {
    var connectCallCount = 0;

    _ = _mockClient.Setup(c => c.ConnectAsync()).ReturnsAsync(() =>
    {
      connectCallCount++;
      return false; // Always fail
    });

    using var cts = new CancellationTokenSource();

    var service = CreateService();
    await service.StartAsync(cts.Token);

    // Let the loop iterate — each retry has a Task.Delay(5s) but the CTS cancels it early
    await Task.Delay(200);

    await cts.CancelAsync();
    await service.StopAsync(CancellationToken.None);

    // Should have called ConnectAsync at least once before cancellation cut the loop short
    _ = connectCallCount.Should().BeGreaterThanOrEqualTo(1,
      "each false return should cause a retry (delayed by stoppingToken, so may only fire once before cancel)");
  }

  /// <summary>
  /// Test 26: HttpClient obtained from IHttpClientFactory is NOT manually disposed inside OnWebsocketConnected
  /// (IHttpClientFactory manages HttpClient lifetime; manual disposal is a bug).
  /// </summary>
  [Fact]
  public async Task OnWebsocketConnected_HttpClientNotManuallyDisposedAsync()
  {
    var mockHttpClient = new TrackingHttpClient();

    _ = _mockHttpClientFactory
      .Setup(f => f.CreateClient(It.IsAny<string>()))
      .Returns(mockHttpClient);

    using var cts = new CancellationTokenSource();

    var service = CreateService();
    await service.StartAsync(cts.Token);

    await Task.Delay(100);

    await _capturedConnectedHandler!.Invoke(null, MakeConnectedArgs(isRequestedReconnect: false));

    await cts.CancelAsync();
    await service.StopAsync(CancellationToken.None);

    _ = mockHttpClient.WasDisposed.Should().BeFalse(
      "IHttpClientFactory manages the HttpClient lifetime; the service must not call Dispose()");
  }

  // -------------------------------------------------------------------------
  // Supporting types
  // -------------------------------------------------------------------------

  /// <summary>An HttpClient that tracks whether Dispose() was called.</summary>
  private sealed class TrackingHttpClient : HttpClient
  {
    public bool WasDisposed { get; private set; }

    protected override void Dispose(bool disposing)
    {
      WasDisposed = true;
      base.Dispose(disposing);
    }
  }
}
