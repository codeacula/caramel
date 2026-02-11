namespace Caramel.Database.ToDos.Events;

public sealed record ReminderSentEvent(
  Guid Id,
  DateTime SentOn);
