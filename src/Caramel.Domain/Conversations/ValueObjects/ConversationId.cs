namespace Caramel.Domain.Conversations.ValueObjects;

/// <summary>
/// Represents a unique identifier for a conversation.
/// </summary>
public readonly record struct ConversationId(Guid Value);
