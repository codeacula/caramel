namespace Caramel.Database.Conversations.Events;

public sealed record CaramelRepliedEvent : BaseEvent
{
  public required string Message { get; init; }
}
