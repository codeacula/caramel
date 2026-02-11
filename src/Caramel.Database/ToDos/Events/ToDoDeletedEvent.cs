namespace Caramel.Database.ToDos.Events;

public sealed record ToDoDeletedEvent(
  Guid Id,
  DateTime DeletedOn);
