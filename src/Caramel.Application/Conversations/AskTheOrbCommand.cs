using Caramel.AI;
using Caramel.AI.DTOs;
using Caramel.AI.Enums;
using Caramel.Core;
using Caramel.Core.People;
using Caramel.Domain.Common.ValueObjects;
using Caramel.Domain.People.ValueObjects;

using FluentResults;

namespace Caramel.Application.Conversations;

/// <summary>
/// Asks the AI system a question and gets a response without storing it in the conversation history.
/// </summary>
/// <param name="PersonId">The ID of the person asking the question.</param>
/// <param name="Content">The question text to ask the AI.</param>
public sealed record AskTheOrbCommand(PersonId PersonId, Content Content) : IRequest<Result<string>>;

/// <summary>
/// Handles the execution of AskTheOrbCommand requests.
/// Queries the AI agent and returns a response without persisting the exchange.
/// </summary>
public sealed class AskTheOrbCommandHandler(
  ICaramelAIAgent caramelAIAgent,
  IPersonStore personStore,
  TimeProvider timeProvider
) : IRequestHandler<AskTheOrbCommand, Result<string>>
{
  /// <summary>
  /// Handles the command to ask the AI a question and receive a response.
  /// </summary>
  /// <param name="request">The ask-the-orb command containing the person ID and question content.</param>
  /// <param name="cancellationToken">Cancellation token for async operation.</param>
  /// <returns>A Result containing the AI response string, or an error if the request failed.</returns>
  public async Task<Result<string>> Handle(AskTheOrbCommand request, CancellationToken cancellationToken)
  {
    if (string.IsNullOrWhiteSpace(request.Content.Value))
    {
      return Result.Fail<string>("AskTheOrb request content cannot be empty.");
    }

    var personResult = await personStore.GetAsync(request.PersonId, cancellationToken);
    if (personResult.IsFailed)
    {
      return Result.Fail<string>($"Failed to load person {request.PersonId.Value}");
    }

    var person = personResult.Value;
    var userTimezone = person.TimeZoneId?.Value ?? "UTC";

    var messages = new List<ChatMessageDTO>
    {
      new(ChatRole.User, request.Content.Value, timeProvider.GetUtcDateTime())
    };

    var responseResult = await caramelAIAgent
      .CreateResponseRequest(messages, actionsSummary: "None", userTimezone)
      .ExecuteAsync(cancellationToken);

    return responseResult.Success switch
    {
      true => Result.Ok(responseResult.Content),
      false when responseResult.ErrorMessage != null => Result.Fail<string>(responseResult.ErrorMessage),
      _ => Result.Fail<string>("AskTheOrb request failed.")
    };
  }
}
