namespace Caramel.Twitch.Tests.Controllers;

public sealed class AdsControllerTests
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

  private readonly Mock<ITwitchSetupState> _mockSetupState = new();
  private readonly Mock<IHttpClientFactory> _mockHttpClientFactory = new();
  private readonly Mock<ILogger<AdsController>> _mockLogger = new();
  private readonly Mock<IAdsCoordinator> _mockAdsCoordinator = new();

  public AdsControllerTests()
  {
    // Default: not on cooldown
    _ = _mockAdsCoordinator.Setup(c => c.IsOnCooldown()).Returns(false);
  }

  private AdsController CreateController(HttpMessageHandler? handler = null)
  {
    handler ??= new FakeHttpMessageHandler(HttpStatusCode.OK, "{}");
    var httpClient = new HttpClient(handler);
    _ = _mockHttpClientFactory
        .Setup(f => f.CreateClient(It.IsAny<string>()))
        .Returns(httpClient);
    var tokenManager = new TwitchTokenManager(DefaultConfig, _mockHttpClientFactory.Object, new Mock<ILogger<TwitchTokenManager>>().Object);
    return new AdsController(tokenManager, DefaultConfig, _mockSetupState.Object, _mockHttpClientFactory.Object, _mockAdsCoordinator.Object, _mockLogger.Object);
  }

  private static TwitchSetup MakeSetup(string broadcasterId = "channel123")
  {
    return new()
    {
      BotUserId = "bot123",
      BotLogin = "testbot",
      Channels = [new TwitchChannel { UserId = broadcasterId, Login = "testchannel" }],
      ConfiguredOn = DateTimeOffset.UtcNow,
      UpdatedOn = DateTimeOffset.UtcNow,
    };
  }

  // --- Existing tests ---

  [Fact]
  public async Task RunAdsAsyncWhenSetupIsNullReturnsServiceUnavailableAsync()
  {
    // Arrange
    _ = _mockSetupState.Setup(s => s.Current).Returns((TwitchSetup?)null);
    var controller = CreateController();
    var request = new RunAdsRequest { Duration = 30 };

    // Act
    var result = await controller.RunAdsAsync(request, CancellationToken.None);

    // Assert
    _ = result.Should().BeOfType<ObjectResult>()
        .Which.StatusCode.Should().Be(503);
  }

  [Fact]
  public async Task RunAdsAsyncWhenNoChannelsConfiguredReturnsBadRequestAsync()
  {
    // Arrange
    var setup = new TwitchSetup
    {
      BotUserId = "bot123",
      BotLogin = "testbot",
      Channels = [],
      ConfiguredOn = DateTimeOffset.UtcNow,
      UpdatedOn = DateTimeOffset.UtcNow,
    };
    _ = _mockSetupState.Setup(s => s.Current).Returns(setup);
    var controller = CreateController();
    var request = new RunAdsRequest { Duration = 30 };

    // Act
    var result = await controller.RunAdsAsync(request, CancellationToken.None);

    // Assert
    _ = result.Should().BeOfType<BadRequestObjectResult>();
  }

  [Theory]
  [InlineData(0)]
  [InlineData(15)]
  [InlineData(45)]
  [InlineData(200)]
  [InlineData(-30)]
  public async Task RunAdsAsyncWithInvalidDurationReturnsBadRequestAsync(int invalidDuration)
  {
    // Arrange
    var setup = new TwitchSetup
    {
      BotUserId = "bot123",
      BotLogin = "testbot",
      Channels = [new TwitchChannel { UserId = "channel123", Login = "testchannel" }],
      ConfiguredOn = DateTimeOffset.UtcNow,
      UpdatedOn = DateTimeOffset.UtcNow,
    };
    _ = _mockSetupState.Setup(s => s.Current).Returns(setup);
    var controller = CreateController();
    var request = new RunAdsRequest { Duration = invalidDuration };

    // Act
    var result = await controller.RunAdsAsync(request, CancellationToken.None);

    // Assert
    _ = result.Should().BeOfType<BadRequestObjectResult>();
  }

  [Fact]
  public async Task RunAdsAsyncWithValidSetupReturnsOkWithRetryAfterAsync()
  {
    // Arrange
    const string twitchResponse = /*lang=json,strict*/ """{"data":[{"length":180,"message":"","retry_after":480}]}""";
    var handler = new FakeHttpMessageHandler(HttpStatusCode.OK, twitchResponse);
    _ = _mockSetupState.Setup(s => s.Current).Returns(MakeSetup());
    var controller = CreateController(handler);
    var request = new RunAdsRequest { Duration = 180 };

    // Act
    var result = await controller.RunAdsAsync(request, CancellationToken.None);

    // Assert
    _ = result.Should().BeOfType<OkObjectResult>();
  }

  [Fact]
  public async Task RunAdsAsyncWithValidSetupCallsCorrectHelixUrlAsync()
  {
    // Arrange
    const string twitchResponse = /*lang=json,strict*/ """{"data":[{"length":180,"message":"","retry_after":480}]}""";
    var handler = new FakeHttpMessageHandler(HttpStatusCode.OK, twitchResponse);
    _ = _mockSetupState.Setup(s => s.Current).Returns(MakeSetup());
    var controller = CreateController(handler);
    var request = new RunAdsRequest { Duration = 180 };

    // Act
    _ = await controller.RunAdsAsync(request, CancellationToken.None);

    // Assert
    _ = handler.LastRequest.Should().NotBeNull();
    _ = handler.LastRequest!.RequestUri!.ToString().Should().Be("https://api.twitch.tv/helix/channels/commercial");
  }

  [Fact]
  public async Task RunAdsAsyncWithValidSetupSendsBroadcasterIdAndLengthInBodyAsync()
  {
    // Arrange
    const string twitchResponse = /*lang=json,strict*/ """{"data":[{"length":180,"message":"","retry_after":480}]}""";
    var handler = new FakeHttpMessageHandler(HttpStatusCode.OK, twitchResponse);
    _ = _mockSetupState.Setup(s => s.Current).Returns(MakeSetup("my-broadcaster-id"));
    var controller = CreateController(handler);
    var request = new RunAdsRequest { Duration = 180 };

    // Act
    _ = await controller.RunAdsAsync(request, CancellationToken.None);

    // Assert
    _ = handler.LastRequestBody.Should().NotBeNull();
    using var doc = JsonDocument.Parse(handler.LastRequestBody!);
    _ = doc.RootElement.GetProperty("broadcaster_id").GetString().Should().Be("my-broadcaster-id");
    _ = doc.RootElement.GetProperty("length").GetInt32().Should().Be(180);
  }

  [Fact]
  public async Task RunAdsAsyncWhenTwitchReturnsNon2xxReturnsProblemAsync()
  {
    // Arrange
    var handler = new FakeHttpMessageHandler(HttpStatusCode.BadRequest, /*lang=json,strict*/ """{"error":"Bad Request"}""");
    _ = _mockSetupState.Setup(s => s.Current).Returns(MakeSetup());
    var controller = CreateController(handler);
    var request = new RunAdsRequest { Duration = 180 };

    // Act
    var result = await controller.RunAdsAsync(request, CancellationToken.None);

    // Assert
    _ = result.Should().BeOfType<ObjectResult>()
        .Which.StatusCode.Should().Be(500);
  }

  [Fact]
  public async Task RunAdsAsyncWhenTwitchReturns403ReturnsProblemWithScopeHintAsync()
  {
    // Arrange
    var handler = new FakeHttpMessageHandler(HttpStatusCode.Forbidden, /*lang=json,strict*/ """{"error":"Forbidden"}""");
    _ = _mockSetupState.Setup(s => s.Current).Returns(MakeSetup());
    var controller = CreateController(handler);
    var request = new RunAdsRequest { Duration = 180 };

    // Act
    var result = await controller.RunAdsAsync(request, CancellationToken.None);

    // Assert
    var problem = result.Should().BeOfType<ObjectResult>().Subject;
    _ = problem.StatusCode.Should().Be(500);
    var details = problem.Value.Should().BeOfType<ProblemDetails>().Subject;
    _ = details.Detail.Should().Contain("channel:edit:commercial");
  }

  [Fact]
  public async Task RunAdsAsyncWhenExceptionThrownReturnsProblemAsync()
  {
    // Arrange
    _ = _mockSetupState.Setup(s => s.Current).Returns(MakeSetup());
    _ = _mockHttpClientFactory
        .Setup(f => f.CreateClient(It.IsAny<string>()))
        .Throws(new InvalidOperationException("Connection failed"));
    var tokenManager = new TwitchTokenManager(DefaultConfig, _mockHttpClientFactory.Object, new Mock<ILogger<TwitchTokenManager>>().Object);
    var controller = new AdsController(tokenManager, DefaultConfig, _mockSetupState.Object, _mockHttpClientFactory.Object, _mockAdsCoordinator.Object, _mockLogger.Object);
    var request = new RunAdsRequest { Duration = 180 };

    // Act
    var result = await controller.RunAdsAsync(request, CancellationToken.None);

    // Assert
    _ = result.Should().BeOfType<ObjectResult>()
        .Which.StatusCode.Should().Be(500);
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
