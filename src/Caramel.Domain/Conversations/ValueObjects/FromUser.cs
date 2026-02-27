namespace Caramel.Domain.Conversations.ValueObjects;

/// <summary>
/// Indicates whether a message originated from a user (true) or the system (false).
/// </summary>
public readonly record struct FromUser(bool Value);
