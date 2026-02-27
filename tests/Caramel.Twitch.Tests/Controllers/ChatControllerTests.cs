namespace Caramel.Twitch.Tests.Controllers;

public sealed class ChatControllerTests
{
  private readonly Mock<ITwitchChatClient> _mockChatClient = new();
  private readonly Mock<ILogger<ChatController>> _mockLogger = new();

  private ChatController CreateController()
  {
    return new ChatController(_mockChatClient.Object, _mockLogger.Object);
  }

  [Fact]
  public async Task SendAsyncWithEmptyMessageReturnsBadRequestAsync()
  {
    // Arrange
    var controller = CreateController();
    var request = new SendChatMessageRequest("   ");

    // Act
    var result = await controller.SendAsync(request, CancellationToken.None);

    // Assert
    _ = result.Should().BeOfType<BadRequestObjectResult>();
  }

  [Fact]
  public async Task SendAsyncWithMessageExceedingLimitReturnsBadRequestAsync()
  {
    // Arrange
    var controller = CreateController();
    var request = new SendChatMessageRequest(new string('x', 501));

    // Act
    var result = await controller.SendAsync(request, CancellationToken.None);

    // Assert
    _ = result.Should().BeOfType<BadRequestObjectResult>();
  }

  [Fact]
  public async Task SendAsyncWithValidMessageDelegatesToChatClientAsync()
  {
    // Arrange
    _ = _mockChatClient
      .Setup(x => x.SendChatMessageAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
      .ReturnsAsync(Result.Ok());
    var controller = CreateController();
    var request = new SendChatMessageRequest("Hello chat!");

    // Act
    var result = await controller.SendAsync(request, CancellationToken.None);

    // Assert
    _ = result.Should().BeOfType<OkResult>();
    _mockChatClient.Verify(
      x => x.SendChatMessageAsync("Hello chat!", It.IsAny<CancellationToken>()),
      Times.Once);
  }

  [Fact]
  public async Task SendAsyncWhenChatClientFailsReturnsProblemAsync()
  {
    // Arrange
    _ = _mockChatClient
      .Setup(x => x.SendChatMessageAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
      .ReturnsAsync(Result.Fail("Twitch API rejected the message."));
    var controller = CreateController();
    var request = new SendChatMessageRequest("Hello chat!");

    // Act
    var result = await controller.SendAsync(request, CancellationToken.None);

    // Assert
    _ = result.Should().BeOfType<ObjectResult>()
      .Which.StatusCode.Should().Be(500);
  }

  [Fact]
  public async Task SendAsyncWithExactly500CharsSucceedsAsync()
  {
    // Arrange
    _ = _mockChatClient
      .Setup(x => x.SendChatMessageAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
      .ReturnsAsync(Result.Ok());
    var controller = CreateController();
    var request = new SendChatMessageRequest(new string('a', 500));

    // Act
    var result = await controller.SendAsync(request, CancellationToken.None);

    // Assert
    _ = result.Should().BeOfType<OkResult>();
  }
}
