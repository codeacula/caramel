using System.Text;

using Caramel.Core;
using Caramel.Twitch.Auth;

using FluentResults;

namespace Caramel.Twitch.Services;

public interface ITwitchChatClient
{
  Task<Result> SendChatMessageAsync(string message, CancellationToken cancellationToken = default);
}

public sealed class TwitchChatClient(
  IHttpClientFactory httpClientFactory,
  TwitchConfig twitchConfig,
  ITwitchTokenManager tokenManager,
  ITwitchSetupState setupState,
  ILogger<TwitchChatClient> logger) : ITwitchChatClient
{
  public async Task<Result> SendChatMessageAsync(string message, CancellationToken cancellationToken = default)
  {
    return await ResultExtensions.ExecuteAsync(
      () => SendChatMessageInternalAsync(message, cancellationToken),
      "Network error sending chat message",
      "Unexpected error sending chat message");
  }

  private async Task SendChatMessageInternalAsync(string message, CancellationToken cancellationToken)
  {
    var setup = setupState.Current;
    if (setup is null)
    {
      TwitchChatClientLogs.SetupNotConfigured(logger);
      throw new InvalidOperationException("Twitch setup has not been completed.");
    }

    if (setup.Channels.Count == 0)
    {
      TwitchChatClientLogs.NoChannelsConfigured(logger);
      throw new InvalidOperationException("No channels are configured.");
    }

    var broadcasterId = setup.Channels[0].UserId;
    var senderId = setup.BotUserId;

    var accessToken = await tokenManager.GetValidAccessTokenAsync(cancellationToken);

    using var httpClient = httpClientFactory.CreateClient("TwitchHelix");
    httpClient.DefaultRequestHeaders.Add("Client-Id", twitchConfig.ClientId);
    httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {accessToken}");

    var body = new
    {
      broadcaster_id = broadcasterId,
      sender_id = senderId,
      message,
    };

    var json = JsonSerializer.Serialize(body);
    using var content = new StringContent(json, Encoding.UTF8, "application/json");
    var response = await httpClient.PostAsync("https://api.twitch.tv/helix/chat/messages", content, cancellationToken);

    if (response.IsSuccessStatusCode)
    {
      TwitchChatClientLogs.ChatMessageSent(logger, broadcasterId);
      return;
    }

    var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
    TwitchChatClientLogs.ChatMessageSendFailed(logger, (int)response.StatusCode, errorBody);
    throw new InvalidOperationException($"Twitch API rejected the message with status {(int)response.StatusCode}.");
  }
}

/// <summary>
/// Structured logging for <see cref="TwitchChatClient"/>.
/// </summary>
internal static partial class TwitchChatClientLogs
{
  [LoggerMessage(Level = LogLevel.Warning, Message = "Cannot send chat message: Twitch setup has not been completed")]
  public static partial void SetupNotConfigured(ILogger logger);

  [LoggerMessage(Level = LogLevel.Warning, Message = "Cannot send chat message: no channels are configured")]
  public static partial void NoChannelsConfigured(ILogger logger);

  [LoggerMessage(Level = LogLevel.Information, Message = "Chat message sent to channel {ChannelId}")]
  public static partial void ChatMessageSent(ILogger logger, string channelId);

  [LoggerMessage(Level = LogLevel.Warning, Message = "Twitch API rejected chat message with status {StatusCode}: {Error}")]
  public static partial void ChatMessageSendFailed(ILogger logger, int statusCode, string error);

  [LoggerMessage(Level = LogLevel.Error, Message = "Error sending chat message: {Error}")]
  public static partial void ChatMessageSendError(ILogger logger, string error);
}
