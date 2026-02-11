namespace Caramel.Database.ToDos.Events;

public sealed record ReminderAcknowledgedEvent(
  Guid Id,
  DateTime AcknowledgedOn);
