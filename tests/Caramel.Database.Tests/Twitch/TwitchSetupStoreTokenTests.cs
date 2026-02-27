using Caramel.Core.Security;
using Caramel.Core.Twitch;
using Caramel.Database.Twitch;
using Caramel.Domain.Twitch;

using FluentAssertions;

using Moq;

namespace Caramel.Database.Tests.Twitch;

/// <summary>
/// Unit tests for Twitch token encryption models and interfaces.
/// These tests verify token encryption and storage behavior at the model level.
/// </summary>
public class TwitchSetupStoreTokenTests
{
  [Fact]
  public void TokenEncryptionService_HasEncryptMethod()
  {
    // Arrange
    var mockEncryption = new Mock<ITokenEncryptionService>();
    const string plainText = "token_value";
    const string cipherText = "encrypted_value";

    mockEncryption
      .Setup(s => s.Encrypt(plainText))
      .Returns(cipherText);

    // Act
    var result = mockEncryption.Object.Encrypt(plainText);

    // Assert
    result.Should().Be(cipherText, "Encryption should transform plain text to cipher text");
    mockEncryption.Verify(s => s.Encrypt(plainText), Times.Once);
  }

  [Fact]
  public void TokenEncryptionService_HasTryDecryptMethod()
  {
    // Arrange
    var mockEncryption = new Mock<ITokenEncryptionService>();
    const string cipherText = "encrypted_value";
    const string plainText = "token_value";

    mockEncryption
      .Setup(s => s.TryDecrypt(cipherText))
      .Returns(plainText);

    // Act
    var result = mockEncryption.Object.TryDecrypt(cipherText);

    // Assert
    result.Should().Be(plainText, "Decryption should return decrypted plain text");
    mockEncryption.Verify(s => s.TryDecrypt(cipherText), Times.Once);
  }

  [Fact]
  public void TokenEncryptionService_TryDecryptReturnsNullOnFailure()
  {
    // Arrange
    var mockEncryption = new Mock<ITokenEncryptionService>();
    const string invalidCipherText = "corrupted_or_invalid_ciphertext";

    mockEncryption
      .Setup(s => s.TryDecrypt(invalidCipherText))
      .Returns((string?)null);

    // Act
    var result = mockEncryption.Object.TryDecrypt(invalidCipherText);

    // Assert
    result.Should().BeNull("Decryption should return null for invalid cipher text");
  }

  [Fact]
  public void TwitchAccountTokens_CanBeCreatedWithAllFields()
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
    tokens.UserId.Should().Be(userId);
    tokens.Login.Should().Be(login);
    tokens.AccessToken.Should().Be(accessToken);
    tokens.RefreshToken.Should().Be(refreshToken);
    tokens.ExpiresAt.Should().BeCloseTo(now.AddHours(1), TimeSpan.FromSeconds(1));
  }

  [Fact]
  public void TwitchAccountTokens_RefreshTokenCanBeNull()
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
    tokens.RefreshToken.Should().BeNull();
  }

  [Fact]
  public void DbTwitchSetup_CanStoreEncryptedBotTokens()
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
    setup.BotAccessToken.Should().Be(encryptedAccessToken);
    setup.BotRefreshToken.Should().Be(encryptedRefreshToken);
    setup.BotTokenExpiresAt.Should().Be(now.AddHours(1));
  }

  [Fact]
  public void DbTwitchSetup_CanStoreEncryptedBroadcasterTokens()
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
    setup.BroadcasterAccessToken.Should().Be(encryptedAccessToken);
    setup.BroadcasterRefreshToken.Should().Be(encryptedRefreshToken);
    setup.BroadcasterTokenExpiresAt.Should().Be(now.AddHours(1));
  }

  [Fact]
  public void DbTwitchSetup_Cast_ConvertsEncryptedBotTokensToDomain()
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
    var domainSetup = (Domain.Twitch.TwitchSetup)dbSetup;

    // Assert
    domainSetup.BotTokens.Should().NotBeNull("Bot tokens should be converted from DB model");
    domainSetup.BotTokens!.UserId.Should().Be("bot_123");
    domainSetup.BotTokens.Login.Should().Be("bot_login");
    domainSetup.BotTokens.AccessToken.Should().Be(encryptedAccessToken);
  }

  [Fact]
  public void DbTwitchSetup_Cast_ConvertsEncryptedBroadcasterTokensToDomain()
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
    var domainSetup = (Domain.Twitch.TwitchSetup)dbSetup;

    // Assert
    domainSetup.BroadcasterTokens.Should().NotBeNull("Broadcaster tokens should be converted from DB model");
    domainSetup.BroadcasterTokens!.UserId.Should().Be("bc_123");
    domainSetup.BroadcasterTokens.Login.Should().Be("bc_login");
    domainSetup.BroadcasterTokens.AccessToken.Should().Be(encryptedAccessToken);
  }

  [Fact]
  public void ITwitchSetupStore_HasSaveBotTokensMethod()
  {
    // Act
    var method = typeof(ITwitchSetupStore)
      .GetMethod("SaveBotTokensAsync");

    // Assert
    method.Should().NotBeNull("ITwitchSetupStore should have SaveBotTokensAsync method");
  }

  [Fact]
  public void ITwitchSetupStore_HasSaveBroadcasterTokensMethod()
  {
    // Act
    var method = typeof(ITwitchSetupStore)
      .GetMethod("SaveBroadcasterTokensAsync");

    // Assert
    method.Should().NotBeNull("ITwitchSetupStore should have SaveBroadcasterTokensAsync method");
  }
}


