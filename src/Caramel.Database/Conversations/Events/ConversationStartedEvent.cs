namespace Caramel.Database.Conversations.Events;

public sealed record ConversationStartedEvent : BaseEvent
{
  public required Guid PersonId { get; init; }
}
