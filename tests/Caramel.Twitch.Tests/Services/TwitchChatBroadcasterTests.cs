using Caramel.Core.Twitch;
using Caramel.Twitch.Services;

using Microsoft.Extensions.Logging;

using StackExchange.Redis;

namespace Caramel.Twitch.Tests.Services;

public sealed class TwitchChatBroadcasterTests
{
  private readonly Mock<IConnectionMultiplexer> _redis = new();
  private readonly Mock<ISubscriber> _subscriber = new();
  private readonly Mock<ILogger<TwitchChatBroadcaster>> _logger = new();

  public TwitchChatBroadcasterTests()
  {
    _redis.Setup(r => r.GetSubscriber(It.IsAny<object>())).Returns(_subscriber.Object);
    _subscriber
      .Setup(s => s.PublishAsync(It.IsAny<RedisChannel>(), It.IsAny<RedisValue>(), It.IsAny<CommandFlags>()))
      .ReturnsAsync(1L);
  }

  private TwitchChatBroadcaster CreateBroadcaster() =>
    new(_redis.Object, _logger.Object);

  // -----------------------------------------------------------------------
  // PublishAsync
  // -----------------------------------------------------------------------

  [Fact]
  public async Task PublishAsync_PublishesToCorrectRedisChannel()
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
  public async Task PublishAsync_SerializesEnvelopeWithChatMessageType()
  {
    var broadcaster = CreateBroadcaster();
    string? capturedPayload = null;

    _subscriber
      .Setup(s => s.PublishAsync(It.IsAny<RedisChannel>(), It.IsAny<RedisValue>(), It.IsAny<CommandFlags>()))
      .Callback<RedisChannel, RedisValue, CommandFlags>((_, v, _) => capturedPayload = v.ToString())
      .ReturnsAsync(1L);

    await broadcaster.PublishAsync("msg1", "bid", "streamer", "uid", "viewer", "Viewer", "hello", "#FF0000");

    capturedPayload.Should().NotBeNull();
    capturedPayload!.Should().Contain("\"type\"");
    capturedPayload.Should().Contain("chat_message");
  }

  [Fact]
  public async Task PublishAsync_DoesNotThrow_WhenRedisThrows()
  {
    _subscriber
      .Setup(s => s.PublishAsync(It.IsAny<RedisChannel>(), It.IsAny<RedisValue>(), It.IsAny<CommandFlags>()))
      .ThrowsAsync(new RedisConnectionException(ConnectionFailureType.UnableToConnect, "Redis down"));

    var broadcaster = CreateBroadcaster();

    var act = async () => await broadcaster.PublishAsync("msg1", "bid", "streamer", "uid", "viewer", "Viewer", "hello", "#FF0000");

    await act.Should().NotThrowAsync();
  }

  // -----------------------------------------------------------------------
  // PublishSystemMessageAsync
  // -----------------------------------------------------------------------

  [Fact]
  public async Task PublishSystemMessageAsync_PublishesToCorrectRedisChannel()
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
  public async Task PublishSystemMessageAsync_SerializesEnvelopeWithGivenType()
  {
    var broadcaster = CreateBroadcaster();
    string? capturedPayload = null;

    _subscriber
      .Setup(s => s.PublishAsync(It.IsAny<RedisChannel>(), It.IsAny<RedisValue>(), It.IsAny<CommandFlags>()))
      .Callback<RedisChannel, RedisValue, CommandFlags>((_, v, _) => capturedPayload = v.ToString())
      .ReturnsAsync(1L);

    await broadcaster.PublishSystemMessageAsync("setup_status", new { configured = true });

    capturedPayload.Should().NotBeNull();
    capturedPayload!.Should().Contain("\"type\"");
    capturedPayload.Should().Contain("setup_status");
  }

  [Fact]
  public async Task PublishSystemMessageAsync_DoesNotThrow_WhenRedisThrows()
  {
    _subscriber
      .Setup(s => s.PublishAsync(It.IsAny<RedisChannel>(), It.IsAny<RedisValue>(), It.IsAny<CommandFlags>()))
      .ThrowsAsync(new RedisConnectionException(ConnectionFailureType.UnableToConnect, "Redis down"));

    var broadcaster = CreateBroadcaster();

    var act = async () => await broadcaster.PublishSystemMessageAsync("setup_status", new { configured = true });

    await act.Should().NotThrowAsync();
  }
}
