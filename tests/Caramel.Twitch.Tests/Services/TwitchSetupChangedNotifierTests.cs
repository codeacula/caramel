using StackExchange.Redis;

namespace Caramel.Twitch.Tests.Services;

public sealed class TwitchSetupChangedNotifierTests
{
  private readonly Mock<IConnectionMultiplexer> _redis = new();
  private readonly Mock<ISubscriber> _subscriber = new();
  private readonly Mock<ILogger<TwitchSetupChangedNotifier>> _logger = new();

  public TwitchSetupChangedNotifierTests()
  {
    _ = _redis
      .Setup(r => r.GetSubscriber(It.IsAny<object?>()))
      .Returns(_subscriber.Object);
  }

  private TwitchSetupChangedNotifier CreateSut()
  {
    return new TwitchSetupChangedNotifier(_redis.Object, _logger.Object);
  }

  private static TwitchSetup CreateSetup()
  {
    return new()
    {
      BotUserId = "111",
      BotLogin = "caramel_bot",
      Channels =
      [
        new TwitchChannel
        {
          UserId = "999",
          Login = "streamer",
        },
        new TwitchChannel
        {
          UserId = "888",
          Login = "other_streamer",
        },
      ],
      ConfiguredOn = new DateTimeOffset(2025, 1, 2, 3, 4, 5, TimeSpan.Zero),
      UpdatedOn = new DateTimeOffset(2025, 1, 2, 4, 5, 6, TimeSpan.Zero),
      BotTokens = new TwitchAccountTokens
      {
        UserId = "111",
        Login = "caramel_bot",
        AccessToken = "bot-access-token",
        RefreshToken = "bot-refresh-token",
        ExpiresAt = new DateTime(2025, 1, 3, 0, 0, 0, DateTimeKind.Utc),
        LastRefreshedOn = new DateTimeOffset(2025, 1, 2, 4, 0, 0, TimeSpan.Zero),
      },
      BroadcasterTokens = new TwitchAccountTokens
      {
        UserId = "999",
        Login = "streamer",
        AccessToken = "broadcaster-access-token",
        RefreshToken = "broadcaster-refresh-token",
        ExpiresAt = new DateTime(2025, 1, 3, 1, 0, 0, DateTimeKind.Utc),
        LastRefreshedOn = new DateTimeOffset(2025, 1, 2, 4, 30, 0, TimeSpan.Zero),
      },
    };
  }

  [Fact]
  public async Task PublishAsyncPublishesJsonPayloadToExpectedRedisChannelAsync()
  {
    // Arrange
    RedisChannel? capturedChannel = null;
    RedisValue capturedPayload = default;

    _ = _subscriber
      .Setup(s => s.PublishAsync(
        It.IsAny<RedisChannel>(),
        It.IsAny<RedisValue>(),
        It.IsAny<CommandFlags>()))
      .Callback<RedisChannel, RedisValue, CommandFlags>((channel, payload, _) =>
      {
        capturedChannel = channel;
        capturedPayload = payload;
      })
      .ReturnsAsync(1L);

    var sut = CreateSut();
    var setup = CreateSetup();

    // Act
    await sut.PublishAsync(setup, CancellationToken.None);

    // Assert
    _ = capturedChannel.Should().NotBeNull();
    _ = capturedChannel!.Value.ToString().Should().Be(TwitchSetupChangedNotifier.RedisChannel);

    _ = capturedPayload.HasValue.Should().BeTrue();

    using var json = JsonDocument.Parse(capturedPayload.ToString());
    var root = json.RootElement;

    _ = root.GetProperty("eventType").GetString().Should().Be("twitch.setup.changed");

    var setupElement = root.GetProperty("setup");
    _ = setupElement.GetProperty("botUserId").GetString().Should().Be("111");
    _ = setupElement.GetProperty("botLogin").GetString().Should().Be("caramel_bot");

    var channels = setupElement.GetProperty("channels");
    _ = channels.GetArrayLength().Should().Be(2);
    _ = channels[0].GetProperty("userId").GetString().Should().Be("999");
    _ = channels[0].GetProperty("login").GetString().Should().Be("streamer");
    _ = channels[1].GetProperty("userId").GetString().Should().Be("888");
    _ = channels[1].GetProperty("login").GetString().Should().Be("other_streamer");
  }

  [Fact]
  public async Task PublishAsyncUsesRedisSubscriberFromConnectionMultiplexerAsync()
  {
    // Arrange
    _ = _subscriber
      .Setup(s => s.PublishAsync(
        It.IsAny<RedisChannel>(),
        It.IsAny<RedisValue>(),
        It.IsAny<CommandFlags>()))
      .ReturnsAsync(1L);

    var sut = CreateSut();

    // Act
    await sut.PublishAsync(CreateSetup(), CancellationToken.None);

    // Assert
    _redis.Verify(r => r.GetSubscriber(It.IsAny<object?>()), Times.Once);
  }

  [Fact]
  public async Task PublishAsyncPublishesOncePerInvocationAsync()
  {
    // Arrange
    _ = _subscriber
      .Setup(s => s.PublishAsync(
        It.IsAny<RedisChannel>(),
        It.IsAny<RedisValue>(),
        It.IsAny<CommandFlags>()))
      .ReturnsAsync(1L);

    var sut = CreateSut();

    // Act
    await sut.PublishAsync(CreateSetup(), CancellationToken.None);

    // Assert
    _subscriber.Verify(
      s => s.PublishAsync(
        It.IsAny<RedisChannel>(),
        It.IsAny<RedisValue>(),
        It.IsAny<CommandFlags>()),
      Times.Once);
  }

  [Fact]
  public async Task PublishAsyncWhenBroadcasterTokensAreMissingStillPublishesPayloadAsync()
  {
    // Arrange
    RedisValue capturedPayload = default;

    _ = _subscriber
      .Setup(s => s.PublishAsync(
        It.IsAny<RedisChannel>(),
        It.IsAny<RedisValue>(),
        It.IsAny<CommandFlags>()))
      .Callback<RedisChannel, RedisValue, CommandFlags>((_, payload, _) => capturedPayload = payload)
      .ReturnsAsync(1L);

    var setup = CreateSetup() with
    {
      BroadcasterTokens = null,
    };

    var sut = CreateSut();

    // Act
    await sut.PublishAsync(setup, CancellationToken.None);

    // Assert
    using var json = JsonDocument.Parse(capturedPayload.ToString());
    var root = json.RootElement;
    var setupElement = root.GetProperty("setup");

    _ = setupElement.TryGetProperty("broadcasterTokens", out var broadcasterTokens).Should().BeTrue();
    _ = broadcasterTokens.ValueKind.Should().Be(JsonValueKind.Null);
  }

  [Fact]
  public async Task PublishAsyncWhenSubscriberThrowsPropagatesExceptionAsync()
  {
    // Arrange
    _ = _subscriber
      .Setup(s => s.PublishAsync(
        It.IsAny<RedisChannel>(),
        It.IsAny<RedisValue>(),
        It.IsAny<CommandFlags>()))
      .ThrowsAsync(new RedisException("publish failed"));

    var sut = CreateSut();

    // Act
    var act = async () => await sut.PublishAsync(CreateSetup(), CancellationToken.None);

    // Assert
    _ = await act.Should().ThrowAsync<RedisException>()
      .WithMessage("*publish failed*");
  }
}
