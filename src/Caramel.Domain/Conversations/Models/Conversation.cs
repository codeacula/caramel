using Caramel.Domain.Common.ValueObjects;
using Caramel.Domain.Conversations.ValueObjects;
using Caramel.Domain.People.ValueObjects;

namespace Caramel.Domain.Conversations.Models;

public record Conversation
{
  public ConversationId Id { get; init; }
  public PersonId PersonId { get; init; }
  public ICollection<Message> Messages { get; init; } = [];
  public CreatedOn CreatedOn { get; init; }
  public UpdatedOn UpdatedOn { get; init; }
}
