using Caramel.Domain.Common.ValueObjects;
using Caramel.Domain.Conversations.ValueObjects;
using Caramel.Domain.People.ValueObjects;

namespace Caramel.Domain.Conversations.Models;

/// <summary>
/// Represents a single message within a conversation.
/// Can be either a user message or a system/assistant response.
/// </summary>
public record Message
{
  /// <summary>
  /// Gets the unique identifier for this message.
  /// </summary>
  public MessageId Id { get; init; }

  /// <summary>
  /// Gets the identifier of the conversation this message belongs to.
  /// </summary>
  public ConversationId ConversationId { get; init; }

  /// <summary>
  /// Gets the identifier of the person who sent or received this message.
  /// </summary>
  public PersonId PersonId { get; init; }

  /// <summary>
  /// Gets the text content of this message.
  /// </summary>
  public Content Content { get; init; }

  /// <summary>
  /// Gets the UTC timestamp when this message was created.
  /// </summary>
  public CreatedOn CreatedOn { get; init; }

  /// <summary>
  /// Gets a value indicating whether this message originated from the user (true) or the system (false).
  /// </summary>
  public FromUser FromUser { get; init; }
}
