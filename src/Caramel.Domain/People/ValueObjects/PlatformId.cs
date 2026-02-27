using Caramel.Domain.Common.Enums;

namespace Caramel.Domain.People.ValueObjects;

/// <summary>
/// Represents a person's identity on an external platform (Discord, Twitch, Web, etc.).
/// Uniquely identifies a person on a specific platform using their username and platform user ID.
/// </summary>
/// <param name="Username">The person's username on the platform.</param>
/// <param name="PlatformUserId">The numeric or string user ID on the platform.</param>
/// <param name="Platform">The platform type (Discord, Twitch, Web).</param>
public readonly record struct PlatformId(string Username, string PlatformUserId, Platform Platform);
