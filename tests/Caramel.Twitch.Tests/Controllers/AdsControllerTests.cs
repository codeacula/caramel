using Caramel.Domain.Twitch;
using Caramel.Twitch.Controllers;
using Caramel.Twitch.Services;

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

  private AdsController CreateController()
  {
    var tokenManager = new TwitchTokenManager(DefaultConfig, _mockHttpClientFactory.Object, new Mock<ILogger<TwitchTokenManager>>().Object);
    return new AdsController(tokenManager, DefaultConfig, _mockSetupState.Object, _mockHttpClientFactory.Object, _mockLogger.Object);
  }

  [Fact]
  public async Task RunAdsAsyncWhenSetupIsNullReturnsServiceUnavailable()
  {
    _ = _mockSetupState.Setup(s => s.Current).Returns((TwitchSetup?)null);
    var controller = CreateController();
    var request = new RunAdsRequest { Duration = 30 };

    var result = await controller.RunAdsAsync(request, CancellationToken.None);

    result.Should().BeOfType<ObjectResult>()
      .Which.StatusCode.Should().Be(503);
  }

  [Fact]
  public async Task RunAdsAsyncWhenNoChannelsConfiguredReturnsBadRequest()
  {
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

    var result = await controller.RunAdsAsync(request, CancellationToken.None);

    result.Should().BeOfType<BadRequestObjectResult>();
  }

  [Theory]
  [InlineData(0)]
  [InlineData(15)]
  [InlineData(45)]
  [InlineData(200)]
  [InlineData(-30)]
  public async Task RunAdsAsyncWithInvalidDurationReturnsBadRequest(int invalidDuration)
  {
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

    var result = await controller.RunAdsAsync(request, CancellationToken.None);

    result.Should().BeOfType<BadRequestObjectResult>();
  }
}
