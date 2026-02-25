using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;

using Caramel.Core.Twitch;

using StackExchange.Redis;

namespace Caramel.API.Services;

internal sealed class TwitchChatRelayService(
  IConnectionMultiplexer redis,
  ConcurrentDictionary<string, WebSocket> registry,
  ILogger<TwitchChatRelayService> logger) : BackgroundService
{
  protected override async Task ExecuteAsync(CancellationToken stoppingToken)
  {
    var subscriber = redis.GetSubscriber();

    await subscriber.SubscribeAsync(
      RedisChannel.Literal(TwitchChatMessage.RedisChannel),
      (_, value) => OnRedisMessage(value));

    TwitchChatRelayLogs.Subscribed(logger, TwitchChatMessage.RedisChannel);

    // Keep the service alive until the host shuts down
    await Task.Delay(Timeout.Infinite, stoppingToken);
  }

  private void OnRedisMessage(RedisValue value)
  {
    if (!value.HasValue)
    {
      return;
    }

    var payload = Encoding.UTF8.GetBytes(value.ToString());

    // Fan-out: send to every connected WebSocket client
    foreach (var (id, socket) in registry)
    {
      if (socket.State != WebSocketState.Open)
      {
        _ = registry.TryRemove(id, out _);
        continue;
      }

      _ = BroadcastAsync(id, socket, payload);
    }
  }

  private async Task BroadcastAsync(string id, WebSocket socket, byte[] payload)
  {
    try
    {
      await socket.SendAsync(
        new ArraySegment<byte>(payload),
        WebSocketMessageType.Text,
        endOfMessage: true,
        CancellationToken.None);
    }
    catch (Exception ex)
    {
      TwitchChatRelayLogs.BroadcastFailed(logger, id, ex.Message);
      _ = registry.TryRemove(id, out _);
    }
  }

  public override async Task StopAsync(CancellationToken cancellationToken)
  {
    TwitchChatRelayLogs.Stopping(logger);
    await base.StopAsync(cancellationToken);
  }
}
