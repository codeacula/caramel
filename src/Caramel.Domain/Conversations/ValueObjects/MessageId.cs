namespace Caramel.Domain.Conversations.ValueObjects;

/// <summary>
/// Represents a unique identifier for a message.
/// </summary>
public readonly record struct MessageId(Guid Value);
