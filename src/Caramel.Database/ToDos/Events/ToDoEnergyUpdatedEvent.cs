namespace Caramel.Database.ToDos.Events;

public sealed record ToDoEnergyUpdatedEvent(
  Guid Id,
  int Energy,
  DateTime UpdatedOn
);
