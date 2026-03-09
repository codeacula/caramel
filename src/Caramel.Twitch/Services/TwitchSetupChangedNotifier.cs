namespace Caramel.Twitch.Services;

public interface ITwitchSetupChangedSubscriber
{
  Task SubscribeAsync(Func<CancellationToken, Task> onSetupChanged, CancellationToken cancellationToken = default);
}

public sealed class TwitchSetupChangedNotifier(
  IConnectionMultiplexer redis,
  ILogger<TwitchSetupChangedNotifier> logger) : ITwitchSetupChangedNotifier
{
  public const string RedisChannel = "caramel:twitch:setup-changed";

  private static readonly JsonSerializerOptions SerializerOptions = new()
  {
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
  };

  public async Task PublishAsync(TwitchSetup setup, CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(setup);

    cancellationToken.ThrowIfCancellationRequested();

    var message = new TwitchSetupChangedMessage
    {
      EventType = "twitch.setup.changed",
      Setup = TwitchSetupChangedSnapshot.FromDomain(setup),
      OccurredAt = DateTimeOffset.UtcNow,
    };

    var payload = JsonSerializer.Serialize(message, SerializerOptions);

    var subscriber = redis.GetSubscriber();
    _ = await subscriber.PublishAsync(
      StackExchange.Redis.RedisChannel.Literal(RedisChannel),
      payload);

    TwitchSetupChangedNotifierLogs.Published(logger, RedisChannel, setup.BotLogin);
  }
}

public sealed class TwitchSetupChangedSubscriber(
  IConnectionMultiplexer redis,
  ILogger<TwitchSetupChangedSubscriber> logger) : ITwitchSetupChangedSubscriber
{
  private int _subscribed;

  /// <inheritdoc />
  public async Task SubscribeAsync(Func<CancellationToken, Task> onSetupChanged, CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(onSetupChanged);

    cancellationToken.ThrowIfCancellationRequested();

    if (Interlocked.Exchange(ref _subscribed, 1) == 1)
    {
      TwitchSetupChangedSubscriberLogs.AlreadySubscribed(logger, TwitchSetupChangedNotifier.RedisChannel);
      return;
    }

    var subscriber = redis.GetSubscriber();

    await subscriber.SubscribeAsync(
      RedisChannel.Literal(TwitchSetupChangedNotifier.RedisChannel),
      async (_, payload) =>
      {
        try
        {
          if (!payload.HasValue || payload.IsNullOrEmpty)
          {
            TwitchSetupChangedSubscriberLogs.EmptyPayload(logger, TwitchSetupChangedNotifier.RedisChannel);
            return;
          }

          var message = JsonSerializer.Deserialize<TwitchSetupChangedMessage>(payload.ToString(), SerializerOptions);
          if (message is null)
          {
            TwitchSetupChangedSubscriberLogs.InvalidPayload(logger, TwitchSetupChangedNotifier.RedisChannel);
            return;
          }

          TwitchSetupChangedSubscriberLogs.Received(
            logger,
            TwitchSetupChangedNotifier.RedisChannel,
            message.EventType ?? "unknown");

          await onSetupChanged(CancellationToken.None);
        }
        catch (JsonException ex)
        {
          TwitchSetupChangedSubscriberLogs.PayloadDeserializationFailed(logger, ex.Message);
        }
        catch (OperationCanceledException)
        {
          throw;
        }
        catch (Exception ex)
        {
          TwitchSetupChangedSubscriberLogs.CallbackFailed(logger, ex.Message);
        }
      });

    TwitchSetupChangedSubscriberLogs.Subscribed(logger, TwitchSetupChangedNotifier.RedisChannel);
  }

  private static readonly JsonSerializerOptions SerializerOptions = new()
  {
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
  };
}

internal sealed record TwitchSetupChangedMessage
{
  public string EventType { get; init; } = string.Empty;
  public DateTimeOffset OccurredAt { get; init; }
  public TwitchSetupChangedSnapshot? Setup { get; init; }
}

internal sealed record TwitchSetupChangedSnapshot
{
  public string BotUserId { get; init; } = string.Empty;
  public string BotLogin { get; init; } = string.Empty;
  public IReadOnlyList<TwitchSetupChangedChannelSnapshot> Channels { get; init; } = [];
  public DateTimeOffset ConfiguredOn { get; init; }
  public DateTimeOffset UpdatedOn { get; init; }
  public TwitchSetupChangedTokensSnapshot? BotTokens { get; init; }
  public TwitchSetupChangedTokensSnapshot? BroadcasterTokens { get; init; }

  public static TwitchSetupChangedSnapshot FromDomain(TwitchSetup setup)
  {
    return new TwitchSetupChangedSnapshot
    {
      BotUserId = setup.BotUserId,
      BotLogin = setup.BotLogin,
      Channels = [.. setup.Channels.Select(TwitchSetupChangedChannelSnapshot.FromDomain)],
      ConfiguredOn = setup.ConfiguredOn,
      UpdatedOn = setup.UpdatedOn,
      BotTokens = TwitchSetupChangedTokensSnapshot.FromDomain(setup.BotTokens),
      BroadcasterTokens = TwitchSetupChangedTokensSnapshot.FromDomain(setup.BroadcasterTokens),
    };
  }
}

internal sealed record TwitchSetupChangedChannelSnapshot
{
  public string UserId { get; init; } = string.Empty;
  public string Login { get; init; } = string.Empty;

  public static TwitchSetupChangedChannelSnapshot FromDomain(TwitchChannel channel)
  {
    return new TwitchSetupChangedChannelSnapshot
    {
      UserId = channel.UserId,
      Login = channel.Login,
    };
  }
}

internal sealed record TwitchSetupChangedTokensSnapshot
{
  public string UserId { get; init; } = string.Empty;
  public string Login { get; init; } = string.Empty;
  public string AccessToken { get; init; } = string.Empty;
  public string? RefreshToken { get; init; }
  public DateTime ExpiresAt { get; init; }
  public DateTimeOffset LastRefreshedOn { get; init; }

  public static TwitchSetupChangedTokensSnapshot? FromDomain(TwitchAccountTokens? tokens)
  {
    return tokens is null
      ? null
      : new TwitchSetupChangedTokensSnapshot
    {
      UserId = tokens.UserId,
      Login = tokens.Login,
      AccessToken = tokens.AccessToken,
      RefreshToken = tokens.RefreshToken,
      ExpiresAt = tokens.ExpiresAt,
      LastRefreshedOn = tokens.LastRefreshedOn,
    };
  }
}

internal static partial class TwitchSetupChangedNotifierLogs
{
  [LoggerMessage(
    EventId = 1,
    Level = LogLevel.Information,
    Message = "Published Twitch setup change notification to Redis channel '{Channel}' for bot '{BotLogin}'")]
  public static partial void Published(ILogger logger, string channel, string botLogin);
}

internal static partial class TwitchSetupChangedSubscriberLogs
{
  [LoggerMessage(
    EventId = 1,
    Level = LogLevel.Information,
    Message = "Subscribed to Twitch setup change Redis channel '{Channel}'")]
  public static partial void Subscribed(ILogger logger, string channel);

  [LoggerMessage(
    EventId = 2,
    Level = LogLevel.Debug,
    Message = "Twitch setup change subscriber already active for Redis channel '{Channel}'")]
  public static partial void AlreadySubscribed(ILogger logger, string channel);

  [LoggerMessage(
    EventId = 3,
    Level = LogLevel.Warning,
    Message = "Received empty Twitch setup change payload from Redis channel '{Channel}'")]
  public static partial void EmptyPayload(ILogger logger, string channel);

  [LoggerMessage(
    EventId = 4,
    Level = LogLevel.Warning,
    Message = "Received invalid Twitch setup change payload from Redis channel '{Channel}'")]
  public static partial void InvalidPayload(ILogger logger, string channel);

  [LoggerMessage(
    EventId = 5,
    Level = LogLevel.Information,
    Message = "Received Twitch setup change event '{EventType}' from Redis channel '{Channel}'")]
  public static partial void Received(ILogger logger, string channel, string eventType);

  [LoggerMessage(
    EventId = 6,
    Level = LogLevel.Error,
    Message = "Failed to deserialize Twitch setup change payload: {Error}")]
  public static partial void PayloadDeserializationFailed(ILogger logger, string error);

  [LoggerMessage(
    EventId = 7,
    Level = LogLevel.Error,
    Message = "Twitch setup change callback failed: {Error}")]
  public static partial void CallbackFailed(ILogger logger, string error);
}
