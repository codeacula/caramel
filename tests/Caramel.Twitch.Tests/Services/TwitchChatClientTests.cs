namespace Caramel.Twitch.Tests.Services;

public sealed class TwitchChatClientTests
{
  private static readonly TwitchConfig DefaultConfig = new()
  {
    ClientId = "test-client-id",
    ClientSecret = "test-client-secret",
    AccessToken = "initial-access-token",
    RefreshToken = "initial-refresh-token",
    OAuthCallbackUrl = "http://localhost:5146/auth/twitch/callback",
    EncryptionKey = Convert.ToBase64String(new byte[32]),
  };

  private readonly Mock<ITwitchSetupState> _setupStateMock = new();
  private readonly Mock<IHttpClientFactory> _httpClientFactoryMock = new();
  private readonly Mock<ILogger<TwitchChatClient>> _loggerMock = new();

  [Fact]
  public async Task SendChatMessageAsyncReturnsSuccessWhenHelixReturnsOkAsync()
  {
    // Arrange
    var handler = new FakeHttpMessageHandler(HttpStatusCode.OK, "{}");
    var client = CreateClient(handler);
    SetupConfiguredState();

    // Act
    var result = await client.SendChatMessageAsync("Hello chat!", CancellationToken.None);

    // Assert
    _ = result.IsSuccess.Should().BeTrue();
  }

  [Fact]
  public async Task SendChatMessageAsyncPostsCorrectPayloadToHelixApiAsync()
  {
    // Arrange
    var handler = new FakeHttpMessageHandler(HttpStatusCode.OK, "{}");
    var client = CreateClient(handler);
    SetupConfiguredState(broadcasterId: "broadcaster-99", botUserId: "bot-42");

    // Act
    _ = await client.SendChatMessageAsync("Test message", CancellationToken.None);

    // Assert
    _ = handler.LastRequest.Should().NotBeNull();
    _ = handler.LastRequest!.RequestUri!.ToString().Should().Be("https://api.twitch.tv/helix/chat/messages");
    _ = handler.LastRequest.Method.Should().Be(HttpMethod.Post);

    var body = JsonSerializer.Deserialize<JsonElement>(handler.LastRequestBody!);
    _ = body.GetProperty("broadcaster_id").GetString().Should().Be("broadcaster-99");
    _ = body.GetProperty("sender_id").GetString().Should().Be("bot-42");
    _ = body.GetProperty("message").GetString().Should().Be("Test message");
  }

  [Fact]
  public async Task SendChatMessageAsyncSetsAuthorizationHeadersAsync()
  {
    // Arrange
    var handler = new FakeHttpMessageHandler(HttpStatusCode.OK, "{}");
    var client = CreateClient(handler);
    SetupConfiguredState();

    // Act
    _ = await client.SendChatMessageAsync("Hello", CancellationToken.None);

    // Assert
    _ = handler.LastRequest.Should().NotBeNull();
    _ = handler.LastRequest!.Headers.GetValues("Client-Id").Should().Contain("test-client-id");
    _ = handler.LastRequest.Headers.GetValues("Authorization").Should().Contain("Bearer initial-access-token");
  }

  [Fact]
  public async Task SendChatMessageAsyncReturnsFailureWhenSetupNotConfiguredAsync()
  {
    // Arrange
    _ = _setupStateMock.Setup(x => x.Current).Returns((TwitchSetup?)null);
    var handler = new FakeHttpMessageHandler(HttpStatusCode.OK, "{}");
    var client = CreateClient(handler);

    // Act
    var result = await client.SendChatMessageAsync("Hello", CancellationToken.None);

    // Assert
    _ = result.IsFailed.Should().BeTrue();
    _ = handler.LastRequest.Should().BeNull("no HTTP request should have been made");
  }

  [Fact]
  public async Task SendChatMessageAsyncReturnsFailureWhenNoChannelsConfiguredAsync()
  {
    // Arrange
    var setup = new TwitchSetup
    {
      BotUserId = "bot-42",
      BotLogin = "testbot",
      Channels = [],
      ConfiguredOn = DateTimeOffset.UtcNow,
      UpdatedOn = DateTimeOffset.UtcNow,
    };
    _ = _setupStateMock.Setup(x => x.Current).Returns(setup);
    var handler = new FakeHttpMessageHandler(HttpStatusCode.OK, "{}");
    var client = CreateClient(handler);

    // Act
    var result = await client.SendChatMessageAsync("Hello", CancellationToken.None);

    // Assert
    _ = result.IsFailed.Should().BeTrue();
    _ = handler.LastRequest.Should().BeNull("no HTTP request should have been made");
  }

  [Fact]
  public async Task SendChatMessageAsyncReturnsFailureWhenHelixReturnsNonSuccessStatusCodeAsync()
  {
    // Arrange
    var handler = new FakeHttpMessageHandler(HttpStatusCode.UnprocessableEntity, /*lang=json,strict*/ """{"error":"Message too large"}""");
    var client = CreateClient(handler);
    SetupConfiguredState();

    // Act
    var result = await client.SendChatMessageAsync("Hello", CancellationToken.None);

    // Assert
    _ = result.IsFailed.Should().BeTrue();
  }

  [Fact]
  public async Task SendChatMessageAsyncReturnsFailureWhenTokenManagerThrowsAsync()
  {
    // Arrange — create token manager with expired token and no refresh capability
    var expiredConfig = DefaultConfig with
    {
      AccessToken = "expired-token",
      RefreshToken = null!,
    };
    var tokenManager = new TwitchTokenManager(expiredConfig, _httpClientFactoryMock.Object, new Mock<ILogger<TwitchTokenManager>>().Object);
    tokenManager.SetTokens("expired-token", null, expiresInSeconds: 0);

    var handler = new FakeHttpMessageHandler(HttpStatusCode.OK, "{}");
    var httpClient = new HttpClient(handler);
    _ = _httpClientFactoryMock
      .Setup(f => f.CreateClient(It.IsAny<string>()))
      .Returns(httpClient);
    SetupConfiguredState();

    var client = new TwitchChatClient(
      _httpClientFactoryMock.Object,
      DefaultConfig,
      tokenManager,
      _setupStateMock.Object,
      _loggerMock.Object);

    // Act
    var result = await client.SendChatMessageAsync("Hello", CancellationToken.None);

    // Assert
    _ = result.IsFailed.Should().BeTrue();
  }

  private TwitchChatClient CreateClient(FakeHttpMessageHandler handler)
  {
    var httpClient = new HttpClient(handler);
    _ = _httpClientFactoryMock
      .Setup(f => f.CreateClient(It.IsAny<string>()))
      .Returns(httpClient);

    var tokenManager = new TwitchTokenManager(DefaultConfig, _httpClientFactoryMock.Object, new Mock<ILogger<TwitchTokenManager>>().Object);

    return new TwitchChatClient(
      _httpClientFactoryMock.Object,
      DefaultConfig,
      tokenManager,
      _setupStateMock.Object,
      _loggerMock.Object);
  }

  private void SetupConfiguredState(string broadcasterId = "broadcaster-1", string botUserId = "bot-1")
  {
    var setup = new TwitchSetup
    {
      BotUserId = botUserId,
      BotLogin = "testbot",
      Channels = [new TwitchChannel { UserId = broadcasterId, Login = "testchannel" }],
      ConfiguredOn = DateTimeOffset.UtcNow,
      UpdatedOn = DateTimeOffset.UtcNow,
    };
    _ = _setupStateMock.Setup(x => x.Current).Returns(setup);
  }
}

internal sealed class FakeHttpMessageHandler(HttpStatusCode statusCode, string responseBody) : HttpMessageHandler
{
  public HttpRequestMessage? LastRequest { get; private set; }
  public string? LastRequestBody { get; private set; }

  protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
  {
    LastRequest = request;
    LastRequestBody = request.Content is not null
      ? await request.Content.ReadAsStringAsync(cancellationToken)
      : null;
    return new HttpResponseMessage(statusCode)
    {
      Content = new StringContent(responseBody, Encoding.UTF8, "application/json"),
    };
  }
}
