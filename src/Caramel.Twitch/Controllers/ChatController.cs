using Microsoft.AspNetCore.Mvc;

namespace Caramel.Twitch.Controllers;

[ApiController]
[Route("chat")]
public sealed class ChatController(
  ITwitchChatClient chatClient,
  ILogger<ChatController> logger) : ControllerBase
{
  private const int MaxMessageLength = 500;

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

    var result = await chatClient.SendChatMessageAsync(request.Message, cancellationToken);
    if (result.IsSuccess)
    {
      return Ok();
    }

    ChatControllerLogs.ChatMessageSendFailed(logger, result.Errors[0].Message);
    return Problem("Failed to send chat message.");
  }
}

public sealed record SendChatMessageRequest(string Message);

internal static partial class ChatControllerLogs
{
  [LoggerMessage(Level = LogLevel.Warning, Message = "Chat message send failed: {Error}")]
  public static partial void ChatMessageSendFailed(ILogger logger, string error);
}
