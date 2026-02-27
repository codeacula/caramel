using MediatR;

using Microsoft.Extensions.Logging;

using TwitchLib.EventSub.Core;
using TwitchLib.EventSub.Core.EventArgs.Channel;
using TwitchLib.EventSub.Core.EventArgs.User;
using TwitchLib.EventSub.Core.Models;
using TwitchLib.EventSub.Websockets.Core.EventArgs;

using Caramel.Domain.Twitch;
using Caramel.Twitch.Auth;

namespace Caramel.Twitch.Tests.Services;

/// <summary>
/// Acceptance tests for UoW-4: EventSub Token Routing.
/// Verifies that the correct token (bot or broadcaster) is routed to each subscription type.
/// </summary>
public sealed class EventSubTokenRoutingTests : IDisposable
{
  private readonly Mock<IEventSubWebsocketClientWrapper> _mockClient = new();
  private readonly Mock<IDualOAuthTokenManager> _mockTokenManager = new();
  private readonly Mock<ITwitchSetupState> _mockSetupState = new();
  private readonly Mock<ICaramelServiceClient> _mockServiceClient = new();
  private readonly Mock<IEventSubSubscriptionClient> _mockEventSubSubscriptionClient = new();
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
    BotTokens = new TwitchAccountTokens
    {
      UserId = "111",
      Login = "caramel_bot",
      AccessToken = "bot-token-123",
      RefreshToken = "bot-refresh",
      ExpiresAt = DateTime.UtcNow.AddHours(1),
      LastRefreshedOn = DateTimeOffset.UtcNow,
    },
    BroadcasterTokens = new TwitchAccountTokens
    {
      UserId = "999",
      Login = "streamer",
      AccessToken = "broadcaster-token-456",
      RefreshToken = "broadcaster-refresh",
      ExpiresAt = DateTime.UtcNow.AddHours(1),
      LastRefreshedOn = DateTimeOffset.UtcNow,
    },
  };

  private AsyncEventHandler<WebsocketConnectedArgs>? _capturedConnectedHandler;

  public EventSubTokenRoutingTests()
  {
    // Setup token manager with both tokens available
    _ = _mockTokenManager
      .Setup(m => m.InitializeAsync(It.IsAny<CancellationToken>()))
      .Returns(Task.CompletedTask);

    _ = _mockTokenManager
      .Setup(m => m.GetValidBotTokenAsync(It.IsAny<CancellationToken>()))
      .ReturnsAsync("bot-token-123");

    _ = _mockTokenManager
      .Setup(m => m.GetValidBotTokenAsync())
      .ReturnsAsync("bot-token-123");

    _ = _mockTokenManager
      .Setup(m => m.GetValidBroadcasterTokenAsync(It.IsAny<CancellationToken>()))
      .ReturnsAsync("broadcaster-token-456");

    _ = _mockTokenManager
      .Setup(m => m.GetCurrentBotAccessToken())
      .Returns("bot-token-123");

    _ = _mockTokenManager
      .Setup(m => m.GetCurrentBroadcasterAccessToken())
      .Returns("broadcaster-token-456");

    // Setup state
    _ = _mockSetupState.Setup(s => s.IsConfigured).Returns(true);
    _ = _mockSetupState.Setup(s => s.Current).Returns(_twitchSetup);

    // Setup client
    _ = _mockClient.Setup(c => c.ConnectAsync()).ReturnsAsync(true);
    _ = _mockClient.Setup(c => c.SessionId).Returns("test-session-id");

    _mockClient
      .SetupAdd(m => m.WebsocketConnected += It.IsAny<AsyncEventHandler<WebsocketConnectedArgs>>())
      .Callback<AsyncEventHandler<WebsocketConnectedArgs>>(h => _capturedConnectedHandler = h);

    _mockClient.SetupRemove(m => m.WebsocketConnected -= It.IsAny<AsyncEventHandler<WebsocketConnectedArgs>>());
    _mockClient.SetupRemove(m => m.WebsocketDisconnected -= It.IsAny<AsyncEventHandler<WebsocketDisconnectedArgs>>());
    _mockClient.SetupRemove(m => m.WebsocketReconnected -= It.IsAny<AsyncEventHandler<WebsocketReconnectedArgs>>());
    _mockClient.SetupRemove(m => m.ChannelChatMessage -= It.IsAny<AsyncEventHandler<ChannelChatMessageArgs>>());
    _mockClient.SetupRemove(m => m.UserWhisperMessage -= It.IsAny<AsyncEventHandler<UserWhisperMessageArgs>>());
    _mockClient.SetupRemove(m => m.ChannelPointsCustomRewardRedemptionAdd -= It.IsAny<AsyncEventHandler<ChannelPointsCustomRewardRedemptionArgs>>());

    // Setup mediator
    _ = _mockMediator
      .Setup(m => m.Publish(It.IsAny<INotification>(), It.IsAny<CancellationToken>()))
      .Returns(Task.CompletedTask);

    // Setup HTTP factory
    _ = _mockHttpClientFactory
      .Setup(f => f.CreateClient(It.IsAny<string>()))
      .Returns(new HttpClient());

    // Setup service client
    _ = _mockServiceClient
      .Setup(sc => sc.GetTwitchSetupAsync(It.IsAny<CancellationToken>()))
      .ReturnsAsync(FluentResults.Result.Ok(_twitchSetup));
  }

  public void Dispose()
  {
  }

  /// <summary>
  /// Test 1: Verifies that ChannelChatMessageSubscriptionRegistrar receives bot token.
  /// </summary>
  [Fact]
  public async Task ChannelChatMessageRegistrarUsesBoTTokenAsync()
  {
    var registrationContext = (EventSubSubscriptionRegistrationContext?)null;
    var chatRegistrar = new Mock<IEventSubSubscriptionRegistrar>();
    chatRegistrar
      .Setup(r => r.RegisterAsync(It.IsAny<EventSubSubscriptionRegistrationContext>(), It.IsAny<CancellationToken>()))
      .Callback<EventSubSubscriptionRegistrationContext, CancellationToken>((ctx, _) => registrationContext = ctx)
      .Returns(Task.CompletedTask);

    var service = new EventSubLifecycleService(
      _mockClient.Object,
      _twitchConfig,
      _mockTokenManager.Object,
      _mockSetupState.Object,
      _mockServiceClient.Object,
      [chatRegistrar.Object],
      _mockMediator.Object,
      _mockHttpClientFactory.Object,
      _mockLogger.Object);

    using var cts = new CancellationTokenSource(2000);
    _ = service.StartAsync(cts.Token);

    // Wait for service to initialize
    await Task.Delay(300);

    // Fire connected event
    if (_capturedConnectedHandler is not null)
    {
      var args = new WebsocketConnectedArgs();
      await _capturedConnectedHandler.Invoke(null, args);
    }

    // Verify registrar was called with bot token
    chatRegistrar.Verify(
      r => r.RegisterAsync(It.IsAny<EventSubSubscriptionRegistrationContext>(), It.IsAny<CancellationToken>()),
      Times.Once);

    registrationContext.Should().NotBeNull();
    registrationContext!.BotAccessToken.Should().Be("bot-token-123");
  }

  /// <summary>
  /// Test 2: Verifies that ChannelPointRedeemSubscriptionRegistrar receives broadcaster token.
  /// </summary>
  [Fact]
  public async Task ChannelPointRedeemRegistrarUsesBroadcasterTokenAsync()
  {
    var registrationContext = (EventSubSubscriptionRegistrationContext?)null;
    var pointsRegistrar = new Mock<IEventSubSubscriptionRegistrar>();
    pointsRegistrar
      .Setup(r => r.RegisterAsync(It.IsAny<EventSubSubscriptionRegistrationContext>(), It.IsAny<CancellationToken>()))
      .Callback<EventSubSubscriptionRegistrationContext, CancellationToken>((ctx, _) => registrationContext = ctx)
      .Returns(Task.CompletedTask);

    var service = new EventSubLifecycleService(
      _mockClient.Object,
      _twitchConfig,
      _mockTokenManager.Object,
      _mockSetupState.Object,
      _mockServiceClient.Object,
      [pointsRegistrar.Object],
      _mockMediator.Object,
      _mockHttpClientFactory.Object,
      _mockLogger.Object);

    using var cts = new CancellationTokenSource(2000);
    _ = service.StartAsync(cts.Token);

    // Wait for service to initialize
    await Task.Delay(300);

    // Fire connected event
    if (_capturedConnectedHandler is not null)
    {
      var args = new WebsocketConnectedArgs();
      await _capturedConnectedHandler.Invoke(null, args);
    }

    // Verify registrar was called with broadcaster token
    pointsRegistrar.Verify(
      r => r.RegisterAsync(It.IsAny<EventSubSubscriptionRegistrationContext>(), It.IsAny<CancellationToken>()),
      Times.Once);

    registrationContext.Should().NotBeNull();
    registrationContext!.BroadcasterAccessToken.Should().Be("broadcaster-token-456");
  }

  /// <summary>
  /// Test 3: Verifies graceful degradation when broadcaster token is not available.
  /// Bot-only subscriptions should still work; broadcaster subscriptions should be skipped.
  /// </summary>
  [Fact]
  public async Task GracefullyHandlesNoBroadcasterTokenAsync()
  {
    // Setup: broadcaster token is unavailable
    _ = _mockTokenManager
      .Setup(m => m.GetValidBroadcasterTokenAsync(It.IsAny<CancellationToken>()))
      .ThrowsAsync(new InvalidOperationException("No broadcaster token available"));

    _ = _mockTokenManager
      .Setup(m => m.GetCurrentBroadcasterAccessToken())
      .Returns((string?)null);

    var registrationContext = (EventSubSubscriptionRegistrationContext?)null;
    var pointsRegistrar = new Mock<IEventSubSubscriptionRegistrar>();
    pointsRegistrar
      .Setup(r => r.RegisterAsync(It.IsAny<EventSubSubscriptionRegistrationContext>(), It.IsAny<CancellationToken>()))
      .Callback<EventSubSubscriptionRegistrationContext, CancellationToken>((ctx, _) => registrationContext = ctx)
      .Returns(Task.CompletedTask);

    var service = new EventSubLifecycleService(
      _mockClient.Object,
      _twitchConfig,
      _mockTokenManager.Object,
      _mockSetupState.Object,
      _mockServiceClient.Object,
      [pointsRegistrar.Object],
      _mockMediator.Object,
      _mockHttpClientFactory.Object,
      _mockLogger.Object);

    using var cts = new CancellationTokenSource(2000);
    _ = service.StartAsync(cts.Token);

    // Wait for service to initialize
    await Task.Delay(300);

    // Fire connected event
    if (_capturedConnectedHandler is not null)
    {
      var args = new WebsocketConnectedArgs();
      await _capturedConnectedHandler.Invoke(null, args);
    }

    // Verify registrar was called
    pointsRegistrar.Verify(
      r => r.RegisterAsync(It.IsAny<EventSubSubscriptionRegistrationContext>(), It.IsAny<CancellationToken>()),
      Times.Once);

    // Verify broadcaster token is null in context
    registrationContext.Should().NotBeNull();
    registrationContext!.BroadcasterAccessToken.Should().BeNull();
    registrationContext!.BotAccessToken.Should().Be("bot-token-123");
  }

  /// <summary>
  /// Test 4: Verifies that multiple registrars are called with correct context.
  /// </summary>
  [Fact]
  public async Task MultipleRegistrarsCalledWithCorrectContextAsync()
  {
    var chatRegistrationContext = (EventSubSubscriptionRegistrationContext?)null;
    var pointsRegistrationContext = (EventSubSubscriptionRegistrationContext?)null;

    var chatRegistrar = new Mock<IEventSubSubscriptionRegistrar>();
    chatRegistrar
      .Setup(r => r.RegisterAsync(It.IsAny<EventSubSubscriptionRegistrationContext>(), It.IsAny<CancellationToken>()))
      .Callback<EventSubSubscriptionRegistrationContext, CancellationToken>((ctx, _) => chatRegistrationContext = ctx)
      .Returns(Task.CompletedTask);

    var pointsRegistrar = new Mock<IEventSubSubscriptionRegistrar>();
    pointsRegistrar
      .Setup(r => r.RegisterAsync(It.IsAny<EventSubSubscriptionRegistrationContext>(), It.IsAny<CancellationToken>()))
      .Callback<EventSubSubscriptionRegistrationContext, CancellationToken>((ctx, _) => pointsRegistrationContext = ctx)
      .Returns(Task.CompletedTask);

    var service = new EventSubLifecycleService(
      _mockClient.Object,
      _twitchConfig,
      _mockTokenManager.Object,
      _mockSetupState.Object,
      _mockServiceClient.Object,
      [chatRegistrar.Object, pointsRegistrar.Object],
      _mockMediator.Object,
      _mockHttpClientFactory.Object,
      _mockLogger.Object);

    using var cts = new CancellationTokenSource(2000);
    _ = service.StartAsync(cts.Token);

    // Wait for service to initialize
    await Task.Delay(300);

    // Fire connected event
    if (_capturedConnectedHandler is not null)
    {
      var args = new WebsocketConnectedArgs();
      await _capturedConnectedHandler.Invoke(null, args);
    }

    // Verify both registrars were called
    chatRegistrar.Verify(
      r => r.RegisterAsync(It.IsAny<EventSubSubscriptionRegistrationContext>(), It.IsAny<CancellationToken>()),
      Times.Once);

    pointsRegistrar.Verify(
      r => r.RegisterAsync(It.IsAny<EventSubSubscriptionRegistrationContext>(), It.IsAny<CancellationToken>()),
      Times.Once);

    // Verify both received the correct tokens
    chatRegistrationContext.Should().NotBeNull();
    chatRegistrationContext!.BotAccessToken.Should().Be("bot-token-123");
    chatRegistrationContext!.BroadcasterAccessToken.Should().Be("broadcaster-token-456");

    pointsRegistrationContext.Should().NotBeNull();
    pointsRegistrationContext!.BotAccessToken.Should().Be("bot-token-123");
    pointsRegistrationContext!.BroadcasterAccessToken.Should().Be("broadcaster-token-456");
  }

  /// <summary>
  /// Test 5: Verifies that broadcaster ID is included in registration context.
  /// </summary>
  [Fact]
  public async Task BroadcasterUserIdIncludedInContextAsync()
  {
    var registrationContext = (EventSubSubscriptionRegistrationContext?)null;
    var registrar = new Mock<IEventSubSubscriptionRegistrar>();
    registrar
      .Setup(r => r.RegisterAsync(It.IsAny<EventSubSubscriptionRegistrationContext>(), It.IsAny<CancellationToken>()))
      .Callback<EventSubSubscriptionRegistrationContext, CancellationToken>((ctx, _) => registrationContext = ctx)
      .Returns(Task.CompletedTask);

    var service = new EventSubLifecycleService(
      _mockClient.Object,
      _twitchConfig,
      _mockTokenManager.Object,
      _mockSetupState.Object,
      _mockServiceClient.Object,
      [registrar.Object],
      _mockMediator.Object,
      _mockHttpClientFactory.Object,
      _mockLogger.Object);

    using var cts = new CancellationTokenSource(2000);
    _ = service.StartAsync(cts.Token);

    // Wait for service to initialize
    await Task.Delay(300);

    // Fire connected event
    if (_capturedConnectedHandler is not null)
    {
      var args = new WebsocketConnectedArgs();
      await _capturedConnectedHandler.Invoke(null, args);
    }

    // Verify context includes broadcaster user ID
    registrationContext.Should().NotBeNull();
    registrationContext!.BroadcasterUserId.Should().Be("999"); // From the channel in _twitchSetup
  }
}
