using Caramel.Twitch.Services;
using Caramel.Domain.Twitch;
using Caramel.Core.ToDos.Requests;
using Caramel.Core.Conversations;
using Caramel.Core.People;
using Caramel.Domain.People;
using Caramel.Domain.ToDos;
using Moq;
using FluentResults;

namespace Caramel.Twitch.Tests.Handlers;

/// <summary>
/// Tests for ChatMessageEventHandler - Twitch channel chat message routing and quick command handling.
/// </summary>
public sealed class ChatMessageEventHandlerTests
{
  private static (
    Mock<ICaramelServiceClient>,
    Mock<IPersonCache>,
    Mock<ITwitchChatBroadcaster>,
    Mock<ITwitchSetupState>,
    Mock<ILogger<ChatMessageEventHandler>>) CreateMocks()
  {
    var serviceClientMock = new Mock<ICaramelServiceClient>();
    var personCacheMock = new Mock<IPersonCache>();
    var broadcasterMock = new Mock<ITwitchChatBroadcaster>();
    var setupStateMock = new Mock<ITwitchSetupState>();
    var loggerMock = new Mock<ILogger<ChatMessageEventHandler>>();
    return (serviceClientMock, personCacheMock, broadcasterMock, setupStateMock, loggerMock);
  }

  private static Task HandleAsync(
    ChatMessageEventHandler handler,
    string broadcasterUserId = "streamer_123",
    string broadcasterLogin = "streamer",
    string chatterUserId = "user_456",
    string chatterLogin = "viewer",
    string chatterDisplayName = "Viewer",
    string messageId = "msg-001",
    string messageText = "hello",
    string color = "#FF0000",
    CancellationToken cancellationToken = default)
  {
    return handler.HandleAsync(
      broadcasterUserId,
      broadcasterLogin,
      chatterUserId,
      chatterLogin,
      chatterDisplayName,
      messageId,
      messageText,
      color,
      cancellationToken);
  }

  [Fact]
  public async Task HandleAsyncWithBotSelfMessageReturnsEarly()
  {
    // Arrange
    var (serviceClientMock, personCacheMock, broadcasterMock, setupStateMock, loggerMock) = CreateMocks();
    
    setupStateMock.Setup(x => x.Current).Returns(new TwitchSetup 
    { 
        BotUserId = "bot_123",
        BotLogin = "caramel_bot",
        Channels = new List<TwitchChannel>(),
        ConfiguredOn = DateTimeOffset.UtcNow,
        UpdatedOn = DateTimeOffset.UtcNow
    });

    var handler = new ChatMessageEventHandler(
      serviceClientMock.Object,
      personCacheMock.Object,
      broadcasterMock.Object,
      setupStateMock.Object,
      loggerMock.Object);

    // Act
    await HandleAsync(
      handler,
      chatterUserId: "bot_123", // Bot's own user ID
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
    var (serviceClientMock, personCacheMock, broadcasterMock, setupStateMock, loggerMock) = CreateMocks();
    var handler = new ChatMessageEventHandler(
      serviceClientMock.Object,
      personCacheMock.Object,
      broadcasterMock.Object,
      setupStateMock.Object,
      loggerMock.Object);

    personCacheMock
      .Setup(x => x.GetAccessAsync(It.IsAny<PlatformId>()))
      .Returns(Task.FromResult(Result.Ok<bool?>(false)));

    // Act
    await HandleAsync(handler, messageText: "!caramel todo test");

    // Assert
    serviceClientMock.Verify(
      x => x.CreateToDoAsync(It.IsAny<CreateToDoRequest>(), It.IsAny<CancellationToken>()),
      Times.Never);
  }

  [Fact]
  public async Task HandleAsyncWithAccessCheckFailureReturnsEarly()
  {
    // Arrange
    var (serviceClientMock, personCacheMock, broadcasterMock, setupStateMock, loggerMock) = CreateMocks();
    var handler = new ChatMessageEventHandler(
      serviceClientMock.Object,
      personCacheMock.Object,
      broadcasterMock.Object,
      setupStateMock.Object,
      loggerMock.Object);

    personCacheMock
      .Setup(x => x.GetAccessAsync(It.IsAny<PlatformId>()))
      .Returns(Task.FromResult(Result.Fail<bool?>("Cache error")));

    // Act
    await HandleAsync(handler, messageText: "!caramel todo test");

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
    var (serviceClientMock, personCacheMock, broadcasterMock, setupStateMock, loggerMock) = CreateMocks();
    var handler = new ChatMessageEventHandler(
      serviceClientMock.Object,
      personCacheMock.Object,
      broadcasterMock.Object,
      setupStateMock.Object,
      loggerMock.Object);

    personCacheMock
      .Setup(x => x.GetAccessAsync(It.IsAny<PlatformId>()))
      .Returns(Task.FromResult(Result.Ok<bool?>(true)));

    // Act
    await HandleAsync(handler, messageText: messageText);

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
    var (serviceClientMock, personCacheMock, broadcasterMock, setupStateMock, loggerMock) = CreateMocks();
    var handler = new ChatMessageEventHandler(
      serviceClientMock.Object,
      personCacheMock.Object,
      broadcasterMock.Object,
      setupStateMock.Object,
      loggerMock.Object);

    personCacheMock
      .Setup(x => x.GetAccessAsync(It.IsAny<PlatformId>()))
      .Returns(Task.FromResult(Result.Ok<bool?>(true)));

    serviceClientMock
      .Setup(x => x.CreateToDoAsync(It.IsAny<CreateToDoRequest>(), It.IsAny<CancellationToken>()))
      .Returns(Task.FromResult(Result.Ok(It.IsAny<ToDo>())));

    // Act
    await HandleAsync(handler, messageText: "!caramel todo buy milk");

    // Assert
    serviceClientMock.Verify(
      x => x.CreateToDoAsync(It.IsAny<CreateToDoRequest>(), It.IsAny<CancellationToken>()),
      Times.Once);
  }

  [Fact]
  public async Task HandleAsyncWithMentionPrefixProcessesCommand()
  {
    // Arrange
    var (serviceClientMock, personCacheMock, broadcasterMock, setupStateMock, loggerMock) = CreateMocks();
    var handler = new ChatMessageEventHandler(
      serviceClientMock.Object,
      personCacheMock.Object,
      broadcasterMock.Object,
      setupStateMock.Object,
      loggerMock.Object);

    personCacheMock
      .Setup(x => x.GetAccessAsync(It.IsAny<PlatformId>()))
      .Returns(Task.FromResult(Result.Ok<bool?>(true)));

    serviceClientMock
      .Setup(x => x.SendMessageAsync(It.IsAny<ProcessMessageRequest>(), It.IsAny<CancellationToken>()))
      .Returns(Task.FromResult(Result.Ok("reply")));

    // Act
    await HandleAsync(handler, messageText: "Hey @caramel, what's up?");

    // Assert
    serviceClientMock.Verify(
      x => x.SendMessageAsync(It.IsAny<ProcessMessageRequest>(), It.IsAny<CancellationToken>()),
      Times.Once);
  }

  [Fact]
  public async Task HandleAsyncAlwaysPublishesToRedisBeforeBotFiltering()
  {
    // Arrange
    var (serviceClientMock, personCacheMock, broadcasterMock, setupStateMock, loggerMock) = CreateMocks();
    var handler = new ChatMessageEventHandler(
      serviceClientMock.Object,
      personCacheMock.Object,
      broadcasterMock.Object,
      setupStateMock.Object,
      loggerMock.Object);

    personCacheMock
      .Setup(x => x.GetAccessAsync(It.IsAny<PlatformId>()))
      .Returns(Task.FromResult(Result.Ok<bool?>(true)));

    serviceClientMock
      .Setup(x => x.SendMessageAsync(It.IsAny<ProcessMessageRequest>(), It.IsAny<CancellationToken>()))
      .Returns(Task.FromResult(Result.Ok("reply")));

    // Act - send a plain message not directed at the bot
    await HandleAsync(handler, messageText: "just a plain chat message");

    // Assert – broadcaster should still have been called
    broadcasterMock.Verify(
      x => x.PublishAsync(
        It.IsAny<string>(),
        It.IsAny<string>(),
        It.IsAny<string>(),
        It.IsAny<string>(),
        It.IsAny<string>(),
        It.IsAny<string>(),
        It.IsAny<string>(),
        It.IsAny<string>(),
        It.IsAny<CancellationToken>()),
      Times.Once);
  }
}
