using System.Text;

using Caramel.Twitch.Auth;

using Microsoft.AspNetCore.Mvc;

namespace Caramel.Twitch.Controllers;

[ApiController]
[Route("chat")]
public sealed class ChatController(
  TwitchTokenManager tokenManager,
  TwitchConfig twitchConfig,
  ILogger<ChatController> logger) : ControllerBase
{
  private const int MaxMessageLength = 500;

  /// <summary>
  /// Sends a message to the first configured Twitch channel via the Helix chat API.
  /// </summary>
  [HttpPost("send")]
  public async Task<IActionResult> SendAsync([FromBody] SendChatMessageRequest request, CancellationToken cancellationToken)
  {
    if (string.IsNullOrWhiteSpace(request.Message))
    {
      return BadRequest("Message cannot be empty.");
    }

    if (request.Message.Length > MaxMessageLength)
    {
      return BadRequest($"Message exceeds the {MaxMessageLength}-character limit.");
    }

    var channelIds = twitchConfig.ChannelIds
      .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    var broadcasterId = channelIds.FirstOrDefault();
    if (broadcasterId is null)
    {
      ChatControllerLogs.NoBroadcasterConfigured(logger);
      return Problem("No channels are configured.");
    }

    try
    {
      var accessToken = await tokenManager.GetValidAccessTokenAsync(cancellationToken);

      using var httpClient = new HttpClient();
      httpClient.DefaultRequestHeaders.Add("Client-Id", twitchConfig.ClientId);
      httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {accessToken}");

      var body = new
      {
        broadcaster_id = broadcasterId,
        sender_id = twitchConfig.BotUserId,
        message = request.Message,
      };

      var json = JsonSerializer.Serialize(body);
      using var content = new StringContent(json, Encoding.UTF8, "application/json");
      var response = await httpClient.PostAsync("https://api.twitch.tv/helix/chat/messages", content, cancellationToken);

      if (response.IsSuccessStatusCode)
      {
        ChatControllerLogs.ChatMessageSent(logger, broadcasterId);
        return Ok();
      }

      var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
      ChatControllerLogs.ChatMessageSendFailed(logger, (int)response.StatusCode, errorBody);
      return Problem("Twitch API rejected the message.");
    }
    catch (Exception ex)
    {
      ChatControllerLogs.ChatMessageSendError(logger, ex.Message);
      return Problem("An internal error occurred while sending the message.");
    }
  }
}

/// <summary>
/// Request body for <see cref="ChatController.SendAsync"/>.
/// </summary>
public sealed record SendChatMessageRequest(string Message);

/// <summary>
/// Structured log messages for <see cref="ChatController"/>.
/// </summary>
internal static partial class ChatControllerLogs
{
  [LoggerMessage(Level = LogLevel.Warning, Message = "Cannot send chat message: no broadcaster channel IDs are configured")]
  public static partial void NoBroadcasterConfigured(ILogger logger);

  [LoggerMessage(Level = LogLevel.Information, Message = "Chat message sent to channel {ChannelId}")]
  public static partial void ChatMessageSent(ILogger logger, string channelId);

  [LoggerMessage(Level = LogLevel.Warning, Message = "Twitch API rejected chat message with status {StatusCode}: {Error}")]
  public static partial void ChatMessageSendFailed(ILogger logger, int statusCode, string error);

  [LoggerMessage(Level = LogLevel.Error, Message = "Error sending chat message: {Error}")]
  public static partial void ChatMessageSendError(ILogger logger, string error);
}
