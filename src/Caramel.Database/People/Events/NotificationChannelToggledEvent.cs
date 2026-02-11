using Caramel.Domain.Common.Enums;

namespace Caramel.Database.People.Events;

public sealed record NotificationChannelToggledEvent(
  Platform PersonPlatform,
  string PersonProviderId,
  NotificationChannelType ChannelType,
  string Identifier,
  bool IsEnabled,
  DateTime ToggledOn) : BaseEvent;
