using Caramel.AI;
using Caramel.AI.Models;
using Caramel.AI.Planning;
using Caramel.AI.Requests;
using Caramel.AI.Tooling;
using Caramel.Application.People;
using Caramel.Core;
using Caramel.Core.Conversations;
using Caramel.Core.Logging;
using Caramel.Core.People;
using Caramel.Domain.Common.ValueObjects;
using Caramel.Domain.Conversations.Models;
using Caramel.Domain.People.Models;
using Caramel.Domain.People.ValueObjects;

using FluentResults;

namespace Caramel.Application.Conversations;

/// <summary>
/// Processes an incoming message from a supported platform, generates an AI response, and stores both in conversation history.
/// </summary>
/// <param name="PersonId">The ID of the person sending the message.</param>
/// <param name="Content">The message content to process.</param>
/// <seealso cref="ProcessIncomingMessageCommandHandler"/>
public sealed record ProcessIncomingMessageCommand(PersonId PersonId, Content Content) : IRequest<Result<Reply>>;

/// <summary>
/// Handles the execution of ProcessIncomingMessageCommand requests.
/// Orchestrates conversation storage, AI processing with tool planning and validation, and response generation.
/// </summary>
public sealed class ProcessIncomingMessageCommandHandler(
  ICaramelAIAgent caramelAIAgent,
  IConversationStore conversationStore,
  ILogger<ProcessIncomingMessageCommandHandler> logger,
  IPersonStore personStore,
  PersonConfig personConfig,
  TimeProvider timeProvider
) : IRequestHandler<ProcessIncomingMessageCommand, Result<Reply>>
{
  /// <summary>
  /// Processes an incoming user message through the full conversation pipeline.
  /// </summary>
  /// <param name="request">The command containing the person ID and message content.</param>
  /// <param name="cancellationToken">Cancellation token for async operation.</param>
  /// <returns>A Result containing the system Reply, or an error if processing failed.</returns>
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
    catch (OperationCanceledException)
    {
      throw;
    }
    catch (InvalidOperationException ex)
    {
      DataAccessLogs.UnhandledMessageProcessingError(logger, ex, request.PersonId.Value.ToString());
      return Result.Fail<Reply>($"Invalid message processing state: {ex.Message}");
    }
    catch (Exception ex)
    {
      DataAccessLogs.UnhandledMessageProcessingError(logger, ex, request.PersonId.Value.ToString());
      return Result.Fail<Reply>("Unexpected error processing message");
    }
  }

  private async Task<Result<Conversation>> GetOrCreateConversationWithMessageAsync(Person person, string messageContent, CancellationToken cancellationToken)
  {
    /// <summary>
    /// Gets or creates a conversation and adds the incoming message to it.
    /// </summary>
    /// <param name="person">The person sending the message.</param>
    /// <param name="messageContent">The message text to add.</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    /// <returns>A Result containing the updated Conversation, or an error if the operation failed.</returns>

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
    /// <summary>
    /// Processes the conversation with AI through tool planning, validation, and response generation phases.
    /// </summary>
    /// <param name="conversation">The conversation containing the message to process.</param>
    /// <param name="person">The person sending the message.</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    /// <returns>The AI-generated response string.</returns>

    var responseMessages = ConversationHistoryBuilder.BuildForResponse(conversation);
    var plugins = CreatePlugins(person);

    // Get context variables
    var userTimezone = GetUserTimezone(person);
    var toolPlanningMessages = ConversationHistoryBuilder.BuildForToolPlanning(conversation);

    // Phase 1: Tool Planning (JSON output)
    var toolPlanResult = await caramelAIAgent
      .CreateToolPlanningRequest(toolPlanningMessages, userTimezone)
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
      toolPlanningMessages);

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

     if (logger.IsEnabled(LogLevel.Information))
     {
       ConversationLogs.ActionsTaken(logger, person.Id.Value, [actionsSummary]);
     }

     var responseResult = await caramelAIAgent
       .CreateResponseRequest(responseMessages, actionsSummary, userTimezone)
       .ExecuteAsync(cancellationToken);

    return !responseResult.Success
      ? $"I encountered an issue while processing your request: {responseResult.ErrorMessage}"
      : responseResult.Content;
  }

  private Dictionary<string, object> CreatePlugins(Person person)
  {
    /// <summary>
    /// Creates the plugin registry for tool execution.
    /// </summary>
    /// <param name="person">The person context for which to create plugins.</param>
    /// <returns>A dictionary of available plugins keyed by plugin name.</returns>

    var personPlugin = new PersonPlugin(personStore, personConfig, person.Id);

    return new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
    {
      [PersonPlugin.PluginName] = personPlugin
    };
  }

    private async Task SaveReplyAsync(Conversation conversation, string response, CancellationToken cancellationToken)
    {
      /// <summary>
      /// Saves the AI response as a system reply in the conversation.
      /// </summary>
      /// <param name="conversation">The conversation to add the reply to.</param>
      /// <param name="response">The response text to save.</param>
      /// <param name="cancellationToken">Cancellation token for async operation.</param>

     var addReplyResult = await conversationStore.AddReplyAsync(conversation.Id, new Content(response), cancellationToken);

     if (addReplyResult.IsFailed)
     {
       if (logger.IsEnabled(LogLevel.Error))
       {
         DataAccessLogs.UnableToSaveMessageToConversation(logger, conversation.Id.Value, response);
       }
     }
   }

  private Result<Reply> CreateReplyToUser(string response)
  {
    /// <summary>
    /// Creates a Reply object to return to the user.
    /// </summary>
    /// <param name="response">The response text.</param>
    /// <returns>A Result containing the Reply object.</returns>

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
    /// <summary>
    /// Gets the user's timezone, defaulting to UTC if not configured.
    /// </summary>
    /// <param name="person">The person whose timezone to retrieve.</param>
    /// <returns>The IANA timezone ID string for the person, or "UTC" as default.</returns>

    return person.TimeZoneId?.Value ?? "UTC";
  }

  private sealed record ActiveTodosSnapshot(string Summary, IReadOnlyCollection<string> TodoIds);
  /// <summary>
  /// Snapshot of active todos used during conversation processing.
  /// </summary>
}
