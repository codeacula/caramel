using Caramel.Domain.Common.Enums;

namespace Caramel.Database.People.Events;

public sealed record NotificationChannelRemovedEvent(
  Platform PersonPlatform,
  string PersonProviderId,
  NotificationChannelType ChannelType,
  string Identifier,
  DateTime RemovedOn) : BaseEvent;
