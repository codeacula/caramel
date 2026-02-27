namespace Caramel.Twitch.Tests.Handlers;

public sealed class ChannelPointRedeemHandlerTests
{
  [Fact]
  public async Task HandleWithMatchingAiRewardCallsAskTheOrbAsync()
  {
    // Arrange
    var messageTheAiRewardId = Guid.NewGuid();
    var serviceClientMock = new Mock<ICaramelServiceClient>();
    var broadcasterMock = new Mock<ITwitchChatBroadcaster>();
    var chatClientMock = new Mock<ITwitchChatClient>();
    var loggerMock = new Mock<ILogger<ChannelPointRedeemHandler>>();
    var config = CreateTwitchConfig(messageTheAiRewardId.ToString());

    _ = serviceClientMock
      .Setup(x => x.AskTheOrbAsync(It.IsAny<AskTheOrbRequest>(), It.IsAny<CancellationToken>()))
      .ReturnsAsync(Result.Ok("The orb has spoken."));

    _ = chatClientMock
      .Setup(x => x.SendChatMessageAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
      .ReturnsAsync(Result.Ok());

    var handler = new ChannelPointRedeemHandler(serviceClientMock.Object, broadcasterMock.Object, chatClientMock.Object, config, loggerMock.Object);
    var notification = CreateRedeemNotification(
      rewardId: messageTheAiRewardId.ToString(),
      userInput: "Should I touch grass today?");

    // Act
    await handler.Handle(notification, CancellationToken.None);

    // Assert
    broadcasterMock.Verify(
      x => x.PublishRedeemAsync(
        It.IsAny<string>(),
        It.IsAny<string>(),
        It.IsAny<string>(),
        It.IsAny<string>(),
        It.IsAny<string>(),
        It.IsAny<string>(),
        It.IsAny<string>(),
        It.IsAny<string>(),
        It.IsAny<int>(),
        It.IsAny<string>(),
        It.IsAny<DateTimeOffset>(),
        It.IsAny<CancellationToken>()),
      Times.Once);

    serviceClientMock.Verify(
      x => x.AskTheOrbAsync(
        It.Is<AskTheOrbRequest>(r =>
          r.Platform == Platform.Twitch
          && r.PlatformUserId == notification.RedeemerUserId
          && r.Username == notification.RedeemerLogin
          && r.Content == notification.UserInput),
        It.IsAny<CancellationToken>()),
      Times.Once);
  }

  [Fact]
  public async Task HandleWithNonMatchingRewardPublishesRedeemAndDoesNotCallAskTheOrbAsync()
  {
    // Arrange
    var messageTheAiRewardId = Guid.NewGuid();
    var differentRewardId = Guid.NewGuid();
    var serviceClientMock = new Mock<ICaramelServiceClient>();
    var broadcasterMock = new Mock<ITwitchChatBroadcaster>();
    var chatClientMock = new Mock<ITwitchChatClient>();
    var loggerMock = new Mock<ILogger<ChannelPointRedeemHandler>>();
    var config = CreateTwitchConfig(messageTheAiRewardId.ToString());

    var handler = new ChannelPointRedeemHandler(serviceClientMock.Object, broadcasterMock.Object, chatClientMock.Object, config, loggerMock.Object);
    var notification = CreateRedeemNotification(
      rewardId: differentRewardId.ToString(),
      userInput: "hello orb");

    // Act
    await handler.Handle(notification, CancellationToken.None);

    // Assert
    broadcasterMock.Verify(
      x => x.PublishRedeemAsync(
        notification.RedemptionId,
        notification.BroadcasterUserId,
        notification.BroadcasterLogin,
        notification.RedeemerUserId,
        notification.RedeemerLogin,
        notification.RedeemerDisplayName,
        notification.RewardId,
        notification.RewardTitle,
        notification.RewardCost,
        notification.UserInput,
        notification.RedeemedAt,
        It.IsAny<CancellationToken>()),
      Times.Once);

    serviceClientMock.Verify(
      x => x.AskTheOrbAsync(It.IsAny<AskTheOrbRequest>(), It.IsAny<CancellationToken>()),
      Times.Never);
  }

  [Fact]
  public async Task HandleWithMatchingRewardButNoInputDoesNotCallAskTheOrbAsync()
  {
    // Arrange
    var messageTheAiRewardId = Guid.NewGuid();
    var serviceClientMock = new Mock<ICaramelServiceClient>();
    var broadcasterMock = new Mock<ITwitchChatBroadcaster>();
    var chatClientMock = new Mock<ITwitchChatClient>();
    var loggerMock = new Mock<ILogger<ChannelPointRedeemHandler>>();
    var config = CreateTwitchConfig(messageTheAiRewardId.ToString());

    var handler = new ChannelPointRedeemHandler(serviceClientMock.Object, broadcasterMock.Object, chatClientMock.Object, config, loggerMock.Object);
    var notification = CreateRedeemNotification(
      rewardId: messageTheAiRewardId.ToString(),
      userInput: "   ");

    // Act
    await handler.Handle(notification, CancellationToken.None);

    // Assert
    serviceClientMock.Verify(
      x => x.AskTheOrbAsync(It.IsAny<AskTheOrbRequest>(), It.IsAny<CancellationToken>()),
      Times.Never);
  }

  [Fact]
  public async Task HandleWithMatchingAiRewardSendsResponseToChatAsync()
  {
    // Arrange
    var messageTheAiRewardId = Guid.NewGuid();
    var serviceClientMock = new Mock<ICaramelServiceClient>();
    var broadcasterMock = new Mock<ITwitchChatBroadcaster>();
    var chatClientMock = new Mock<ITwitchChatClient>();
    var loggerMock = new Mock<ILogger<ChannelPointRedeemHandler>>();
    var config = CreateTwitchConfig(messageTheAiRewardId.ToString());

    _ = serviceClientMock
      .Setup(x => x.AskTheOrbAsync(It.IsAny<AskTheOrbRequest>(), It.IsAny<CancellationToken>()))
      .ReturnsAsync(Result.Ok("The orb has spoken."));

    _ = chatClientMock
      .Setup(x => x.SendChatMessageAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
      .ReturnsAsync(Result.Ok());

    var handler = new ChannelPointRedeemHandler(serviceClientMock.Object, broadcasterMock.Object, chatClientMock.Object, config, loggerMock.Object);
    var notification = CreateRedeemNotification(
      rewardId: messageTheAiRewardId.ToString(),
      userInput: "Should I touch grass today?");

    // Act
    await handler.Handle(notification, CancellationToken.None);

    // Assert
    chatClientMock.Verify(
      x => x.SendChatMessageAsync("@viewer The orb has spoken.", It.IsAny<CancellationToken>()),
      Times.Once);
  }

  [Fact]
  public async Task HandleWithMatchingAiRewardDoesNotSendToChatWhenAiFailsAsync()
  {
    // Arrange
    var messageTheAiRewardId = Guid.NewGuid();
    var serviceClientMock = new Mock<ICaramelServiceClient>();
    var broadcasterMock = new Mock<ITwitchChatBroadcaster>();
    var chatClientMock = new Mock<ITwitchChatClient>();
    var loggerMock = new Mock<ILogger<ChannelPointRedeemHandler>>();
    var config = CreateTwitchConfig(messageTheAiRewardId.ToString());

    _ = serviceClientMock
      .Setup(x => x.AskTheOrbAsync(It.IsAny<AskTheOrbRequest>(), It.IsAny<CancellationToken>()))
      .ReturnsAsync(Result.Fail<string>("AI service unavailable"));

    var handler = new ChannelPointRedeemHandler(serviceClientMock.Object, broadcasterMock.Object, chatClientMock.Object, config, loggerMock.Object);
    var notification = CreateRedeemNotification(
      rewardId: messageTheAiRewardId.ToString(),
      userInput: "Hello orb");

    // Act
    await handler.Handle(notification, CancellationToken.None);

    // Assert
    chatClientMock.Verify(
      x => x.SendChatMessageAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
      Times.Never);
  }

  [Fact]
  public async Task HandleWithMatchingAiRewardDoesNotSendToChatWhenResponseIsEmptyAsync()
  {
    // Arrange
    var messageTheAiRewardId = Guid.NewGuid();
    var serviceClientMock = new Mock<ICaramelServiceClient>();
    var broadcasterMock = new Mock<ITwitchChatBroadcaster>();
    var chatClientMock = new Mock<ITwitchChatClient>();
    var loggerMock = new Mock<ILogger<ChannelPointRedeemHandler>>();
    var config = CreateTwitchConfig(messageTheAiRewardId.ToString());

    _ = serviceClientMock
      .Setup(x => x.AskTheOrbAsync(It.IsAny<AskTheOrbRequest>(), It.IsAny<CancellationToken>()))
      .ReturnsAsync(Result.Ok("   "));

    var handler = new ChannelPointRedeemHandler(serviceClientMock.Object, broadcasterMock.Object, chatClientMock.Object, config, loggerMock.Object);
    var notification = CreateRedeemNotification(
      rewardId: messageTheAiRewardId.ToString(),
      userInput: "Hello orb");

    // Act
    await handler.Handle(notification, CancellationToken.None);

    // Assert
    chatClientMock.Verify(
      x => x.SendChatMessageAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
      Times.Never);
  }

  [Fact]
  public async Task HandleWithMatchingAiRewardTruncatesLongResponseAsync()
  {
    // Arrange
    var messageTheAiRewardId = Guid.NewGuid();
    var serviceClientMock = new Mock<ICaramelServiceClient>();
    var broadcasterMock = new Mock<ITwitchChatBroadcaster>();
    var chatClientMock = new Mock<ITwitchChatClient>();
    var loggerMock = new Mock<ILogger<ChannelPointRedeemHandler>>();
    var config = CreateTwitchConfig(messageTheAiRewardId.ToString());

    var longResponse = new string('x', 600);
    _ = serviceClientMock
      .Setup(x => x.AskTheOrbAsync(It.IsAny<AskTheOrbRequest>(), It.IsAny<CancellationToken>()))
      .ReturnsAsync(Result.Ok(longResponse));

    _ = chatClientMock
      .Setup(x => x.SendChatMessageAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
      .ReturnsAsync(Result.Ok());

    var handler = new ChannelPointRedeemHandler(serviceClientMock.Object, broadcasterMock.Object, chatClientMock.Object, config, loggerMock.Object);
    var notification = CreateRedeemNotification(
      rewardId: messageTheAiRewardId.ToString(),
      userInput: "Tell me everything");

    // Act
    await handler.Handle(notification, CancellationToken.None);

    // Assert — "@viewer " is 8 chars, so the message must be ≤ 500 total
    chatClientMock.Verify(
      x => x.SendChatMessageAsync(
        It.Is<string>(msg => msg.Length <= 500 && msg.StartsWith("@viewer ")),
        It.IsAny<CancellationToken>()),
      Times.Once);
  }

  private static TwitchConfig CreateTwitchConfig(string? messageTheAiRewardId)
  {
    return new TwitchConfig
    {
      AccessToken = "token",
      ClientId = "client-id",
      ClientSecret = "client-secret",
      EncryptionKey = "cGxhY2Vob2xkZXItZGV2LWtleS0zMmI=",
      OAuthCallbackUrl = "http://localhost/auth/twitch/callback",
      RefreshToken = "refresh",
      MessageTheAiRewardId = messageTheAiRewardId
    };
  }

  private static ChannelPointsCustomRewardRedeemed CreateRedeemNotification(string rewardId, string userInput)
  {
    return new ChannelPointsCustomRewardRedeemed(
      RedemptionId: "redeem-123",
      BroadcasterUserId: "broadcaster-1",
      BroadcasterLogin: "streamer",
      RedeemerUserId: "viewer-123",
      RedeemerLogin: "viewer",
      RedeemerDisplayName: "Viewer",
      RewardId: rewardId,
      RewardTitle: "Message The AI",
      RewardCost: 1_000,
      UserInput: userInput,
      RedeemedAt: DateTimeOffset.UtcNow);
  }
}
