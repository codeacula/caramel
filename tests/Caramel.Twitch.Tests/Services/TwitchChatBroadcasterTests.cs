using Caramel.Core.Twitch;
using Caramel.Twitch.Services;

using StackExchange.Redis;

namespace Caramel.Twitch.Tests.Services;

public sealed class TwitchChatBroadcasterTests
{
  private readonly Mock<IConnectionMultiplexer> _redis = new();
  private readonly Mock<ISubscriber> _subscriber = new();
  private readonly Mock<ILogger<TwitchChatBroadcaster>> _logger = new();

  public TwitchChatBroadcasterTests()
  {
    _ = _redis.Setup(r => r.GetSubscriber(It.IsAny<object>())).Returns(_subscriber.Object);
    _ = _subscriber
      .Setup(s => s.PublishAsync(It.IsAny<RedisChannel>(), It.IsAny<RedisValue>(), It.IsAny<CommandFlags>()))
      .ReturnsAsync(1L);
  }

  private TwitchChatBroadcaster CreateBroadcaster()
  {
    return new(_redis.Object, _logger.Object);
  }

  // -----------------------------------------------------------------------
  // PublishAsync
  // -----------------------------------------------------------------------

  [Fact]
  public async Task PublishAsyncPublishesToCorrectRedisChannelAsync()
  {
    var broadcaster = CreateBroadcaster();

    await broadcaster.PublishAsync("msg1", "bid", "streamer", "uid", "viewer", "Viewer", "hello", "#FF0000");

    _subscriber.Verify(
      s => s.PublishAsync(
        It.Is<RedisChannel>(c => c == RedisChannel.Literal(TwitchChatMessage.RedisChannel)),
        It.IsAny<RedisValue>(),
        It.IsAny<CommandFlags>()),
      Times.Once);
  }

  [Fact]
  public async Task PublishAsyncSerializesEnvelopeWithChatMessageTypeAsync()
  {
    var broadcaster = CreateBroadcaster();
    string? capturedPayload = null;

    _ = _subscriber
      .Setup(s => s.PublishAsync(It.IsAny<RedisChannel>(), It.IsAny<RedisValue>(), It.IsAny<CommandFlags>()))
      .Callback<RedisChannel, RedisValue, CommandFlags>((_, v, _) => capturedPayload = v.ToString())
      .ReturnsAsync(1L);

    await broadcaster.PublishAsync("msg1", "bid", "streamer", "uid", "viewer", "Viewer", "hello", "#FF0000");

    _ = capturedPayload.Should().NotBeNull();
    _ = capturedPayload!.Should().Contain("\"type\"");
    _ = capturedPayload.Should().Contain("chat_message");
  }

  [Fact]
  public async Task PublishAsyncDoesNotThrowWhenRedisThrowsAsync()
  {
    _ = _subscriber
      .Setup(s => s.PublishAsync(It.IsAny<RedisChannel>(), It.IsAny<RedisValue>(), It.IsAny<CommandFlags>()))
      .ThrowsAsync(new RedisConnectionException(ConnectionFailureType.UnableToConnect, "Redis down"));

    var broadcaster = CreateBroadcaster();

    var act = async () => await broadcaster.PublishAsync("msg1", "bid", "streamer", "uid", "viewer", "Viewer", "hello", "#FF0000");

    _ = await act.Should().NotThrowAsync();
  }

  // -----------------------------------------------------------------------
  // PublishSystemMessageAsync
  // -----------------------------------------------------------------------

  [Fact]
  public async Task PublishSystemMessageAsyncPublishesToCorrectRedisChannelAsync()
  {
    var broadcaster = CreateBroadcaster();

    await broadcaster.PublishSystemMessageAsync("setup_status", new { configured = true });

    _subscriber.Verify(
      s => s.PublishAsync(
        It.Is<RedisChannel>(c => c == RedisChannel.Literal(TwitchChatMessage.RedisChannel)),
        It.IsAny<RedisValue>(),
        It.IsAny<CommandFlags>()),
      Times.Once);
  }

  [Fact]
  public async Task PublishSystemMessageAsyncSerializesEnvelopeWithGivenTypeAsync()
  {
    var broadcaster = CreateBroadcaster();
    string? capturedPayload = null;

    _ = _subscriber
      .Setup(s => s.PublishAsync(It.IsAny<RedisChannel>(), It.IsAny<RedisValue>(), It.IsAny<CommandFlags>()))
      .Callback<RedisChannel, RedisValue, CommandFlags>((_, v, _) => capturedPayload = v.ToString())
      .ReturnsAsync(1L);

    await broadcaster.PublishSystemMessageAsync("setup_status", new { configured = true });

    _ = capturedPayload.Should().NotBeNull();
    _ = capturedPayload!.Should().Contain("\"type\"");
    _ = capturedPayload.Should().Contain("setup_status");
  }

  [Fact]
  public async Task PublishSystemMessageAsyncDoesNotThrowWhenRedisThrowsAsync()
  {
    _ = _subscriber
      .Setup(s => s.PublishAsync(It.IsAny<RedisChannel>(), It.IsAny<RedisValue>(), It.IsAny<CommandFlags>()))
      .ThrowsAsync(new RedisConnectionException(ConnectionFailureType.UnableToConnect, "Redis down"));

    var broadcaster = CreateBroadcaster();

    var act = async () => await broadcaster.PublishSystemMessageAsync("setup_status", new { configured = true });

    _ = await act.Should().NotThrowAsync();
  }
}
