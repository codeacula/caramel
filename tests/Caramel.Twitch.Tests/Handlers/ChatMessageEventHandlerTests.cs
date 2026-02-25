using Caramel.Domain.Twitch;
using Caramel.Twitch.Services;

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
    Mock<ILogger<ChatMessageHandler>>) CreateMocks()
  {
    var serviceClientMock = new Mock<ICaramelServiceClient>();
    var personCacheMock = new Mock<IPersonCache>();
    var broadcasterMock = new Mock<ITwitchChatBroadcaster>();
    var setupStateMock = new Mock<ITwitchSetupState>();
    var loggerMock = new Mock<ILogger<ChatMessageHandler>>();
    return (serviceClientMock, personCacheMock, broadcasterMock, setupStateMock, loggerMock);
  }

  private static Task HandleAsync(
    ChatMessageHandler handler,
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
  public async Task HandleAsyncWithBotSelfMessageReturnsEarlyAsync()
  {
    // Arrange
    var (serviceClientMock, personCacheMock, broadcasterMock, setupStateMock, loggerMock) = CreateMocks();

    _ = setupStateMock.Setup(x => x.Current).Returns(new TwitchSetup
    {
      BotUserId = "bot_123",
      BotLogin = "caramel_bot",
      Channels = [],
      ConfiguredOn = DateTimeOffset.UtcNow,
      UpdatedOn = DateTimeOffset.UtcNow
    });

    var handler = new ChatMessageHandler(
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
  public async Task HandleAsyncWithMentionPrefixProcessesCommandAsync()
  {
    // Arrange
    var (serviceClientMock, personCacheMock, broadcasterMock, setupStateMock, loggerMock) = CreateMocks();
    var handler = new ChatMessageHandler(
      serviceClientMock.Object,
      personCacheMock.Object,
      broadcasterMock.Object,
      setupStateMock.Object,
      loggerMock.Object);

    _ = personCacheMock
      .Setup(x => x.GetAccessAsync(It.IsAny<PlatformId>()))
      .Returns(Task.FromResult(Result.Ok<bool?>(true)));

    _ = serviceClientMock
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
  public async Task HandleAsyncAlwaysPublishesToRedisBeforeBotFilteringAsync()
  {
    // Arrange
    var (serviceClientMock, personCacheMock, broadcasterMock, setupStateMock, loggerMock) = CreateMocks();
    var handler = new ChatMessageHandler(
      serviceClientMock.Object,
      personCacheMock.Object,
      broadcasterMock.Object,
      setupStateMock.Object,
      loggerMock.Object);

    _ = personCacheMock
      .Setup(x => x.GetAccessAsync(It.IsAny<PlatformId>()))
      .Returns(Task.FromResult(Result.Ok<bool?>(true)));

    _ = serviceClientMock
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
