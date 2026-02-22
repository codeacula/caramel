using Caramel.Domain.Common.Enums;

namespace Caramel.Database.People.Events;

public sealed record NotificationChannelAddedEvent(
  Platform PersonPlatform,
  string PersonProviderId,
  NotificationChannelType ChannelType,
  string Identifier,
  DateTime AddedOn) : BaseEvent;
