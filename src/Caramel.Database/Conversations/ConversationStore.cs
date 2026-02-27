using Caramel.Core;
using Caramel.Core.Conversations;
using Caramel.Database.Conversations.Events;
using Caramel.Domain.Common.ValueObjects;
using Caramel.Domain.Conversations.Models;
using Caramel.Domain.Conversations.ValueObjects;
using Caramel.Domain.People.ValueObjects;

using FluentResults;

using Marten;

namespace Caramel.Database.Conversations;

public sealed class ConversationStore(IDocumentSession session, TimeProvider timeProvider) : IConversationStore
{
  public async Task<Result<Conversation>> AddMessageAsync(ConversationId conversationId, Content message, CancellationToken cancellationToken = default)
  {
    return await ResultExtensions.ExecuteAsync(
      () => AddMessageInternalAsync(conversationId, message, cancellationToken),
      "Invalid operation adding message to conversation",
      "Unexpected error adding message");
  }

  private async Task<Conversation> AddMessageInternalAsync(ConversationId conversationId, Content message, CancellationToken cancellationToken)
  {
    _ = session.Events.Append(conversationId.Value, new UserSentMessageEvent
    {
      Id = conversationId.Value,
      Message = message.Value,
      CreatedOn = timeProvider.GetUtcDateTime()
    });

    await session.SaveChangesAsync(cancellationToken);

    var conversation = await session.Query<DbConversation>().FirstOrDefaultAsync(u => u.Id == conversationId.Value, cancellationToken);
    return (Conversation)(conversation ?? throw new InvalidOperationException("Failed to retrieve message after adding it."));
  }

  public async Task<Result<Conversation>> AddReplyAsync(ConversationId conversationId, Content reply, CancellationToken cancellationToken = default)
  {
    return await ResultExtensions.ExecuteAsync(
      () => AddReplyInternalAsync(conversationId, reply, cancellationToken),
      "Invalid operation adding reply to conversation",
      "Unexpected error adding reply");
  }

  private async Task<Conversation> AddReplyInternalAsync(ConversationId conversationId, Content reply, CancellationToken cancellationToken)
  {
    _ = session.Events.Append(conversationId.Value, new CaramelRepliedEvent
    {
      Id = conversationId.Value,
      Message = reply.Value,
      CreatedOn = timeProvider.GetUtcDateTime()
    });

    await session.SaveChangesAsync(cancellationToken);

    var conversation = await session.Query<DbConversation>().FirstOrDefaultAsync(u => u.Id == conversationId.Value, cancellationToken);
    return (Conversation)(conversation ?? throw new InvalidOperationException("Failed to retrieve reply after adding it."));
  }

  public async Task<Result<Conversation>> CreateAsync(PersonId id, CancellationToken cancellationToken = default)
  {
    return await ResultExtensions.ExecuteAsync(
      () => CreateInternalAsync(id, cancellationToken),
      "Invalid operation creating conversation",
      "Unexpected error creating conversation");
  }

  private async Task<Conversation> CreateInternalAsync(PersonId id, CancellationToken cancellationToken)
  {
    var conversationId = Guid.NewGuid();
    var ev = new ConversationStartedEvent
    {
      Id = conversationId,
      PersonId = id.Value,
      CreatedOn = timeProvider.GetUtcDateTime()
    };

    _ = session.Events.StartStream<DbConversation>(conversationId, [ev]);
    await session.SaveChangesAsync(cancellationToken);

    var newConversation = await session.Events.AggregateStreamAsync<DbConversation>(conversationId, token: cancellationToken);

    return (Conversation)(newConversation ?? throw new InvalidOperationException($"Failed to create new conversation for person {id.Value}"));
  }

  public async Task<Result<Conversation>> GetAsync(ConversationId conversationId, CancellationToken cancellationToken = default)
  {
    return await ResultExtensions.ExecuteAsync(
      () => GetInternalAsync(conversationId, cancellationToken),
      "Invalid operation retrieving conversation",
      "Unexpected error retrieving conversation");
  }

  private async Task<Conversation> GetInternalAsync(ConversationId conversationId, CancellationToken cancellationToken)
  {
    var conversation = await session.Query<DbConversation>().FirstOrDefaultAsync(u => u.Id == conversationId.Value, cancellationToken);
    return (Conversation)(conversation ?? throw new InvalidOperationException("No conversation found."));
  }

  public async Task<Result<Conversation>> GetConversationByPersonIdAsync(PersonId personId, CancellationToken cancellationToken = default)
  {
    return await ResultExtensions.ExecuteAsync(
      () => GetConversationByPersonIdInternalAsync(personId, cancellationToken),
      "Invalid operation retrieving conversation by person",
      "Unexpected error retrieving conversation by person");
  }

  private async Task<Conversation> GetConversationByPersonIdInternalAsync(PersonId personId, CancellationToken cancellationToken)
  {
    var conversation = await session.Query<DbConversation>()
      .FirstOrDefaultAsync(u => u.PersonId == personId.Value, cancellationToken);
    return (Conversation)(conversation ?? throw new InvalidOperationException("No conversation found."));
  }

  public async Task<Result<Conversation>> GetOrCreateConversationByPersonIdAsync(PersonId personId, CancellationToken cancellationToken = default)
  {
    return await ResultExtensions.ExecuteAsync(
      () => GetOrCreateInternalAsync(personId, cancellationToken),
      "Invalid operation retrieving or creating conversation",
      "Unexpected error retrieving or creating conversation");
  }

  private async Task<Conversation> GetOrCreateInternalAsync(PersonId personId, CancellationToken cancellationToken)
  {
    var conversation = await session.Query<DbConversation>()
      .FirstOrDefaultAsync(u => u.PersonId == personId.Value, cancellationToken);

    if (conversation is not null)
    {
      return (Conversation)conversation;
    }

    // Create new conversation
    return await CreateInternalAsync(personId, cancellationToken);
  }
}
