using Caramel.Domain.Common.Enums;

namespace Caramel.Domain.People.ValueObjects;

public readonly record struct PlatformId(string Username, string PlatformUserId, Platform Platform);
