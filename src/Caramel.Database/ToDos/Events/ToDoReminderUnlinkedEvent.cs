namespace Caramel.Database.ToDos.Events;

public sealed record ToDoReminderUnlinkedEvent(
  Guid Id,
  Guid ToDoId,
  Guid ReminderId,
  DateTime UnlinkedOn);
