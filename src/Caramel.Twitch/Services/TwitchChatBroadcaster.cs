using System.Text.Json.Serialization;

namespace Caramel.Twitch.Services;

/// <summary>
/// Contract for publishing messages to the Twitch WebSocket broadcast channel via Redis pub/sub.
/// </summary>
public interface ITwitchChatBroadcaster
{
  /// <summary>
  /// Publishes a chat message to the Redis pub/sub channel wrapped in a typed envelope.
  /// </summary>
  /// <param name="messageId"></param>
  /// <param name="broadcasterUserId"></param>
  /// <param name="broadcasterLogin"></param>
  /// <param name="chatterUserId"></param>
  /// <param name="chatterLogin"></param>
  /// <param name="chatterDisplayName"></param>
  /// <param name="messageText"></param>
  /// <param name="color"></param>
  /// <param name="cancellationToken"></param>
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

  /// <summary>
  /// Publishes a channel point custom reward redemption to the Redis pub/sub channel wrapped in a typed envelope.
  /// </summary>
  Task PublishRedeemAsync(
    string redemptionId,
    string broadcasterUserId,
    string broadcasterLogin,
    string redeemerUserId,
    string redeemerLogin,
    string redeemerDisplayName,
    string rewardId,
    string rewardTitle,
    int rewardCost,
    string userInput,
    DateTimeOffset redeemedAt,
    CancellationToken cancellationToken = default);

  /// <summary>
  /// Publishes a system message (non-chat) to the Redis pub/sub channel.
  /// The payload is serialized as-is alongside a <c>type</c> discriminator field.
  /// </summary>
  /// <param name="type"></param>
  /// <param name="payload"></param>
  /// <param name="cancellationToken"></param>
  Task PublishSystemMessageAsync(string type, object payload, CancellationToken cancellationToken = default);
}

/// <summary>
/// Envelope wrapper for all messages published to the Redis pub/sub channel.
/// The Vue client uses the <c>Type</c> field to route messages appropriately.
/// </summary>
internal sealed record TwitchWebSocketEnvelope
{
  [JsonPropertyName("type")]
  public required string Type { get; init; }

  [JsonPropertyName("data")]
  public required object Data { get; init; }
}

/// <summary>
/// Publishes incoming Twitch chat messages and system notifications to a Redis pub/sub channel
/// so that Caramel.API can broadcast them to connected WebSocket clients.
/// </summary>
/// <param name="redis"></param>
/// <param name="logger"></param>
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

      var envelope = new TwitchWebSocketEnvelope { Type = "chat_message", Data = message };
      var payload = JsonSerializer.Serialize(envelope, SerializerOptions);

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

  /// <inheritdoc/>
  public async Task PublishRedeemAsync(
    string redemptionId,
    string broadcasterUserId,
    string broadcasterLogin,
    string redeemerUserId,
    string redeemerLogin,
    string redeemerDisplayName,
    string rewardId,
    string rewardTitle,
    int rewardCost,
    string userInput,
    DateTimeOffset redeemedAt,
    CancellationToken cancellationToken = default)
  {
    try
    {
      var redeem = new TwitchChannelPointRedeem
      {
        RedemptionId = redemptionId,
        BroadcasterUserId = broadcasterUserId,
        BroadcasterLogin = broadcasterLogin,
        RedeemerUserId = redeemerUserId,
        RedeemerLogin = redeemerLogin,
        RedeemerDisplayName = redeemerDisplayName,
        RewardId = rewardId,
        RewardTitle = rewardTitle,
        RewardCost = rewardCost,
        UserInput = userInput,
        RedeemedAt = redeemedAt,
      };

      var envelope = new TwitchWebSocketEnvelope { Type = "channel_point_redeem", Data = redeem };
      var payload = JsonSerializer.Serialize(envelope, SerializerOptions);

      var subscriber = redis.GetSubscriber();
      _ = await subscriber.PublishAsync(
        RedisChannel.Literal(TwitchChatMessage.RedisChannel),
        payload);

      TwitchChatBroadcasterLogs.RedeemPublished(logger, redeemerLogin, rewardTitle, broadcasterLogin);
    }
    catch (Exception ex)
    {
      TwitchChatBroadcasterLogs.RedeemPublishFailed(logger, redeemerLogin, ex.Message);
    }
  }

  /// <inheritdoc/>
  public async Task PublishSystemMessageAsync(string type, object payload, CancellationToken cancellationToken = default)
  {
    try
    {
      var envelope = new TwitchWebSocketEnvelope { Type = type, Data = payload };
      var json = JsonSerializer.Serialize(envelope, SerializerOptions);

      var subscriber = redis.GetSubscriber();
      _ = await subscriber.PublishAsync(
        RedisChannel.Literal(TwitchChatMessage.RedisChannel),
        json);

      TwitchChatBroadcasterLogs.SystemMessagePublished(logger, type);
    }
    catch (Exception ex)
    {
      TwitchChatBroadcasterLogs.SystemPublishFailed(logger, type, ex.Message);
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

  [LoggerMessage(Level = LogLevel.Debug, Message = "Channel point redeem by {Username} for '{RewardTitle}' in {Channel} published to Redis")]
  public static partial void RedeemPublished(ILogger logger, string username, string rewardTitle, string channel);

  [LoggerMessage(Level = LogLevel.Warning, Message = "Failed to publish channel point redeem from {Username} to Redis: {Error}")]
  public static partial void RedeemPublishFailed(ILogger logger, string username, string error);

  [LoggerMessage(Level = LogLevel.Debug, Message = "System message '{Type}' published to Redis")]
  public static partial void SystemMessagePublished(ILogger logger, string type);

  [LoggerMessage(Level = LogLevel.Warning, Message = "Failed to publish system message '{Type}' to Redis: {Error}")]
  public static partial void SystemPublishFailed(ILogger logger, string type, string error);
}
