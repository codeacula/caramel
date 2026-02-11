namespace Caramel.Database.ToDos.Events;

public sealed record ToDoUpdatedEvent(
  Guid Id,
  string Description,
  DateTime UpdatedOn);
