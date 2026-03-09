namespace Caramel.Twitch.Tests.Auth;

public sealed class DualOAuthTokenManagerTests
{
  [Fact]
  public async Task InitializeAsyncLoadsTokensFromDatabaseAsync()
  {
    // Arrange
    var setupStore = new Mock<ITwitchSetupStore>();
    var botTokens = new TwitchAccountTokens
    {
      UserId = "123",
      Login = "test_bot",
      AccessToken = "bot_access_token",
      RefreshToken = "bot_refresh_token",
      ExpiresAt = DateTime.UtcNow.AddHours(1),
      LastRefreshedOn = DateTimeOffset.UtcNow,
    };
    var broadcasterTokens = new TwitchAccountTokens
    {
      UserId = "456",
      Login = "test_broadcaster",
      AccessToken = "broadcaster_access_token",
      RefreshToken = "broadcaster_refresh_token",
      ExpiresAt = DateTime.UtcNow.AddHours(1),
      LastRefreshedOn = DateTimeOffset.UtcNow,
    };
    var setup = new TwitchSetup
    {
      BotUserId = "123",
      BotLogin = "test_bot",
      Channels = [],
      ConfiguredOn = DateTimeOffset.UtcNow,
      UpdatedOn = DateTimeOffset.UtcNow,
      BotTokens = botTokens,
      BroadcasterTokens = broadcasterTokens,
    };
    _ = setupStore.Setup(s => s.GetAsync(It.IsAny<CancellationToken>()))
      .ReturnsAsync(Result.Ok<TwitchSetup?>(setup));

    var tokenManager = CreateTokenManager(setupStore);

    // Act
    await tokenManager.InitializeAsync();

    // Assert
    _ = tokenManager.GetCurrentBotAccessToken().Should().Be("bot_access_token");
    _ = tokenManager.GetCurrentBroadcasterAccessToken().Should().Be("broadcaster_access_token");
    _ = tokenManager.CanRefreshBotToken().Should().BeTrue();
    _ = tokenManager.CanRefreshBroadcasterToken().Should().BeTrue();
  }

  [Fact]
  public async Task InitializeAsyncHandlesNoSetupInDatabaseAsync()
  {
    // Arrange
    var setupStore = new Mock<ITwitchSetupStore>();
    _ = setupStore.Setup(s => s.GetAsync(It.IsAny<CancellationToken>()))
      .ReturnsAsync(Result.Ok<TwitchSetup?>(null));

    var tokenManager = CreateTokenManager(setupStore);

    // Act
    await tokenManager.InitializeAsync();

    // Assert
    _ = tokenManager.GetCurrentBotAccessToken().Should().BeNull();
    _ = tokenManager.GetCurrentBroadcasterAccessToken().Should().BeNull();
  }

  [Fact]
  public async Task SetBotTokensAsyncPersistsTokensToDatabaseAsync()
  {
    // Arrange
    var setupStore = new Mock<ITwitchSetupStore>();
    var tokens = new TwitchAccountTokens
    {
      UserId = "123",
      Login = "test_bot",
      AccessToken = "new_bot_token",
      RefreshToken = "new_bot_refresh",
      ExpiresAt = DateTime.UtcNow.AddHours(1),
      LastRefreshedOn = DateTimeOffset.UtcNow,
    };
    var updatedSetup = new TwitchSetup
    {
      BotUserId = "123",
      BotLogin = "test_bot",
      Channels = [],
      ConfiguredOn = DateTimeOffset.UtcNow,
      UpdatedOn = DateTimeOffset.UtcNow,
      BotTokens = tokens,
    };
    _ = setupStore.Setup(s => s.SaveBotTokensAsync(tokens, It.IsAny<CancellationToken>()))
      .ReturnsAsync(Result.Ok(updatedSetup));

    var tokenManager = CreateTokenManager(setupStore);

    // Act
    await tokenManager.SetBotTokensAsync(tokens);

    // Assert
    _ = tokenManager.GetCurrentBotAccessToken().Should().Be("new_bot_token");
    setupStore.Verify(s => s.SaveBotTokensAsync(tokens, It.IsAny<CancellationToken>()), Times.Once);
  }

  [Fact]
  public async Task SetBroadcasterTokensAsyncPersistsTokensToDatabaseAsync()
  {
    // Arrange
    var setupStore = new Mock<ITwitchSetupStore>();
    var tokens = new TwitchAccountTokens
    {
      UserId = "456",
      Login = "test_broadcaster",
      AccessToken = "new_broadcaster_token",
      RefreshToken = "new_broadcaster_refresh",
      ExpiresAt = DateTime.UtcNow.AddHours(1),
      LastRefreshedOn = DateTimeOffset.UtcNow,
    };
    var updatedSetup = new TwitchSetup
    {
      BotUserId = "123",
      BotLogin = "test_bot",
      Channels = [],
      ConfiguredOn = DateTimeOffset.UtcNow,
      UpdatedOn = DateTimeOffset.UtcNow,
      BroadcasterTokens = tokens,
    };
    _ = setupStore.Setup(s => s.SaveBroadcasterTokensAsync(tokens, It.IsAny<CancellationToken>()))
      .ReturnsAsync(Result.Ok(updatedSetup));

    var tokenManager = CreateTokenManager(setupStore);

    // Act
    await tokenManager.SetBroadcasterTokensAsync(tokens);

    // Assert
    _ = tokenManager.GetCurrentBroadcasterAccessToken().Should().Be("new_broadcaster_token");
    setupStore.Verify(s => s.SaveBroadcasterTokensAsync(tokens, It.IsAny<CancellationToken>()), Times.Once);
  }

  [Fact]
  public async Task SetBotTokensAsyncThrowsWhenPersistenceFailsAsync()
  {
    // Arrange
    var setupStore = new Mock<ITwitchSetupStore>();
    var tokens = new TwitchAccountTokens
    {
      UserId = "123",
      Login = "test_bot",
      AccessToken = "token",
      RefreshToken = null,
      ExpiresAt = DateTime.UtcNow.AddHours(1),
      LastRefreshedOn = DateTimeOffset.UtcNow,
    };
    _ = setupStore.Setup(s => s.SaveBotTokensAsync(tokens, It.IsAny<CancellationToken>()))
      .ReturnsAsync(Result.Fail("Database error"));

    var tokenManager = CreateTokenManager(setupStore);

    // Act & Assert
    _ = await tokenManager.Invoking(tm => tm.SetBotTokensAsync(tokens))
      .Should().ThrowAsync<InvalidOperationException>();
  }

  [Fact]
  public async Task CanRefreshBotTokenReturnsFalseWhenNoRefreshTokenAsync()
  {
    // Arrange
    var setupStore = new Mock<ITwitchSetupStore>();
    var botTokens = new TwitchAccountTokens
    {
      UserId = "123",
      Login = "test_bot",
      AccessToken = "token",
      RefreshToken = null, // No refresh token
      ExpiresAt = DateTime.UtcNow.AddHours(1),
      LastRefreshedOn = DateTimeOffset.UtcNow,
    };
    var setup = new TwitchSetup
    {
      BotUserId = "123",
      BotLogin = "test_bot",
      Channels = [],
      ConfiguredOn = DateTimeOffset.UtcNow,
      UpdatedOn = DateTimeOffset.UtcNow,
      BotTokens = botTokens,
    };
    _ = setupStore.Setup(s => s.GetAsync(It.IsAny<CancellationToken>()))
      .ReturnsAsync(Result.Ok<TwitchSetup?>(setup));

    var tokenManager = CreateTokenManager(setupStore);
    await tokenManager.InitializeAsync();

    // Act & Assert
    _ = tokenManager.CanRefreshBotToken().Should().BeFalse();
  }

  [Fact]
  public async Task CanRefreshBroadcasterTokenReturnsFalseWhenNoRefreshTokenAsync()
  {
    // Arrange
    var setupStore = new Mock<ITwitchSetupStore>();
    var broadcasterTokens = new TwitchAccountTokens
    {
      UserId = "456",
      Login = "test_broadcaster",
      AccessToken = "token",
      RefreshToken = null, // No refresh token
      ExpiresAt = DateTime.UtcNow.AddHours(1),
      LastRefreshedOn = DateTimeOffset.UtcNow,
    };
    var setup = new TwitchSetup
    {
      BotUserId = "123",
      BotLogin = "test_bot",
      Channels = [],
      ConfiguredOn = DateTimeOffset.UtcNow,
      UpdatedOn = DateTimeOffset.UtcNow,
      BroadcasterTokens = broadcasterTokens,
    };
    _ = setupStore.Setup(s => s.GetAsync(It.IsAny<CancellationToken>()))
      .ReturnsAsync(Result.Ok<TwitchSetup?>(setup));

    var tokenManager = CreateTokenManager(setupStore);
    await tokenManager.InitializeAsync();

    // Act & Assert
    _ = tokenManager.CanRefreshBroadcasterToken().Should().BeFalse();
  }

  [Fact]
  public async Task GetValidBotTokenAsyncThrowsWhenNoRefreshTokenAvailableAsync()
  {
    // Arrange
    var setupStore = new Mock<ITwitchSetupStore>();
    var botTokens = new TwitchAccountTokens
    {
      UserId = "123",
      Login = "test_bot",
      AccessToken = "expired_token",
      RefreshToken = null,
      ExpiresAt = DateTime.UtcNow.AddSeconds(-10), // Expired
      LastRefreshedOn = DateTimeOffset.UtcNow,
    };
    var setup = new TwitchSetup
    {
      BotUserId = "123",
      BotLogin = "test_bot",
      Channels = [],
      ConfiguredOn = DateTimeOffset.UtcNow,
      UpdatedOn = DateTimeOffset.UtcNow,
      BotTokens = botTokens,
    };
    _ = setupStore.Setup(s => s.GetAsync(It.IsAny<CancellationToken>()))
      .ReturnsAsync(Result.Ok<TwitchSetup?>(setup));

    var tokenManager = CreateTokenManager(setupStore);
    await tokenManager.InitializeAsync();

    // Act & Assert
    _ = await tokenManager.Invoking(tm => tm.GetValidBotTokenAsync())
      .Should().ThrowAsync<InvalidOperationException>()
      .WithMessage("*no refresh token available*");
  }

  [Fact]
  public async Task GetValidBroadcasterTokenAsyncThrowsWhenNoRefreshTokenAvailableAsync()
  {
    // Arrange
    var setupStore = new Mock<ITwitchSetupStore>();
    var broadcasterTokens = new TwitchAccountTokens
    {
      UserId = "456",
      Login = "test_broadcaster",
      AccessToken = "expired_token",
      RefreshToken = null,
      ExpiresAt = DateTime.UtcNow.AddSeconds(-10), // Expired
      LastRefreshedOn = DateTimeOffset.UtcNow,
    };
    var setup = new TwitchSetup
    {
      BotUserId = "123",
      BotLogin = "test_bot",
      Channels = [],
      ConfiguredOn = DateTimeOffset.UtcNow,
      UpdatedOn = DateTimeOffset.UtcNow,
      BroadcasterTokens = broadcasterTokens,
    };
    _ = setupStore.Setup(s => s.GetAsync(It.IsAny<CancellationToken>()))
      .ReturnsAsync(Result.Ok<TwitchSetup?>(setup));

    var tokenManager = CreateTokenManager(setupStore);
    await tokenManager.InitializeAsync();

    // Act & Assert
    _ = await tokenManager.Invoking(tm => tm.GetValidBroadcasterTokenAsync())
      .Should().ThrowAsync<InvalidOperationException>()
      .WithMessage("*no refresh token available*");
  }

  private static DualOAuthTokenManager CreateTokenManager(Mock<ITwitchSetupStore>? setupStore = null)
  {
    var encryptionService = new Mock<ITokenEncryptionService>();
    var httpClientFactory = new Mock<IHttpClientFactory>();
    var twitchConfig = new TwitchConfig
    {
      ClientId = "test_client_id",
      ClientSecret = "test_client_secret",
      AccessToken = null,
      RefreshToken = null,
      OAuthCallbackUrl = "http://localhost/auth/twitch/callback",
      EncryptionKey = null,
    };
    var logger = new Mock<ILogger<DualOAuthTokenManager>>();

    var store = setupStore ?? new Mock<ITwitchSetupStore>();
    return new DualOAuthTokenManager(
      store.Object,
      encryptionService.Object,
      httpClientFactory.Object,
      twitchConfig,
      logger.Object
    );
  }
}

