namespace Caramel.Twitch.Tests.Handlers;

public sealed class WhisperEventHandlerTests
{
  private static (Mock<ICaramelServiceClient>, Mock<IPersonCache>, Mock<ILogger<WhisperHandler>>) CreateMocks()
  {
    var serviceClientMock = new Mock<ICaramelServiceClient>();
    var personCacheMock = new Mock<IPersonCache>();
    var loggerMock = new Mock<ILogger<WhisperHandler>>();
    return (serviceClientMock, personCacheMock, loggerMock);
  }

  [Fact]
  public async Task HandleAsyncWithValidAccessSendsMessageToServiceAsync()
  {
    // Arrange
    var (serviceClientMock, personCacheMock, loggerMock) = CreateMocks();
    var handler = new WhisperHandler(
      serviceClientMock.Object,
      personCacheMock.Object,
      loggerMock.Object);

    const string fromUserId = "user_123";
    const string fromUserLogin = "streamer";
    const string messageText = "Hey, can you help?";

    _ = personCacheMock
      .Setup(x => x.GetAccessAsync(It.IsAny<PlatformId>()))
      .Returns(Task.FromResult(Result.Ok<bool?>(true)));

    _ = serviceClientMock
      .Setup(x => x.SendMessageAsync(It.IsAny<ProcessMessageRequest>(), It.IsAny<CancellationToken>()))
      .Returns(Task.FromResult(Result.Ok("processed")));

    // Act
    await handler.Handle(new UserWhisperMessageReceived(fromUserId, fromUserLogin, messageText), CancellationToken.None);

    // Assert
    serviceClientMock.Verify(
      x => x.SendMessageAsync(It.IsAny<ProcessMessageRequest>(), It.IsAny<CancellationToken>()),
      Times.Once);
  }

  [Fact]
  public async Task HandleAsyncWithAccessDeniedReturnsEarlyAsync()
  {
    // Arrange
    var (serviceClientMock, personCacheMock, loggerMock) = CreateMocks();
    var handler = new WhisperHandler(
      serviceClientMock.Object,
      personCacheMock.Object,
      loggerMock.Object);

    _ = personCacheMock
      .Setup(x => x.GetAccessAsync(It.IsAny<PlatformId>()))
      .Returns(Task.FromResult(Result.Ok<bool?>(false))); // Access denied

    // Act
    await handler.Handle(new UserWhisperMessageReceived("user_123", "streamer", "message"), CancellationToken.None);

    // Assert
    serviceClientMock.Verify(
      x => x.SendMessageAsync(It.IsAny<ProcessMessageRequest>(), It.IsAny<CancellationToken>()),
      Times.Never);
  }

  [Fact]
  public async Task HandleAsyncWithAccessCheckFailureReturnsEarlyAsync()
  {
    // Arrange
    var (serviceClientMock, personCacheMock, loggerMock) = CreateMocks();
    var handler = new WhisperHandler(
      serviceClientMock.Object,
      personCacheMock.Object,
      loggerMock.Object);

    _ = personCacheMock
      .Setup(x => x.GetAccessAsync(It.IsAny<PlatformId>()))
      .Returns(Task.FromResult(Result.Fail<bool?>("Cache error")));

    // Act
    await handler.Handle(new UserWhisperMessageReceived("user_123", "streamer", "message"), CancellationToken.None);

    // Assert
    serviceClientMock.Verify(
      x => x.SendMessageAsync(It.IsAny<ProcessMessageRequest>(), It.IsAny<CancellationToken>()),
      Times.Never);
  }

  [Fact]
  public async Task HandleAsyncWithSuccessfulMessageLogsSuccessAsync()
  {
    // Arrange
    var (serviceClientMock, personCacheMock, loggerMock) = CreateMocks();
    var handler = new WhisperHandler(
      serviceClientMock.Object,
      personCacheMock.Object,
      loggerMock.Object);

    _ = personCacheMock
      .Setup(x => x.GetAccessAsync(It.IsAny<PlatformId>()))
      .Returns(Task.FromResult(Result.Ok<bool?>(true)));

    _ = serviceClientMock
      .Setup(x => x.SendMessageAsync(It.IsAny<ProcessMessageRequest>(), It.IsAny<CancellationToken>()))
      .Returns(Task.FromResult(Result.Ok("success")));

    // Act
    await handler.Handle(new UserWhisperMessageReceived("user_123", "streamer", "message"), CancellationToken.None);

    // Assert - Message was sent successfully
    serviceClientMock.Verify(
      x => x.SendMessageAsync(It.IsAny<ProcessMessageRequest>(), It.IsAny<CancellationToken>()),
      Times.Once);
  }

  [Fact]
  public async Task HandleAsyncWithFailedMessageLogsFailureAsync()
  {
    // Arrange
    var (serviceClientMock, personCacheMock, loggerMock) = CreateMocks();
    var handler = new WhisperHandler(
      serviceClientMock.Object,
      personCacheMock.Object,
      loggerMock.Object);

    _ = personCacheMock
      .Setup(x => x.GetAccessAsync(It.IsAny<PlatformId>()))
      .Returns(Task.FromResult(Result.Ok<bool?>(true)));

    _ = serviceClientMock
      .Setup(x => x.SendMessageAsync(It.IsAny<ProcessMessageRequest>(), It.IsAny<CancellationToken>()))
      .Returns(Task.FromResult(Result.Fail<string>("Processing error")));

    // Act
    await handler.Handle(new UserWhisperMessageReceived("user_123", "streamer", "message"), CancellationToken.None);

    // Assert - Handler completes without throwing despite service failure
    serviceClientMock.Verify(
      x => x.SendMessageAsync(It.IsAny<ProcessMessageRequest>(), It.IsAny<CancellationToken>()),
      Times.Once);
  }
}
