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
/// Sends a one-shot AI request with the supplied user input and returns the response.
/// </summary>
public sealed record AskTheOrbCommand(PersonId PersonId, Content Content) : IRequest<Result<string>>;

public sealed class AskTheOrbCommandHandler(
  ICaramelAIAgent caramelAIAgent,
  IPersonStore personStore,
  TimeProvider timeProvider
) : IRequestHandler<AskTheOrbCommand, Result<string>>
{
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

    return responseResult.Success
      ? Result.Ok(responseResult.Content)
      : Result.Fail<string>(responseResult.ErrorMessage ?? "AskTheOrb request failed.");
  }
}