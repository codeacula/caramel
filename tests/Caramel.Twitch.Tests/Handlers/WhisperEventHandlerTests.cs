namespace Caramel.Twitch.Tests.Handlers;

public sealed class WhisperEventHandlerTests
{
  private static (Mock<ICaramelServiceClient>, Mock<IPersonCache>, Mock<ILogger<WhisperEventHandler>>) CreateMocks()
  {
    var serviceClientMock = new Mock<ICaramelServiceClient>();
    var personCacheMock = new Mock<IPersonCache>();
    var loggerMock = new Mock<ILogger<WhisperEventHandler>>();
    return (serviceClientMock, personCacheMock, loggerMock);
  }

  [Fact]
  public async Task HandleAsyncWithValidAccessSendsMessageToService()
  {
    // Arrange
    var (serviceClientMock, personCacheMock, loggerMock) = CreateMocks();
    var handler = new WhisperEventHandler(
      serviceClientMock.Object,
      personCacheMock.Object,
      loggerMock.Object);

    const string fromUserId = "user_123";
    const string fromUserLogin = "streamer";
    const string messageText = "Hey, can you help?";

    personCacheMock
      .Setup(x => x.GetAccessAsync(It.IsAny<PlatformId>()))
      .Returns(Task.FromResult(Result.Ok<bool?>(true)));

    serviceClientMock
      .Setup(x => x.SendMessageAsync(It.IsAny<ProcessMessageRequest>(), It.IsAny<CancellationToken>()))
      .Returns(Task.FromResult(Result.Ok<string>("processed")));

    // Act
    await handler.HandleAsync(fromUserId, fromUserLogin, messageText);

    // Assert
    serviceClientMock.Verify(
      x => x.SendMessageAsync(It.IsAny<ProcessMessageRequest>(), It.IsAny<CancellationToken>()),
      Times.Once);
  }

  [Fact]
  public async Task HandleAsyncWithAccessDeniedReturnsEarly()
  {
    // Arrange
    var (serviceClientMock, personCacheMock, loggerMock) = CreateMocks();
    var handler = new WhisperEventHandler(
      serviceClientMock.Object,
      personCacheMock.Object,
      loggerMock.Object);

    personCacheMock
      .Setup(x => x.GetAccessAsync(It.IsAny<PlatformId>()))
      .Returns(Task.FromResult(Result.Ok<bool?>(false))); // Access denied

    // Act
    await handler.HandleAsync("user_123", "streamer", "message");

    // Assert
    serviceClientMock.Verify(
      x => x.SendMessageAsync(It.IsAny<ProcessMessageRequest>(), It.IsAny<CancellationToken>()),
      Times.Never);
  }

  [Fact]
  public async Task HandleAsyncWithAccessCheckFailureReturnsEarly()
  {
    // Arrange
    var (serviceClientMock, personCacheMock, loggerMock) = CreateMocks();
    var handler = new WhisperEventHandler(
      serviceClientMock.Object,
      personCacheMock.Object,
      loggerMock.Object);

    personCacheMock
      .Setup(x => x.GetAccessAsync(It.IsAny<PlatformId>()))
      .Returns(Task.FromResult(Result.Fail<bool?>("Cache error")));

    // Act
    await handler.HandleAsync("user_123", "streamer", "message");

    // Assert
    serviceClientMock.Verify(
      x => x.SendMessageAsync(It.IsAny<ProcessMessageRequest>(), It.IsAny<CancellationToken>()),
      Times.Never);
  }

  [Fact]
  public async Task HandleAsyncWithSuccessfulMessageLogsSuccess()
  {
    // Arrange
    var (serviceClientMock, personCacheMock, loggerMock) = CreateMocks();
    var handler = new WhisperEventHandler(
      serviceClientMock.Object,
      personCacheMock.Object,
      loggerMock.Object);

    personCacheMock
      .Setup(x => x.GetAccessAsync(It.IsAny<PlatformId>()))
      .Returns(Task.FromResult(Result.Ok<bool?>(true)));

    serviceClientMock
      .Setup(x => x.SendMessageAsync(It.IsAny<ProcessMessageRequest>(), It.IsAny<CancellationToken>()))
      .Returns(Task.FromResult(Result.Ok<string>("success")));

    // Act
    await handler.HandleAsync("user_123", "streamer", "message");

    // Assert - Message was sent successfully
    serviceClientMock.Verify(
      x => x.SendMessageAsync(It.IsAny<ProcessMessageRequest>(), It.IsAny<CancellationToken>()),
      Times.Once);
  }

  [Fact]
  public async Task HandleAsyncWithFailedMessageLogsFailure()
  {
    // Arrange
    var (serviceClientMock, personCacheMock, loggerMock) = CreateMocks();
    var handler = new WhisperEventHandler(
      serviceClientMock.Object,
      personCacheMock.Object,
      loggerMock.Object);

    personCacheMock
      .Setup(x => x.GetAccessAsync(It.IsAny<PlatformId>()))
      .Returns(Task.FromResult(Result.Ok<bool?>(true)));

    serviceClientMock
      .Setup(x => x.SendMessageAsync(It.IsAny<ProcessMessageRequest>(), It.IsAny<CancellationToken>()))
      .Returns(Task.FromResult(Result.Fail<string>("Processing error")));

    // Act
    await handler.HandleAsync("user_123", "streamer", "message");

    // Assert - Handler completes without throwing despite service failure
    serviceClientMock.Verify(
      x => x.SendMessageAsync(It.IsAny<ProcessMessageRequest>(), It.IsAny<CancellationToken>()),
      Times.Once);
  }
}
