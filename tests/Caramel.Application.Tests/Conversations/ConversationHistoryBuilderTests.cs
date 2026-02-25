using Caramel.AI.Enums;
using Caramel.Application.Conversations;
using Caramel.Domain.Common.ValueObjects;
using Caramel.Domain.Conversations.Models;
using Caramel.Domain.Conversations.ValueObjects;
using Caramel.Domain.People.ValueObjects;

namespace Caramel.Application.Tests.Conversations;

public class ConversationHistoryBuilderTests
{
  [Fact]
  public void BuildForToolPlanningIncludesRecentMessagesAndTimezoneContext()
  {
    var personId = new PersonId(Guid.NewGuid());
    var conversationId = new ConversationId(Guid.NewGuid());

    // Keep total at or below minMessages (6) so all messages are returned,
    // including the timezone context message.
    var messages = new List<Message>
    {
      CreateMessage(conversationId, personId, "What timezone are you in?", false, DateTime.UtcNow.AddMinutes(-6)),
      CreateMessage(conversationId, personId, "PST", true, DateTime.UtcNow.AddMinutes(-5)),
      CreateMessage(conversationId, personId, "Add buy milk", true, DateTime.UtcNow.AddMinutes(-4)),
      CreateMessage(conversationId, personId, "Added!", false, DateTime.UtcNow.AddMinutes(-3)),
      CreateMessage(conversationId, personId, "Remind me tomorrow", true, DateTime.UtcNow.AddMinutes(-2)),
      CreateMessage(conversationId, personId, "Sure", false, DateTime.UtcNow.AddMinutes(-1))
    };

    var conversation = new Conversation
    {
      Id = conversationId,
      PersonId = personId,
      Messages = messages,
      CreatedOn = new CreatedOn(DateTime.UtcNow.AddMinutes(-10)),
      UpdatedOn = new UpdatedOn(DateTime.UtcNow)
    };

    var history = ConversationHistoryBuilder.BuildForToolPlanning(conversation);

    Assert.Contains(history, m => m.Content.Contains("timezone", StringComparison.OrdinalIgnoreCase));
    Assert.Contains(history, m => m.Content == "Sure" && m.Role == ChatRole.Assistant);
    Assert.True(history.Count >= 6);
  }

  private static Message CreateMessage(
    ConversationId conversationId,
    PersonId personId,
    string content,
    bool fromUser,
    DateTime createdOn)
  {
    return new Message
    {
      Id = new MessageId(Guid.NewGuid()),
      ConversationId = conversationId,
      PersonId = personId,
      Content = new Content(content),
      CreatedOn = new CreatedOn(createdOn),
      FromUser = new FromUser(fromUser)
    };
  }
}
