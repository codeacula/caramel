using Caramel.AI.DTOs;
using Caramel.AI.Enums;
using Caramel.Domain.Conversations.Models;

namespace Caramel.Application.Conversations;

/// <summary>
/// Builds conversation message histories in different formats for AI processing.
/// Handles message ordering, filtering, and conversion to AI-compatible DTOs.
/// </summary>
public sealed class ConversationHistoryBuilder
{
  private const int DefaultMaxMessages = 15;
  private const int DefaultMinMessages = 6;

  /// <summary>
  /// Builds a conversation history for tool planning by selecting recent messages from the conversation.
  /// </summary>
  /// <param name="conversation">The conversation to build history from.</param>
  /// <param name="maxMessages">The maximum number of recent messages to include (default: 15).</param>
  /// <param name="minMessages">The minimum number of recent messages to ensure are included (default: 6).</param>
  /// <returns>A list of ChatMessageDTOs ordered chronologically.</returns>
  public static List<ChatMessageDTO> BuildForToolPlanning(
    Conversation conversation,
    int maxMessages = DefaultMaxMessages,
    int minMessages = DefaultMinMessages)
  {
    var orderedMessages = conversation.Messages
      .OrderBy(m => m.CreatedOn.Value)
      .ToList();

    if (orderedMessages.Count <= minMessages)
    {
      return ToChatMessages(orderedMessages);
    }

    var recentMessages = orderedMessages.TakeLast(minMessages);

    var selected = recentMessages
      .DistinctBy(m => m.Id.Value)
      .OrderBy(m => m.CreatedOn.Value)
      .ToList();

    if (selected.Count > maxMessages)
    {
      selected = [.. selected.TakeLast(maxMessages)];
    }

    return ToChatMessages(selected);
  }

  /// <summary>
  /// Builds the complete conversation history for AI response generation.
  /// </summary>
  /// <param name="conversation">The conversation to build history from.</param>
  /// <returns>All messages from the conversation ordered chronologically as ChatMessageDTOs.</returns>
  public static List<ChatMessageDTO> BuildForResponse(Conversation conversation)
  {
    var orderedMessages = conversation.Messages
      .OrderBy(m => m.CreatedOn.Value)
      .ToList();

    return ToChatMessages(orderedMessages);
  }

  /// <summary>
  /// Converts domain Message objects to AI-compatible ChatMessageDTOs.
  /// </summary>
  /// <param name="messages">The domain message objects to convert.</param>
  /// <returns>A list of ChatMessageDTOs with appropriate roles based on message origin.</returns>
  private static List<ChatMessageDTO> ToChatMessages(IEnumerable<Message> messages)
  {
    return [.. messages
      .Select(m => new ChatMessageDTO(
        m.FromUser.Value ? ChatRole.User : ChatRole.Assistant,
        m.Content.Value,
        m.CreatedOn.Value))];
  }
}
