namespace Caramel.Database.ToDos.Events;

public sealed record ToDoCreatedEvent(
  Guid Id,
  Guid PersonId,
  string Description,
  int Priority,
  int Energy,
  int Interest,
  DateTime CreatedOn);
