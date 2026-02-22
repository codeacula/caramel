using System.Text.Json;

using Caramel.Core.Twitch;

using Microsoft.Extensions.Logging;

using StackExchange.Redis;

namespace Caramel.Twitch.Services;

/// <summary>
/// Contract for publishing incoming Twitch chat messages to a Redis pub/sub channel.
/// </summary>
public interface ITwitchChatBroadcaster
{
  /// <summary>
  /// Publishes a chat message to the Redis pub/sub channel.
  /// </summary>
  Task PublishAsync(
    string messageId,
    string broadcasterUserId,
    string broadcasterLogin,
    string chatterUserId,
    string chatterLogin,
    string chatterDisplayName,
    string messageText,
    string color,
    CancellationToken cancellationToken = default);
}

/// <summary>
/// Publishes incoming Twitch chat messages to a Redis pub/sub channel so that
/// Caramel.API can broadcast them to connected WebSocket clients.
/// </summary>
public sealed class TwitchChatBroadcaster(
  IConnectionMultiplexer redis,
  ILogger<TwitchChatBroadcaster> logger) : ITwitchChatBroadcaster
{
  private static readonly JsonSerializerOptions SerializerOptions = new()
  {
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
  };

  /// <inheritdoc/>
  public async Task PublishAsync(
    string messageId,
    string broadcasterUserId,
    string broadcasterLogin,
    string chatterUserId,
    string chatterLogin,
    string chatterDisplayName,
    string messageText,
    string color,
    CancellationToken cancellationToken = default)
  {
    try
    {
      var message = new TwitchChatMessage
      {
        MessageId = messageId,
        BroadcasterUserId = broadcasterUserId,
        BroadcasterLogin = broadcasterLogin,
        ChatterUserId = chatterUserId,
        ChatterLogin = chatterLogin,
        ChatterDisplayName = chatterDisplayName,
        MessageText = messageText,
        Color = color,
        Timestamp = DateTimeOffset.UtcNow,
      };

      var payload = JsonSerializer.Serialize(message, SerializerOptions);

      var subscriber = redis.GetSubscriber();
      _ = await subscriber.PublishAsync(
        RedisChannel.Literal(TwitchChatMessage.RedisChannel),
        payload);

      TwitchChatBroadcasterLogs.MessagePublished(logger, chatterLogin, broadcasterLogin);
    }
    catch (Exception ex)
    {
      TwitchChatBroadcasterLogs.PublishFailed(logger, chatterLogin, ex.Message);
    }
  }
}

/// <summary>
/// Structured log messages for <see cref="TwitchChatBroadcaster"/>.
/// </summary>
internal static partial class TwitchChatBroadcasterLogs
{
  [LoggerMessage(Level = LogLevel.Debug, Message = "Chat message from {Username} in {Channel} published to Redis")]
  public static partial void MessagePublished(ILogger logger, string username, string channel);

  [LoggerMessage(Level = LogLevel.Warning, Message = "Failed to publish chat message from {Username} to Redis: {Error}")]
  public static partial void PublishFailed(ILogger logger, string username, string error);
}
