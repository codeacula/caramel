using Caramel.Core.Configuration;

namespace Caramel.Core.Tests.Configuration;

public sealed class TwitchConfigOptionsTests
{
  private static TwitchConfigOptions MakeValidOptions()
  {
    return new()
    {
      ClientId = "test-client-id-123",
      ClientSecret = "test-client-secret-123",
      EncryptionKey = new string('a', 32),
      OAuthCallbackUrl = "https://localhost:8083/auth/twitch/callback",
      AccessToken = "test-access-token-123",
      RefreshToken = "test-refresh-token-123",
    };
  }

  [Fact]
  public void ValidateWithAllRequiredFieldsReturnsNoErrors()
  {
    var options = MakeValidOptions();

    var errors = options.Validate();

    _ = errors.Should().BeEmpty();
  }

  [Fact]
  public void ValidateWithoutAccessTokenDoesNotReturnValidationError()
  {
    var options = MakeValidOptions();
    options.AccessToken = string.Empty;

    var errors = options.Validate();

    _ = errors.Should().NotContain(error => error.Contains("AccessToken"));
  }

  [Fact]
  public void ValidateWithoutRefreshTokenDoesNotReturnValidationError()
  {
    var options = MakeValidOptions();
    options.RefreshToken = string.Empty;

    var errors = options.Validate();

    _ = errors.Should().NotContain(error => error.Contains("RefreshToken"));
  }

  [Fact]
  public void ValidateWithoutRuntimeTokensStillReturnsNoErrors()
  {
    var options = MakeValidOptions();
    options.AccessToken = string.Empty;
    options.RefreshToken = string.Empty;

    var errors = options.Validate();

    _ = errors.Should().BeEmpty();
  }

  [Fact]
  public void ValidateWithoutClientIdReturnsValidationError()
  {
    var options = MakeValidOptions();
    options.ClientId = string.Empty;

    var errors = options.Validate();

    _ = errors.Should().Contain(error => error.Contains("ClientId"));
  }

  [Fact]
  public void ValidateWithoutClientSecretReturnsValidationError()
  {
    var options = MakeValidOptions();
    options.ClientSecret = string.Empty;

    var errors = options.Validate();

    _ = errors.Should().Contain(error => error.Contains("ClientSecret"));
  }

  [Fact]
  public void ValidateWithoutEncryptionKeyReturnsValidationError()
  {
    var options = MakeValidOptions();
    options.EncryptionKey = string.Empty;

    var errors = options.Validate();

    _ = errors.Should().Contain(error => error.Contains("EncryptionKey"));
  }

  [Fact]
  public void ValidateWithoutOAuthCallbackUrlReturnsValidationError()
  {
    var options = MakeValidOptions();
    options.OAuthCallbackUrl = string.Empty;

    var errors = options.Validate();

    _ = errors.Should().Contain(error => error.Contains("OAuthCallbackUrl"));
  }

  [Fact]
  public void ValidateWithoutBotUserIdReturnsNoValidationError()
  {
    var options = MakeValidOptions();
    options.BotUserId = string.Empty;

    var errors = options.Validate();

    _ = errors.Should().NotContain(error => error.Contains("BotUserId"));
  }

  [Fact]
  public void ValidateWithoutChannelIdsReturnsNoValidationError()
  {
    var options = MakeValidOptions();
    options.ChannelIds = string.Empty;

    var errors = options.Validate();

    _ = errors.Should().NotContain(error => error.Contains("ChannelIds"));
  }
}
