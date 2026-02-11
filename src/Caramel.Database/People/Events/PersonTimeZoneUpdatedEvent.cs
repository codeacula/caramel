namespace Caramel.Database.People.Events;

public sealed record PersonTimeZoneUpdatedEvent(string TimeZoneId, DateTime UpdatedOn) : BaseEvent;
