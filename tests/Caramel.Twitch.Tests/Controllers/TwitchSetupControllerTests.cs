using Caramel.Domain.Twitch;
using Caramel.Twitch.Controllers;
using Caramel.Twitch.Services;

using Microsoft.AspNetCore.Mvc;

namespace Caramel.Twitch.Tests.Controllers;

public sealed class TwitchSetupControllerTests
{
  private readonly Mock<ITwitchSetupState> _setupState = new();
  private readonly Mock<ITwitchUserResolver> _userResolver = new();
  private readonly Mock<ICaramelServiceClient> _serviceClient = new();
  private readonly Mock<ITwitchChatBroadcaster> _broadcaster = new();
  private readonly Mock<ILogger<TwitchSetupController>> _logger = new();

  private TwitchSetupController CreateController()
  {
    return new(_setupState.Object, _userResolver.Object, _serviceClient.Object, _broadcaster.Object, _logger.Object);
  }

  private static TwitchSetup MakeSetup(string botLogin = "caramel_bot", string botUserId = "111")
  {
    return new()
    {
      BotUserId = botUserId,
      BotLogin = botLogin,
      Channels = [new TwitchChannel { UserId = "999", Login = "streamer" }],
      ConfiguredOn = DateTimeOffset.UtcNow,
      UpdatedOn = DateTimeOffset.UtcNow,
    };
  }

  // -----------------------------------------------------------------------
  // GET /twitch/setup
  // -----------------------------------------------------------------------

  [Fact]
  public void GetSetupAsyncReturnsIsConfiguredFalseWhenStateIsEmpty()
  {
    _ = _setupState.Setup(s => s.Current).Returns((TwitchSetup?)null);
    var controller = CreateController();

    var result = controller.GetSetup();

    var ok = result.Should().BeOfType<OkObjectResult>().Subject;
    var response = ok.Value.Should().BeOfType<TwitchSetupStatusResponse>().Subject;
    _ = response.IsConfigured.Should().BeFalse();
    _ = response.BotLogin.Should().BeNull();
    _ = response.ChannelLogins.Should().BeNull();
  }

  [Fact]
  public void GetSetupAsyncReturnsIsConfiguredTrueWithBotLoginAndChannelsWhenStateIsSet()
  {
    var setup = MakeSetup();
    _ = _setupState.Setup(s => s.Current).Returns(setup);
    var controller = CreateController();

    var result = controller.GetSetup();

    var ok = result.Should().BeOfType<OkObjectResult>().Subject;
    var response = ok.Value.Should().BeOfType<TwitchSetupStatusResponse>().Subject;
    _ = response.IsConfigured.Should().BeTrue();
    _ = response.BotLogin.Should().Be("caramel_bot");
    _ = response.ChannelLogins.Should().ContainSingle(l => l == "streamer");
  }

  // -----------------------------------------------------------------------
  // POST /twitch/setup — validation failures
  // -----------------------------------------------------------------------

  [Fact]
  public async Task SaveSetupAsyncReturnsBadRequestWhenBotLoginIsEmptyAsync()
  {
    var controller = CreateController();
    var request = new SaveTwitchSetupRequest("", ["streamer"]);

    var result = await controller.SaveSetupAsync(request, CancellationToken.None);

    _ = result.Should().BeOfType<BadRequestObjectResult>();
  }

  [Fact]
  public async Task SaveSetupAsyncReturnsBadRequestWhenBotLoginIsWhiteSpaceAsync()
  {
    var controller = CreateController();
    var request = new SaveTwitchSetupRequest("   ", ["streamer"]);

    var result = await controller.SaveSetupAsync(request, CancellationToken.None);

    _ = result.Should().BeOfType<BadRequestObjectResult>();
  }

  [Fact]
  public async Task SaveSetupAsyncReturnsBadRequestWhenChannelLoginsIsEmptyAsync()
  {
    var controller = CreateController();
    var request = new SaveTwitchSetupRequest("caramel_bot", []);

    var result = await controller.SaveSetupAsync(request, CancellationToken.None);

    _ = result.Should().BeOfType<BadRequestObjectResult>();
  }

  // -----------------------------------------------------------------------
  // POST /twitch/setup — resolver failures
  // -----------------------------------------------------------------------

  [Fact]
  public async Task SaveSetupAsyncReturnsBadRequestWhenResolverThrowsInvalidOperationExceptionAsync()
  {
    _ = _userResolver
      .Setup(r => r.ResolveUserIdAsync("caramel_bot", It.IsAny<CancellationToken>()))
      .ThrowsAsync(new InvalidOperationException("User not found"));
    _ = _userResolver
      .Setup(r => r.ResolveUserIdsAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
      .ReturnsAsync(["999"]);

    var controller = CreateController();
    var request = new SaveTwitchSetupRequest("caramel_bot", ["streamer"]);

    var result = await controller.SaveSetupAsync(request, CancellationToken.None);

    _ = result.Should().BeOfType<BadRequestObjectResult>();
  }

  // -----------------------------------------------------------------------
  // POST /twitch/setup — service client failure
  // -----------------------------------------------------------------------

  [Fact]
  public async Task SaveSetupAsyncReturnsProblemWhenServiceClientFailsAsync()
  {
    _ = _userResolver
      .Setup(r => r.ResolveUserIdAsync("caramel_bot", It.IsAny<CancellationToken>()))
      .ReturnsAsync("111");
    _ = _userResolver
      .Setup(r => r.ResolveUserIdsAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
      .ReturnsAsync(["999"]);
    _ = _serviceClient
      .Setup(c => c.SaveTwitchSetupAsync(It.IsAny<TwitchSetup>(), It.IsAny<CancellationToken>()))
      .ReturnsAsync(Result.Fail<TwitchSetup>("DB error"));

    var controller = CreateController();
    var request = new SaveTwitchSetupRequest("caramel_bot", ["streamer"]);

    var result = await controller.SaveSetupAsync(request, CancellationToken.None);

    _ = result.Should().BeOfType<ObjectResult>()
      .Which.StatusCode.Should().Be(500);
  }

  // -----------------------------------------------------------------------
  // POST /twitch/setup — happy path
  // -----------------------------------------------------------------------

  [Fact]
  public async Task SaveSetupAsyncReturnsOkAndUpdatesStateAndPublishesBroadcastWhenValidAsync()
  {
    var savedSetup = MakeSetup();

    _ = _userResolver
      .Setup(r => r.ResolveUserIdAsync("caramel_bot", It.IsAny<CancellationToken>()))
      .ReturnsAsync("111");
    _ = _userResolver
      .Setup(r => r.ResolveUserIdsAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
      .ReturnsAsync(["999"]);
    _ = _serviceClient
      .Setup(c => c.SaveTwitchSetupAsync(It.IsAny<TwitchSetup>(), It.IsAny<CancellationToken>()))
      .ReturnsAsync(Result.Ok(savedSetup));
    _ = _broadcaster
      .Setup(b => b.PublishSystemMessageAsync(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<CancellationToken>()))
      .Returns(Task.CompletedTask);

    var controller = CreateController();
    var request = new SaveTwitchSetupRequest("caramel_bot", ["streamer"]);

    var result = await controller.SaveSetupAsync(request, CancellationToken.None);

    var ok = result.Should().BeOfType<OkObjectResult>().Subject;
    var response = ok.Value.Should().BeOfType<TwitchSetupStatusResponse>().Subject;
    _ = response.IsConfigured.Should().BeTrue();

    _setupState.Verify(s => s.Update(savedSetup), Times.Once);
    _broadcaster.Verify(
      b => b.PublishSystemMessageAsync("setup_status", It.IsAny<object>(), It.IsAny<CancellationToken>()),
      Times.Once);
  }

  // -----------------------------------------------------------------------
  // POST /twitch/setup — unexpected exception
  // -----------------------------------------------------------------------

  [Fact]
  public async Task SaveSetupAsyncReturnsProblemWhenUnexpectedExceptionOccursAsync()
  {
    _ = _userResolver
      .Setup(r => r.ResolveUserIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
      .ThrowsAsync(new HttpRequestException("Unexpected error"));
    _ = _userResolver
      .Setup(r => r.ResolveUserIdsAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
      .ReturnsAsync(["999"]);

    var controller = CreateController();
    var request = new SaveTwitchSetupRequest("caramel_bot", ["streamer"]);

    var result = await controller.SaveSetupAsync(request, CancellationToken.None);

    _ = result.Should().BeOfType<ObjectResult>()
      .Which.StatusCode.Should().Be(500);
  }
}
