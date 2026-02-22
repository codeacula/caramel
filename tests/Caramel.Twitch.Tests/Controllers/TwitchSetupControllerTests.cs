using Caramel.Core.API;
using Caramel.Domain.Twitch;
using Caramel.Twitch.Controllers;
using Caramel.Twitch.Services;

using FluentResults;

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Caramel.Twitch.Tests.Controllers;

public sealed class TwitchSetupControllerTests
{
  private readonly Mock<ITwitchSetupState> _setupState = new();
  private readonly Mock<ITwitchUserResolver> _userResolver = new();
  private readonly Mock<ICaramelServiceClient> _serviceClient = new();
  private readonly Mock<ITwitchChatBroadcaster> _broadcaster = new();
  private readonly Mock<ILogger<TwitchSetupController>> _logger = new();

  private TwitchSetupController CreateController() =>
    new(_setupState.Object, _userResolver.Object, _serviceClient.Object, _broadcaster.Object, _logger.Object);

  private static TwitchSetup MakeSetup(string botLogin = "caramel_bot", string botUserId = "111") =>
    new()
    {
      BotUserId = botUserId,
      BotLogin = botLogin,
      Channels = [new TwitchChannel { UserId = "999", Login = "streamer" }],
      ConfiguredOn = DateTimeOffset.UtcNow,
      UpdatedOn = DateTimeOffset.UtcNow,
    };

  // -----------------------------------------------------------------------
  // GET /twitch/setup
  // -----------------------------------------------------------------------

  [Fact]
  public void GetSetupAsync_ReturnsIsConfiguredFalse_WhenStateIsEmpty()
  {
    _setupState.Setup(s => s.Current).Returns((TwitchSetup?)null);
    var controller = CreateController();

    var result = controller.GetSetupAsync();

    var ok = result.Should().BeOfType<OkObjectResult>().Subject;
    var response = ok.Value.Should().BeOfType<TwitchSetupStatusResponse>().Subject;
    response.IsConfigured.Should().BeFalse();
    response.BotLogin.Should().BeNull();
    response.ChannelLogins.Should().BeNull();
  }

  [Fact]
  public void GetSetupAsync_ReturnsIsConfiguredTrue_WithBotLoginAndChannels_WhenStateIsSet()
  {
    var setup = MakeSetup();
    _setupState.Setup(s => s.Current).Returns(setup);
    var controller = CreateController();

    var result = controller.GetSetupAsync();

    var ok = result.Should().BeOfType<OkObjectResult>().Subject;
    var response = ok.Value.Should().BeOfType<TwitchSetupStatusResponse>().Subject;
    response.IsConfigured.Should().BeTrue();
    response.BotLogin.Should().Be("caramel_bot");
    response.ChannelLogins.Should().ContainSingle(l => l == "streamer");
  }

  // -----------------------------------------------------------------------
  // POST /twitch/setup — validation failures
  // -----------------------------------------------------------------------

  [Fact]
  public async Task SaveSetupAsync_ReturnsBadRequest_WhenBotLoginIsEmpty()
  {
    var controller = CreateController();
    var request = new SaveTwitchSetupRequest("", ["streamer"]);

    var result = await controller.SaveSetupAsync(request, CancellationToken.None);

    result.Should().BeOfType<BadRequestObjectResult>();
  }

  [Fact]
  public async Task SaveSetupAsync_ReturnsBadRequest_WhenBotLoginIsWhiteSpace()
  {
    var controller = CreateController();
    var request = new SaveTwitchSetupRequest("   ", ["streamer"]);

    var result = await controller.SaveSetupAsync(request, CancellationToken.None);

    result.Should().BeOfType<BadRequestObjectResult>();
  }

  [Fact]
  public async Task SaveSetupAsync_ReturnsBadRequest_WhenChannelLoginsIsEmpty()
  {
    var controller = CreateController();
    var request = new SaveTwitchSetupRequest("caramel_bot", []);

    var result = await controller.SaveSetupAsync(request, CancellationToken.None);

    result.Should().BeOfType<BadRequestObjectResult>();
  }

  // -----------------------------------------------------------------------
  // POST /twitch/setup — resolver failures
  // -----------------------------------------------------------------------

  [Fact]
  public async Task SaveSetupAsync_ReturnsBadRequest_WhenResolverThrowsInvalidOperationException()
  {
    _userResolver
      .Setup(r => r.ResolveUserIdAsync("caramel_bot", It.IsAny<CancellationToken>()))
      .ThrowsAsync(new InvalidOperationException("User not found"));
    _userResolver
      .Setup(r => r.ResolveUserIdsAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
      .ReturnsAsync(["999"]);

    var controller = CreateController();
    var request = new SaveTwitchSetupRequest("caramel_bot", ["streamer"]);

    var result = await controller.SaveSetupAsync(request, CancellationToken.None);

    result.Should().BeOfType<BadRequestObjectResult>();
  }

  // -----------------------------------------------------------------------
  // POST /twitch/setup — service client failure
  // -----------------------------------------------------------------------

  [Fact]
  public async Task SaveSetupAsync_ReturnsProblem_WhenServiceClientFails()
  {
    _userResolver
      .Setup(r => r.ResolveUserIdAsync("caramel_bot", It.IsAny<CancellationToken>()))
      .ReturnsAsync("111");
    _userResolver
      .Setup(r => r.ResolveUserIdsAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
      .ReturnsAsync(["999"]);
    _serviceClient
      .Setup(c => c.SaveTwitchSetupAsync(It.IsAny<TwitchSetup>(), It.IsAny<CancellationToken>()))
      .ReturnsAsync(Result.Fail<TwitchSetup>("DB error"));

    var controller = CreateController();
    var request = new SaveTwitchSetupRequest("caramel_bot", ["streamer"]);

    var result = await controller.SaveSetupAsync(request, CancellationToken.None);

    result.Should().BeOfType<ObjectResult>()
      .Which.StatusCode.Should().Be(500);
  }

  // -----------------------------------------------------------------------
  // POST /twitch/setup — happy path
  // -----------------------------------------------------------------------

  [Fact]
  public async Task SaveSetupAsync_ReturnsOk_AndUpdatesState_AndPublishesBroadcast_WhenValid()
  {
    var savedSetup = MakeSetup();

    _userResolver
      .Setup(r => r.ResolveUserIdAsync("caramel_bot", It.IsAny<CancellationToken>()))
      .ReturnsAsync("111");
    _userResolver
      .Setup(r => r.ResolveUserIdsAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
      .ReturnsAsync(["999"]);
    _serviceClient
      .Setup(c => c.SaveTwitchSetupAsync(It.IsAny<TwitchSetup>(), It.IsAny<CancellationToken>()))
      .ReturnsAsync(Result.Ok(savedSetup));
    _broadcaster
      .Setup(b => b.PublishSystemMessageAsync(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<CancellationToken>()))
      .Returns(Task.CompletedTask);

    var controller = CreateController();
    var request = new SaveTwitchSetupRequest("caramel_bot", ["streamer"]);

    var result = await controller.SaveSetupAsync(request, CancellationToken.None);

    var ok = result.Should().BeOfType<OkObjectResult>().Subject;
    var response = ok.Value.Should().BeOfType<TwitchSetupStatusResponse>().Subject;
    response.IsConfigured.Should().BeTrue();

    _setupState.Verify(s => s.Update(savedSetup), Times.Once);
    _broadcaster.Verify(
      b => b.PublishSystemMessageAsync("setup_status", It.IsAny<object>(), It.IsAny<CancellationToken>()),
      Times.Once);
  }

  // -----------------------------------------------------------------------
  // POST /twitch/setup — unexpected exception
  // -----------------------------------------------------------------------

  [Fact]
  public async Task SaveSetupAsync_ReturnsProblem_WhenUnexpectedExceptionOccurs()
  {
    _userResolver
      .Setup(r => r.ResolveUserIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
      .ThrowsAsync(new Exception("Boom!"));
    _userResolver
      .Setup(r => r.ResolveUserIdsAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
      .ReturnsAsync(["999"]);

    var controller = CreateController();
    var request = new SaveTwitchSetupRequest("caramel_bot", ["streamer"]);

    var result = await controller.SaveSetupAsync(request, CancellationToken.None);

    result.Should().BeOfType<ObjectResult>()
      .Which.StatusCode.Should().Be(500);
  }
}
