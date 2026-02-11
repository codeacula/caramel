using Caramel.Domain.Common.Enums;

namespace Caramel.Database.People.Events;

public sealed record PersonCreatedEvent(string Username, Platform Platform, string PlatformUserId) : BaseEvent;
