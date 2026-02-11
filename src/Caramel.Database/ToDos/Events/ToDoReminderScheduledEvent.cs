namespace Caramel.Database.ToDos.Events;

public sealed record ToDoReminderScheduledEvent(
  Guid Id,
  Guid QuartzJobId,
  DateTime ReminderDate,
  DateTime ScheduledOn);
