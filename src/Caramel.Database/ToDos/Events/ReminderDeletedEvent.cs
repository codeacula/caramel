namespace Caramel.Database.ToDos.Events;

public sealed record ReminderDeletedEvent(
  Guid Id,
  DateTime DeletedOn);
