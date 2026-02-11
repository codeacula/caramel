using Caramel.Domain.Common.ValueObjects;
using Caramel.Domain.Conversations.Models;
using Caramel.Domain.Conversations.ValueObjects;
using Caramel.Domain.People.ValueObjects;

using FluentResults;

namespace Caramel.Core.Conversations;

public interface IConversationStore
{
  Task<Result<Conversation>> AddMessageAsync(ConversationId conversationId, Content message, CancellationToken cancellationToken = default);
  Task<Result<Conversation>> AddReplyAsync(ConversationId conversationId, Content reply, CancellationToken cancellationToken = default);
  Task<Result<Conversation>> CreateAsync(PersonId id, CancellationToken cancellationToken = default);
  Task<Result<Conversation>> GetAsync(ConversationId conversationId, CancellationToken cancellationToken = default);
  Task<Result<Conversation>> GetConversationByPersonIdAsync(PersonId personId, CancellationToken cancellationToken = default);
  Task<Result<Conversation>> GetOrCreateConversationByPersonIdAsync(PersonId personId, CancellationToken cancellationToken = default);
}
