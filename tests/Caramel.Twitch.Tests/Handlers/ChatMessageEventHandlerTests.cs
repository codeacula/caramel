namespace Caramel.Twitch.Tests.Handlers;

/// <summary>
/// Tests for ChatMessageEventHandler - Twitch channel chat message routing and quick command handling.
/// </summary>
public sealed class ChatMessageEventHandlerTests
{
  private static (Mock<ICaramelServiceClient>, Mock<IPersonCache>, Mock<ILogger<ChatMessageEventHandler>>) CreateMocks()
  {
    var serviceClientMock = new Mock<ICaramelServiceClient>();
    var personCacheMock = new Mock<IPersonCache>();
    var loggerMock = new Mock<ILogger<ChatMessageEventHandler>>();
    return (serviceClientMock, personCacheMock, loggerMock);
  }

  [Fact]
  public async Task HandleAsyncWithBotSelfMessageReturnsEarly()
  {
    // Arrange
    var (serviceClientMock, personCacheMock, loggerMock) = CreateMocks();
    var handler = new ChatMessageEventHandler(
      serviceClientMock.Object,
      personCacheMock.Object,
      loggerMock.Object);

    // Act
    await handler.HandleAsync(
      broadcasterUserId: "streamer_123",
      broadcasterLogin: "streamer",
      chatterUserId: "bot_id",
      chatterLogin: "!caramel", // Bot's own login
      messageText: "!caramel todo test");

    // Assert
    personCacheMock.Verify(
      x => x.GetAccessAsync(It.IsAny<PlatformId>()),
      Times.Never); // Cache should not be accessed
  }

  [Fact]
  public async Task HandleAsyncWithAccessDeniedReturnsEarly()
  {
    // Arrange
    var (serviceClientMock, personCacheMock, loggerMock) = CreateMocks();
    var handler = new ChatMessageEventHandler(
      serviceClientMock.Object,
      personCacheMock.Object,
      loggerMock.Object);

    personCacheMock
      .Setup(x => x.GetAccessAsync(It.IsAny<PlatformId>()))
      .Returns(Task.FromResult(Result.Ok<bool?>(false)));

    // Act
    await handler.HandleAsync(
      broadcasterUserId: "streamer_123",
      broadcasterLogin: "streamer",
      chatterUserId: "user_456",
      chatterLogin: "viewer",
      messageText: "!caramel todo test");

    // Assert
    serviceClientMock.Verify(
      x => x.CreateToDoAsync(It.IsAny<CreateToDoRequest>(), It.IsAny<CancellationToken>()),
      Times.Never);
  }

  [Fact]
  public async Task HandleAsyncWithAccessCheckFailureReturnsEarly()
  {
    // Arrange
    var (serviceClientMock, personCacheMock, loggerMock) = CreateMocks();
    var handler = new ChatMessageEventHandler(
      serviceClientMock.Object,
      personCacheMock.Object,
      loggerMock.Object);

    personCacheMock
      .Setup(x => x.GetAccessAsync(It.IsAny<PlatformId>()))
      .Returns(Task.FromResult(Result.Fail<bool?>("Cache error")));

    // Act
    await handler.HandleAsync(
      broadcasterUserId: "streamer_123",
      broadcasterLogin: "streamer",
      chatterUserId: "user_456",
      chatterLogin: "viewer",
      messageText: "!caramel todo test");

    // Assert
    serviceClientMock.Verify(
      x => x.CreateToDoAsync(It.IsAny<CreateToDoRequest>(), It.IsAny<CancellationToken>()),
      Times.Never);
  }

  [Theory]
  [InlineData("just a random message")]
  [InlineData("hello everyone")]
  [InlineData("caramel is cool")]
  public async Task HandleAsyncWithMessageNotDirectedAtBotReturnsEarly(string messageText)
  {
    // Arrange
    var (serviceClientMock, personCacheMock, loggerMock) = CreateMocks();
    var handler = new ChatMessageEventHandler(
      serviceClientMock.Object,
      personCacheMock.Object,
      loggerMock.Object);

    personCacheMock
      .Setup(x => x.GetAccessAsync(It.IsAny<PlatformId>()))
      .Returns(Task.FromResult(Result.Ok<bool?>(true)));

    // Act
    await handler.HandleAsync(
      broadcasterUserId: "streamer_123",
      broadcasterLogin: "streamer",
      chatterUserId: "user_456",
      chatterLogin: "viewer",
      messageText: messageText);

    // Assert
    serviceClientMock.Verify(
      x => x.CreateToDoAsync(It.IsAny<CreateToDoRequest>(), It.IsAny<CancellationToken>()),
      Times.Never);
    serviceClientMock.Verify(
      x => x.SendMessageAsync(It.IsAny<ProcessMessageRequest>(), It.IsAny<CancellationToken>()),
      Times.Never);
  }

  [Fact]
  public async Task HandleAsyncWithBotCommandPrefixProcessesCommand()
  {
    // Arrange
    var (serviceClientMock, personCacheMock, loggerMock) = CreateMocks();
    var handler = new ChatMessageEventHandler(
      serviceClientMock.Object,
      personCacheMock.Object,
      loggerMock.Object);

    personCacheMock
      .Setup(x => x.GetAccessAsync(It.IsAny<PlatformId>()))
      .Returns(Task.FromResult(Result.Ok<bool?>(true)));

    serviceClientMock
      .Setup(x => x.CreateToDoAsync(It.IsAny<CreateToDoRequest>(), It.IsAny<CancellationToken>()))
      .Returns(Task.FromResult(Result.Ok(It.IsAny<ToDo>())));

    // Act
    await handler.HandleAsync(
      broadcasterUserId: "streamer_123",
      broadcasterLogin: "streamer",
      chatterUserId: "user_456",
      chatterLogin: "viewer",
      messageText: "!caramel todo buy milk");

    // Assert
    serviceClientMock.Verify(
      x => x.CreateToDoAsync(It.IsAny<CreateToDoRequest>(), It.IsAny<CancellationToken>()),
      Times.Once);
  }

  [Fact]
  public async Task HandleAsyncWithMentionPrefixProcessesCommand()
  {
    // Arrange
    var (serviceClientMock, personCacheMock, loggerMock) = CreateMocks();
    var handler = new ChatMessageEventHandler(
      serviceClientMock.Object,
      personCacheMock.Object,
      loggerMock.Object);

    personCacheMock
      .Setup(x => x.GetAccessAsync(It.IsAny<PlatformId>()))
      .Returns(Task.FromResult(Result.Ok<bool?>(true)));

    serviceClientMock
      .Setup(x => x.SendMessageAsync(It.IsAny<ProcessMessageRequest>(), It.IsAny<CancellationToken>()))
      .Returns(Task.FromResult(Result.Ok("reply")));

    // Act
    await handler.HandleAsync(
      broadcasterUserId: "streamer_123",
      broadcasterLogin: "streamer",
      chatterUserId: "user_456",
      chatterLogin: "viewer",
      messageText: "Hey @caramel, what's up?");

    // Assert
    serviceClientMock.Verify(
      x => x.SendMessageAsync(It.IsAny<ProcessMessageRequest>(), It.IsAny<CancellationToken>()),
      Times.Once);
  }
}
