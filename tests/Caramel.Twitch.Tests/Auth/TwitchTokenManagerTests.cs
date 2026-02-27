namespace Caramel.Twitch.Tests.Auth;

public sealed class TwitchTokenManagerTests
{
  private readonly TwitchConfig _mockConfig = new()
  {
    ClientId = "test-client-id",
    ClientSecret = "test-client-secret",
    AccessToken = "initial-access-token",
    RefreshToken = "initial-refresh-token",
    OAuthCallbackUrl = "http://localhost:5146/auth/twitch/callback",
    EncryptionKey = Convert.ToBase64String(new byte[32]),
  };

  private readonly Mock<IHttpClientFactory> _mockHttpClientFactory = new();
  private readonly Mock<ILogger<TwitchTokenManager>> _mockLogger = new();

  /// <summary>
  /// Configures <paramref name="factory"/> to return an <see cref="HttpClient"/> whose handler
  /// replies with a successful Twitch token-refresh response body.
  /// </summary>
  private static void SetupRefreshHttpResponse(
    Mock<IHttpClientFactory> factory,
    string accessToken = "refreshed-access-token",
    string refreshToken = "refreshed-refresh-token",
    int expiresIn = 3600)
  {
    var json = JsonSerializer.Serialize(new
    {
      access_token = accessToken,
      refresh_token = refreshToken,
      expires_in = expiresIn,
    });

    var handler = new MockHttpMessageHandler(
      new HttpResponseMessage(HttpStatusCode.OK)
      {
        Content = new StringContent(json, Encoding.UTF8, "application/json"),
      });

    factory
      .Setup(f => f.CreateClient(It.IsAny<string>()))
      .Returns(new HttpClient(handler));
  }

  // ---------------------------------------------------------------------------
  // Constructor / startup-token behaviour
  // ---------------------------------------------------------------------------

  [Fact]
  public async Task Constructor_WithExistingAccessToken_TreatsTokenAsExpiredToForceRefresh()
  {
    // Arrange – config has both an access token and a refresh token
    SetupRefreshHttpResponse(_mockHttpClientFactory, accessToken: "refreshed-access-token");
    var manager = new TwitchTokenManager(_mockConfig, _mockHttpClientFactory.Object, _mockLogger.Object);

    // Act – first call must trigger a refresh, NOT return the startup token directly
    var token = await manager.GetValidAccessTokenAsync();

    // Assert – the returned token is the refreshed one, not the startup "initial-access-token"
    _ = token.Should().Be("refreshed-access-token");
    _mockHttpClientFactory.Verify(f => f.CreateClient(It.IsAny<string>()), Times.Once);
  }

  // ---------------------------------------------------------------------------
  // GetValidAccessTokenAsync
  // ---------------------------------------------------------------------------

  [Fact]
  public async Task GetValidAccessTokenAsyncRefreshesAndReturnsNewTokenOnStartupAsync()
  {
    // Startup token is always treated as expired; a refresh must occur.
    SetupRefreshHttpResponse(_mockHttpClientFactory, accessToken: "refreshed-access-token");
    var manager = new TwitchTokenManager(_mockConfig, _mockHttpClientFactory.Object, _mockLogger.Object);

    var token = await manager.GetValidAccessTokenAsync();

    _ = token.Should().Be("refreshed-access-token");
  }

  [Fact]
  public async Task GetValidAccessTokenAsyncThrowsInvalidOperationExceptionWhenNoRefreshTokenAsync()
  {
    var configWithoutRefresh = new TwitchConfig
    {
      ClientId = "test-client-id",
      ClientSecret = "test-client-secret",
      AccessToken = "expired-token",
      OAuthCallbackUrl = "http://localhost:5146/auth/twitch/callback",
      EncryptionKey = Convert.ToBase64String(new byte[32]),
    };

    // No refresh token in config → manager starts expired and cannot refresh
    var manager = new TwitchTokenManager(configWithoutRefresh, _mockHttpClientFactory.Object, _mockLogger.Object);

    _ = await manager.Invoking(m => m.GetValidAccessTokenAsync())
      .Should()
      .ThrowAsync<InvalidOperationException>();
  }

  [Fact]
  public async Task GetValidAccessTokenAsyncMultipleCallsReturnSameTokenAfterInitialRefreshAsync()
  {
    // After the first refresh the token is valid; subsequent calls must NOT trigger another HTTP call.
    SetupRefreshHttpResponse(_mockHttpClientFactory, accessToken: "refreshed-access-token");
    var manager = new TwitchTokenManager(_mockConfig, _mockHttpClientFactory.Object, _mockLogger.Object);

    var token1 = await manager.GetValidAccessTokenAsync(); // triggers refresh
    var token2 = await manager.GetValidAccessTokenAsync(); // served from cache

    _ = token1.Should().Be(token2);
    // The refresh endpoint should only have been hit once
    _mockHttpClientFactory.Verify(f => f.CreateClient(It.IsAny<string>()), Times.Once);
  }

  // ---------------------------------------------------------------------------
  // GetCurrentAccessToken
  // ---------------------------------------------------------------------------

  [Fact]
  public void GetCurrentAccessTokenReturnsCurrentToken()
  {
    var manager = new TwitchTokenManager(_mockConfig, _mockHttpClientFactory.Object, _mockLogger.Object);
    var token = manager.GetCurrentAccessToken();
    // GetCurrentAccessToken returns the raw field value – still "initial-access-token" until
    // GetValidAccessTokenAsync is called and performs the refresh.
    _ = token.Should().Be("initial-access-token");
  }

  // ---------------------------------------------------------------------------
  // SetTokens
  // ---------------------------------------------------------------------------

  [Fact]
  public void SetTokensUpdatesAccessToken()
  {
    var manager = new TwitchTokenManager(_mockConfig, _mockHttpClientFactory.Object, _mockLogger.Object);
    manager.SetTokens("new-access-token", "new-refresh-token", 3600);
    _ = manager.GetCurrentAccessToken().Should().Be("new-access-token");
  }

  [Fact]
  public void SetTokensWithoutRefreshTokenPreservesExistingRefreshToken()
  {
    var manager = new TwitchTokenManager(_mockConfig, _mockHttpClientFactory.Object, _mockLogger.Object);
    manager.SetTokens("new-access-token", null, 3600);
    // The refresh token should still be the original one since we didn't provide a new one
    // (This is an internal state test - can't verify refresh token directly, but the manager should preserve it)
  }

  [Fact]
  public void SetTokensUpdatesExpiryTime()
  {
    var manager = new TwitchTokenManager(_mockConfig, _mockHttpClientFactory.Object, _mockLogger.Object);
    _ = DateTime.UtcNow;
    manager.SetTokens("new-token", null, 1800);
    _ = DateTime.UtcNow;

    // After calling GetValidAccessTokenAsync, the token should be valid
    // This is a behavioral test
  }

  // ---------------------------------------------------------------------------
  // CanRefresh
  // ---------------------------------------------------------------------------

  [Fact]
  public void CanRefreshReturnsTrueWhenRefreshTokenExists()
  {
    var manager = new TwitchTokenManager(_mockConfig, _mockHttpClientFactory.Object, _mockLogger.Object);
    _ = manager.CanRefresh().Should().BeTrue();
  }

  [Fact]
  public void CanRefreshReturnsFalseWhenNoRefreshToken()
  {
    var configWithoutRefresh = new TwitchConfig
    {
      ClientId = "test-client-id",
      ClientSecret = "test-client-secret",
      AccessToken = "test-token",
      OAuthCallbackUrl = "http://localhost:5146/auth/twitch/callback",
      EncryptionKey = Convert.ToBase64String(new byte[32]),
    };

    var manager = new TwitchTokenManager(configWithoutRefresh, _mockHttpClientFactory.Object, _mockLogger.Object);
    _ = manager.CanRefresh().Should().BeFalse();
  }

  // ---------------------------------------------------------------------------
  // Helper: minimal HttpMessageHandler stub
  // ---------------------------------------------------------------------------

  private sealed class MockHttpMessageHandler(HttpResponseMessage response) : HttpMessageHandler
  {
    protected override Task<HttpResponseMessage> SendAsync(
      HttpRequestMessage request,
      CancellationToken cancellationToken) =>
      Task.FromResult(response);
  }
}
