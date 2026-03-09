namespace Caramel.Database.Tests.Twitch;

/// <summary>
/// Unit tests for Twitch token encryption models and interfaces.
/// These tests verify token encryption and storage behavior at the model level.
/// </summary>
public class TwitchSetupStoreTokenTests
{
  [Fact]
  public void TokenEncryptionServiceHasEncryptMethod()
  {
    // Arrange
    var mockEncryption = new Mock<ITokenEncryptionService>();
    const string plainText = "token_value";
    const string cipherText = "encrypted_value";

    _ = mockEncryption
      .Setup(s => s.Encrypt(plainText))
      .Returns(cipherText);

    // Act
    var result = mockEncryption.Object.Encrypt(plainText);

    // Assert
    _ = result.Should().Be(cipherText, "Encryption should transform plain text to cipher text");
    mockEncryption.Verify(s => s.Encrypt(plainText), Times.Once);
  }

  [Fact]
  public void TokenEncryptionServiceHasTryDecryptMethod()
  {
    // Arrange
    var mockEncryption = new Mock<ITokenEncryptionService>();
    const string cipherText = "encrypted_value";
    const string plainText = "token_value";

    _ = mockEncryption
      .Setup(s => s.TryDecrypt(cipherText))
      .Returns(plainText);

    // Act
    var result = mockEncryption.Object.TryDecrypt(cipherText);

    // Assert
    _ = result.Should().Be(plainText, "Decryption should return decrypted plain text");
    mockEncryption.Verify(s => s.TryDecrypt(cipherText), Times.Once);
  }

  [Fact]
  public void TokenEncryptionServiceTryDecryptReturnsNullOnFailure()
  {
    // Arrange
    var mockEncryption = new Mock<ITokenEncryptionService>();
    const string invalidCipherText = "corrupted_or_invalid_ciphertext";

    _ = mockEncryption
      .Setup(s => s.TryDecrypt(invalidCipherText))
      .Returns((string?)null);

    // Act
    var result = mockEncryption.Object.TryDecrypt(invalidCipherText);

    // Assert
    _ = result.Should().BeNull("Decryption should return null for invalid cipher text");
  }

  [Fact]
  public void TwitchAccountTokensCanBeCreatedWithAllFields()
  {
    // Arrange
    var now = DateTime.UtcNow;
    const string userId = "123456";
    const string login = "testuser";
    const string accessToken = "access_token_value";
    const string refreshToken = "refresh_token_value";

    // Act
    var tokens = new TwitchAccountTokens
    {
      UserId = userId,
      Login = login,
      AccessToken = accessToken,
      RefreshToken = refreshToken,
      ExpiresAt = now.AddHours(1),
      LastRefreshedOn = now,
    };

    // Assert
    _ = tokens.UserId.Should().Be(userId);
    _ = tokens.Login.Should().Be(login);
    _ = tokens.AccessToken.Should().Be(accessToken);
    _ = tokens.RefreshToken.Should().Be(refreshToken);
    _ = tokens.ExpiresAt.Should().BeCloseTo(now.AddHours(1), TimeSpan.FromSeconds(1));
  }

  [Fact]
  public void TwitchAccountTokensRefreshTokenCanBeNull()
  {
    // Arrange
    var now = DateTime.UtcNow;

    // Act
    var tokens = new TwitchAccountTokens
    {
      UserId = "bot_123",
      Login = "bot_login",
      AccessToken = "access_token",
      RefreshToken = null,
      ExpiresAt = now.AddHours(1),
      LastRefreshedOn = now,
    };

    // Assert
    _ = tokens.RefreshToken.Should().BeNull();
  }

  [Fact]
  public void DbTwitchSetupCanStoreEncryptedBotTokens()
  {
    // Arrange
    var now = DateTime.UtcNow;
    const string encryptedAccessToken = "encrypted_access_token_xyz";
    const string encryptedRefreshToken = "encrypted_refresh_token_abc";

    // Act
    var setup = new DbTwitchSetup
    {
      Id = DbTwitchSetup.WellKnownId,
      BotUserId = "bot_123",
      BotLogin = "bot_login",
      Channels = [],
      CreatedOn = now,
      UpdatedOn = now,
      BotAccessToken = encryptedAccessToken,
      BotRefreshToken = encryptedRefreshToken,
      BotTokenExpiresAt = now.AddHours(1),
    };

    // Assert
    _ = setup.BotAccessToken.Should().Be(encryptedAccessToken);
    _ = setup.BotRefreshToken.Should().Be(encryptedRefreshToken);
    _ = setup.BotTokenExpiresAt.Should().Be(now.AddHours(1));
  }

  [Fact]
  public void DbTwitchSetupCanStoreEncryptedBroadcasterTokens()
  {
    // Arrange
    var now = DateTime.UtcNow;
    const string encryptedAccessToken = "encrypted_bc_access_token";
    const string encryptedRefreshToken = "encrypted_bc_refresh_token";

    // Act
    var setup = new DbTwitchSetup
    {
      Id = DbTwitchSetup.WellKnownId,
      BotUserId = "bot_123",
      BotLogin = "bot_login",
      Channels = [],
      CreatedOn = now,
      UpdatedOn = now,
      BroadcasterUserId = "bc_123",
      BroadcasterLogin = "bc_login",
      BroadcasterAccessToken = encryptedAccessToken,
      BroadcasterRefreshToken = encryptedRefreshToken,
      BroadcasterTokenExpiresAt = now.AddHours(1),
    };

    // Assert
    _ = setup.BroadcasterAccessToken.Should().Be(encryptedAccessToken);
    _ = setup.BroadcasterRefreshToken.Should().Be(encryptedRefreshToken);
    _ = setup.BroadcasterTokenExpiresAt.Should().Be(now.AddHours(1));
  }

  [Fact]
  public void DbTwitchSetupCastConvertsEncryptedBotTokensToDomain()
  {
    // Arrange
    var now = DateTime.UtcNow;
    const string encryptedAccessToken = "encrypted_access_token";
    const string encryptedRefreshToken = "encrypted_refresh_token";

    var dbSetup = new DbTwitchSetup
    {
      Id = DbTwitchSetup.WellKnownId,
      BotUserId = "bot_123",
      BotLogin = "bot_login",
      Channels = [],
      CreatedOn = now,
      UpdatedOn = now,
      BotAccessToken = encryptedAccessToken,
      BotRefreshToken = encryptedRefreshToken,
      BotTokenExpiresAt = now.AddHours(1),
    };

    // Act
    var domainSetup = (TwitchSetup)dbSetup;

    // Assert
    _ = domainSetup.BotTokens.Should().NotBeNull("Bot tokens should be converted from DB model");
    _ = domainSetup.BotTokens!.UserId.Should().Be("bot_123");
    _ = domainSetup.BotTokens.Login.Should().Be("bot_login");
    _ = domainSetup.BotTokens.AccessToken.Should().Be(encryptedAccessToken);
  }

  [Fact]
  public void DbTwitchSetupCastConvertsEncryptedBroadcasterTokensToDomain()
  {
    // Arrange
    var now = DateTime.UtcNow;
    const string encryptedAccessToken = "encrypted_bc_access_token";

    var dbSetup = new DbTwitchSetup
    {
      Id = DbTwitchSetup.WellKnownId,
      BotUserId = "bot_123",
      BotLogin = "bot_login",
      Channels = [],
      CreatedOn = now,
      UpdatedOn = now,
      BroadcasterUserId = "bc_123",
      BroadcasterLogin = "bc_login",
      BroadcasterAccessToken = encryptedAccessToken,
      BroadcasterRefreshToken = null,
      BroadcasterTokenExpiresAt = now.AddHours(1),
    };

    // Act
    var domainSetup = (TwitchSetup)dbSetup;

    // Assert
    _ = domainSetup.BroadcasterTokens.Should().NotBeNull("Broadcaster tokens should be converted from DB model");
    _ = domainSetup.BroadcasterTokens!.UserId.Should().Be("bc_123");
    _ = domainSetup.BroadcasterTokens.Login.Should().Be("bc_login");
    _ = domainSetup.BroadcasterTokens.AccessToken.Should().Be(encryptedAccessToken);
  }

  [Fact]
  public void ITwitchSetupStoreHasSaveBotTokensMethod()
  {
    // Act
    var method = typeof(ITwitchSetupStore)
      .GetMethod("SaveBotTokensAsync");

    // Assert
    _ = method.Should().NotBeNull("ITwitchSetupStore should have SaveBotTokensAsync method");
  }

  [Fact]
  public void ITwitchSetupStoreHasSaveBroadcasterTokensMethod()
  {
    // Act
    var method = typeof(ITwitchSetupStore)
      .GetMethod("SaveBroadcasterTokensAsync");

    // Assert
    _ = method.Should().NotBeNull("ITwitchSetupStore should have SaveBroadcasterTokensAsync method");
  }
}


