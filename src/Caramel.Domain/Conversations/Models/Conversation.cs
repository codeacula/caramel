using Caramel.Domain.Common.ValueObjects;
using Caramel.Domain.Conversations.ValueObjects;
using Caramel.Domain.People.ValueObjects;

namespace Caramel.Domain.Conversations.Models;

/// <summary>
/// Represents a conversation between a person and the system.
/// Contains the conversation history and metadata about when it was created and updated.
/// </summary>
public record Conversation
{
  /// <summary>
  /// Gets the unique identifier for this conversation.
  /// </summary>
  public ConversationId Id { get; init; }

  /// <summary>
  /// Gets the identifier of the person participating in this conversation.
  /// </summary>
  public PersonId PersonId { get; init; }

  /// <summary>
  /// Gets the collection of messages in this conversation.
  /// </summary>
  public ICollection<Message> Messages { get; init; } = [];

  /// <summary>
  /// Gets the UTC timestamp when this conversation was created.
  /// </summary>
  public CreatedOn CreatedOn { get; init; }

  /// <summary>
  /// Gets the UTC timestamp when this conversation was last updated.
  /// </summary>
  public UpdatedOn UpdatedOn { get; init; }
}
