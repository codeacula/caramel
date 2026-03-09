namespace Caramel.Domain.Conversations.ValueObjects;

/// <summary>
/// Represents a unique identifier for a message.
/// </summary>
/// <param name="Value"></param>
public readonly record struct MessageId(Guid Value);
