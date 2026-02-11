using Caramel.Domain.Common.ValueObjects;
using Caramel.Domain.Conversations.ValueObjects;
using Caramel.Domain.People.ValueObjects;

namespace Caramel.Domain.Conversations.Models;

public record Message
{
  public MessageId Id { get; init; }
  public ConversationId ConversationId { get; init; }
  public PersonId PersonId { get; init; }
  public Content Content { get; init; }
  public CreatedOn CreatedOn { get; init; }
  public FromUser FromUser { get; init; }
}
