using Caramel.AI;
using Caramel.AI.Models;
using Caramel.AI.Planning;
using Caramel.AI.Requests;
using Caramel.AI.Tooling;
using Caramel.Application.People;
using Caramel.Application.Reminders;
using Caramel.Application.ToDos;
using Caramel.Core;
using Caramel.Core.Conversations;
using Caramel.Core.Logging;
using Caramel.Core.People;
using Caramel.Core.ToDos;
using Caramel.Domain.Common.ValueObjects;
using Caramel.Domain.Conversations.Models;
using Caramel.Domain.People.Models;
using Caramel.Domain.People.ValueObjects;

using FluentResults;

namespace Caramel.Application.Conversations;

/// <summary>
/// Tells the system to process an incoming message from a supported platform.
/// </summary>
/// <param name="PersonId">The ID of the person sending the message.</param>
/// <param name="Content">The message content.</param>
/// <seealso cref="ProcessIncomingMessageCommandHandler"/>
public sealed record ProcessIncomingMessageCommand(PersonId PersonId, Content Content) : IRequest<Result<Reply>>;

public sealed class ProcessIncomingMessageCommandHandler(
  ICaramelAIAgent caramelAIAgent,
  IConversationStore conversationStore,
  IFuzzyTimeParser fuzzyTimeParser,
  ILogger<ProcessIncomingMessageCommandHandler> logger,
  IMediator mediator,
  IPersonStore personStore,
  IToDoStore toDoStore,
  PersonConfig personConfig,
  TimeProvider timeProvider
) : IRequestHandler<ProcessIncomingMessageCommand, Result<Reply>>
{
  public async Task<Result<Reply>> Handle(ProcessIncomingMessageCommand request, CancellationToken cancellationToken = default)
  {
    try
    {
      var personResult = await personStore.GetAsync(request.PersonId, cancellationToken);
      if (personResult.IsFailed)
      {
        return Result.Fail<Reply>($"Failed to load person {request.PersonId.Value}");
      }

      var person = personResult.Value;

      var conversationResult = await GetOrCreateConversationWithMessageAsync(person, request.Content.Value, cancellationToken);
      if (conversationResult.IsFailed)
      {
        return conversationResult.ToResult<Reply>();
      }

      var response = await ProcessWithAIAsync(conversationResult.Value, person, cancellationToken);

      await SaveReplyAsync(conversationResult.Value, response, cancellationToken);

      return CreateReplyToUser(response);
    }
    catch (Exception ex)
    {
      DataAccessLogs.UnhandledMessageProcessingError(logger, ex, request.PersonId.Value.ToString());
      return Result.Fail<Reply>(ex.Message);
    }
  }

  private async Task<Result<Conversation>> GetOrCreateConversationWithMessageAsync(Person person, string messageContent, CancellationToken cancellationToken)
  {
    var convoResult = await conversationStore.GetOrCreateConversationByPersonIdAsync(person.Id, cancellationToken);

    if (convoResult.IsFailed)
    {
      return Result.Fail<Conversation>("Unable to fetch conversation.");
    }

    convoResult = await conversationStore.AddMessageAsync(convoResult.Value.Id, new Content(messageContent), cancellationToken);

    return convoResult.IsFailed ? Result.Fail<Conversation>("Unable to add message to conversation.") : convoResult;
  }

  private async Task<string> ProcessWithAIAsync(Conversation conversation, Person person, CancellationToken cancellationToken)
  {
    var responseMessages = ConversationHistoryBuilder.BuildForResponse(conversation);
    var plugins = CreatePlugins(person);

    // Get context variables
    var userTimezone = GetUserTimezone(person);
    var activeTodosSnapshot = await BuildActiveTodosSnapshotAsync(person.Id, cancellationToken);
    var toolPlanningMessages = ConversationHistoryBuilder.BuildForToolPlanning(conversation, activeTodosSnapshot.TodoIds);

    // Phase 1: Tool Planning (JSON output)
    var toolPlanResult = await caramelAIAgent
      .CreateToolPlanningRequest(toolPlanningMessages, userTimezone, activeTodosSnapshot.Summary)
      .ExecuteAsync(cancellationToken);

    var toolPlan = new ToolPlan();
    if (toolPlanResult.Success)
    {
      var parseResult = ToolPlanParser.Parse(toolPlanResult.Content);
      if (parseResult.IsSuccess)
      {
        toolPlan = parseResult.Value;
        ConversationLogs.ToolPlanReceived(logger, person.Id.Value, toolPlan.ToolCalls.Count);
      }
      else
      {
        ConversationLogs.ToolPlanParsingFailed(logger, person.Id.Value, parseResult.Errors.Count > 0 ? parseResult.Errors[0].Message : "Unknown error");
      }
    }
    else
    {
      ConversationLogs.ToolPlanningRequestFailed(logger, person.Id.Value, toolPlanResult.ErrorMessage);
    }

    // Phase 2: Validate + Execute Tool Calls
    var validationContext = new ToolPlanValidationContext(
      plugins,
      toolPlanningMessages,
      activeTodosSnapshot.TodoIds);

    var validationResult = ToolPlanValidator.Validate(toolPlan, validationContext);
    var toolResults = new List<ToolCallResult>(validationResult.BlockedCalls);

    if (validationResult.ApprovedCalls.Count > 0)
    {
      var executed = await ToolExecutionService.ExecuteToolPlanAsync(validationResult.ApprovedCalls, plugins, cancellationToken);
      toolResults.AddRange(executed);
    }

    ConversationLogs.ToolExecutionCompleted(
      logger,
      person.Id.Value,
      validationResult.ApprovedCalls.Count,
      validationResult.BlockedCalls.Count,
      toolResults.Count - validationResult.BlockedCalls.Count);

    // Phase 3: Response Generation
    var actionsSummary = toolResults.Count == 0
      ? "None"
      : string.Join("\n", toolResults.Select(tc => $"- {tc.ToSummary()}"));

    ConversationLogs.ActionsTaken(logger, person.Id.Value, [actionsSummary]);

    var responseResult = await caramelAIAgent
      .CreateResponseRequest(responseMessages, actionsSummary, userTimezone)
      .ExecuteAsync(cancellationToken);

    return !responseResult.Success
      ? $"I encountered an issue while processing your request: {responseResult.ErrorMessage}"
      : responseResult.Content;
  }

  private Dictionary<string, object> CreatePlugins(Person person)
  {
    var toDoPlugin = new ToDoPlugin(mediator, personStore, fuzzyTimeParser, timeProvider, personConfig, person.Id);
    var remindersPlugin = new RemindersPlugin(mediator, personStore, fuzzyTimeParser, timeProvider, personConfig, person.Id);
    var personPlugin = new PersonPlugin(personStore, personConfig, person.Id);

    return new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
    {
      [ToDoPlugin.PluginName] = toDoPlugin,
      [RemindersPlugin.PluginName] = remindersPlugin,
      [PersonPlugin.PluginName] = personPlugin
    };
  }

  private async Task SaveReplyAsync(Conversation conversation, string response, CancellationToken cancellationToken)
  {
    var addReplyResult = await conversationStore.AddReplyAsync(conversation.Id, new Content(response), cancellationToken);

    if (addReplyResult.IsFailed)
    {
      DataAccessLogs.UnableToSaveMessageToConversation(logger, conversation.Id.Value, response);
    }
  }

  private Result<Reply> CreateReplyToUser(string response)
  {
    var currentTime = timeProvider.GetUtcDateTime();
    return Result.Ok(new Reply
    {
      Content = new(response),
      CreatedOn = new(currentTime),
      UpdatedOn = new(currentTime)
    });
  }

  private static string GetUserTimezone(Person person)
  {
    return person.TimeZoneId?.Value ?? "UTC";
  }

  private async Task<ActiveTodosSnapshot> BuildActiveTodosSnapshotAsync(PersonId personId, CancellationToken cancellationToken)
  {
    var todosResult = await toDoStore.GetByPersonIdAsync(personId, includeCompleted: false, cancellationToken);

    if (todosResult.IsFailed || !todosResult.Value.Any())
    {
      return new ActiveTodosSnapshot("No active todos", []);
    }

    var todos = todosResult.Value
      .OrderBy(t => t.DueDate?.Value)
      .Take(10) // Limit to 10 to avoid bloating the prompt
      .ToList();

    var summary = string.Join("\n", todos.Select(t =>
    {
      var dueDate = t.DueDate?.Value.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture) ?? "No due date";
      return $"• [{t.Id.Value}] {t.Description.Value} (Due: {dueDate})";
    }));

    var todoIds = todos.ConvertAll(t => t.Id.Value.ToString());
    return new ActiveTodosSnapshot(summary, todoIds);
  }

  private sealed record ActiveTodosSnapshot(string Summary, IReadOnlyCollection<string> TodoIds);
}
