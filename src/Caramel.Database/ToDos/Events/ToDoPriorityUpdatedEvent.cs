namespace Caramel.Database.ToDos.Events;

public sealed record ToDoPriorityUpdatedEvent(
  Guid Id,
  int Priority,
  DateTime UpdatedOn
);
