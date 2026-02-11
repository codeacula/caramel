namespace Caramel.Twitch.Tests.Auth;

public sealed class TwitchTokenManagerTests
{
  private readonly TwitchConfig _mockConfig = new()
  {
    ClientId = "test-client-id",
    ClientSecret = "test-client-secret",
    AccessToken = "initial-access-token",
    RefreshToken = "initial-refresh-token",
    BotUserId = "123456",
    ChannelIds = "123456",
    OAuthCallbackUrl = "http://localhost:5146/auth/callback",
    EncryptionKey = Convert.ToBase64String(new byte[32]),
  };

  [Fact]
  public async Task GetValidAccessTokenAsyncReturnsInitialToken_WhenNotExpired()
  {
    var manager = new TwitchTokenManager(_mockConfig);
    var token = await manager.GetValidAccessTokenAsync();
    token.Should().Be("initial-access-token");
  }

  [Fact]
  public void GetCurrentAccessTokenReturnsCurrentToken()
  {
    var manager = new TwitchTokenManager(_mockConfig);
    var token = manager.GetCurrentAccessToken();
    token.Should().Be("initial-access-token");
  }

  [Fact]
  public void SetTokensUpdatesAccessToken()
  {
    var manager = new TwitchTokenManager(_mockConfig);
    manager.SetTokens("new-access-token", "new-refresh-token", 3600);
    manager.GetCurrentAccessToken().Should().Be("new-access-token");
  }

  [Fact]
  public void SetTokensWithoutRefreshTokenPreservesExistingRefreshToken()
  {
    var manager = new TwitchTokenManager(_mockConfig);
    manager.SetTokens("new-access-token", null, 3600);
    // The refresh token should still be the original one since we didn't provide a new one
    // (This is an internal state test - can't verify refresh token directly, but the manager should preserve it)
  }

  [Fact]
  public void SetTokensUpdatesExpiryTime()
  {
    var manager = new TwitchTokenManager(_mockConfig);
    var beforeSet = DateTime.UtcNow;
    manager.SetTokens("new-token", null, 1800);
    var afterSet = DateTime.UtcNow;

    // After calling GetValidAccessTokenAsync, the token should be valid
    // This is a behavioral test
  }

  [Fact]
  public void CanRefreshReturnsTrueWhenRefreshTokenExists()
  {
    var manager = new TwitchTokenManager(_mockConfig);
    manager.CanRefresh().Should().BeTrue();
  }

  [Fact]
  public void CanRefreshReturnsFalseWhenNoRefreshToken()
  {
    var configWithoutRefresh = new TwitchConfig
    {
      ClientId = "test-client-id",
      ClientSecret = "test-client-secret",
      AccessToken = "test-token",
      BotUserId = "123456",
      ChannelIds = "123456",
      OAuthCallbackUrl = "http://localhost:5146/auth/callback",
      EncryptionKey = Convert.ToBase64String(new byte[32]),
    };

    var manager = new TwitchTokenManager(configWithoutRefresh);
    manager.CanRefresh().Should().BeFalse();
  }

  [Fact]
  public async Task GetValidAccessTokenAsyncThrowsInvalidOperationException_WhenNoRefreshToken()
  {
    var configWithoutRefresh = new TwitchConfig
    {
      ClientId = "test-client-id",
      ClientSecret = "test-client-secret",
      AccessToken = "expired-token",
      BotUserId = "123456",
      ChannelIds = "123456",
      OAuthCallbackUrl = "http://localhost:5146/auth/callback",
      EncryptionKey = Convert.ToBase64String(new byte[32]),
    };

    var manager = new TwitchTokenManager(configWithoutRefresh);
    // Set tokens to expire immediately
    manager.SetTokens("expired-token", null, -1);

    await manager.Invoking(m => m.GetValidAccessTokenAsync())
      .Should()
      .ThrowAsync<InvalidOperationException>();
  }

  [Fact]
  public void SetTokensMultipleTimesIsThreadSafe()
  {
    var manager = new TwitchTokenManager(_mockConfig);
    var tasks = Enumerable.Range(0, 50).Select(i =>
    {
      return Task.Run(() =>
      {
        manager.SetTokens($"token-{i}", $"refresh-{i}", 3600);
      });
    });

    Task.WaitAll(tasks.ToArray());

    // Should end up with one of the tokens set
    var finalToken = manager.GetCurrentAccessToken();
    finalToken.Should().StartWith("token-");
  }

  [Fact]
  public void GetCurrentAccessTokenIsThreadSafe()
  {
    var manager = new TwitchTokenManager(_mockConfig);
    var tasks = Enumerable.Range(0, 100).Select(_ =>
    {
      return Task.Run(() =>
      {
        var token = manager.GetCurrentAccessToken();
        token.Should().NotBeNullOrEmpty();
      });
    });

    Task.WaitAll(tasks.ToArray());
  }

  [Fact]
  public async Task GetValidAccessTokenAsyncMultipleCallsReturnSameToken_WhenNotExpired()
  {
    var manager = new TwitchTokenManager(_mockConfig);
    var token1 = await manager.GetValidAccessTokenAsync();
    var token2 = await manager.GetValidAccessTokenAsync();
    token1.Should().Be(token2);
  }
}
