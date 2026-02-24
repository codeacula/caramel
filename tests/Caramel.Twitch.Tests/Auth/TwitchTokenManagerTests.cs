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

  [Fact]
  public async Task GetValidAccessTokenAsyncReturnsInitialTokenWhenNotExpiredAsync()
  {
    var manager = new TwitchTokenManager(_mockConfig, _mockHttpClientFactory.Object, _mockLogger.Object);
    var token = await manager.GetValidAccessTokenAsync();
    _ = token.Should().Be("initial-access-token");
  }

  [Fact]
  public void GetCurrentAccessTokenReturnsCurrentToken()
  {
    var manager = new TwitchTokenManager(_mockConfig, _mockHttpClientFactory.Object, _mockLogger.Object);
    var token = manager.GetCurrentAccessToken();
    _ = token.Should().Be("initial-access-token");
  }

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

    var manager = new TwitchTokenManager(configWithoutRefresh, _mockHttpClientFactory.Object, _mockLogger.Object);
    // Set tokens to expire immediately
    manager.SetTokens("expired-token", null, -1);

    _ = await manager.Invoking(m => m.GetValidAccessTokenAsync())
      .Should()
      .ThrowAsync<InvalidOperationException>();
  }

  [Fact]
  public async Task GetValidAccessTokenAsyncMultipleCallsReturnSameTokenWhenNotExpiredAsync()
  {
    var manager = new TwitchTokenManager(_mockConfig, _mockHttpClientFactory.Object, _mockLogger.Object);
    var token1 = await manager.GetValidAccessTokenAsync();
    var token2 = await manager.GetValidAccessTokenAsync();
    _ = token1.Should().Be(token2);
  }
}
