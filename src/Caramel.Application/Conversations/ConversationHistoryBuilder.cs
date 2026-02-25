using Caramel.AI.DTOs;
using Caramel.AI.Enums;
using Caramel.Domain.Conversations.Models;

namespace Caramel.Application.Conversations;

public sealed class ConversationHistoryBuilder
{
  private const int DefaultMaxMessages = 15;
  private const int DefaultMinMessages = 6;

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

  public static List<ChatMessageDTO> BuildForResponse(Conversation conversation)
  {
    var orderedMessages = conversation.Messages
      .OrderBy(m => m.CreatedOn.Value)
      .ToList();

    return ToChatMessages(orderedMessages);
  }

  private static List<ChatMessageDTO> ToChatMessages(IEnumerable<Message> messages)
  {
    return [.. messages
      .Select(m => new ChatMessageDTO(
        m.FromUser.Value ? ChatRole.User : ChatRole.Assistant,
        m.Content.Value,
        m.CreatedOn.Value))];
  }
}
