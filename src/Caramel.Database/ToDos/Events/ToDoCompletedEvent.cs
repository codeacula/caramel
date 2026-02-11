namespace Caramel.Database.ToDos.Events;

public sealed record ToDoCompletedEvent(
  Guid Id,
  DateTime CompletedOn);
