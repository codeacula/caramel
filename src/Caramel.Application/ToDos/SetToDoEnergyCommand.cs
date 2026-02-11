using Caramel.Core.ToDos;
using Caramel.Domain.People.ValueObjects;
using Caramel.Domain.ToDos.ValueObjects;

using FluentResults;

namespace Caramel.Application.ToDos;

public sealed record SetToDoEnergyCommand(
  PersonId PersonId,
  ToDoId ToDoId,
  Energy Energy
) : IRequest<Result>;

public sealed class SetToDoEnergyCommandHandler(IToDoStore toDoStore) : IRequestHandler<SetToDoEnergyCommand, Result>
{
  public async Task<Result> Handle(SetToDoEnergyCommand request, CancellationToken cancellationToken)
  {
    try
    {
      var ownershipResult = await VerifyOwnershipAsync(request.ToDoId, request.PersonId, cancellationToken);
      return ownershipResult.IsFailed
        ? ownershipResult
        : await toDoStore.UpdateEnergyAsync(request.ToDoId, request.Energy, cancellationToken);
    }
    catch (Exception ex)
    {
      return Result.Fail(ex.Message);
    }
  }

  private async Task<Result> VerifyOwnershipAsync(ToDoId toDoId, PersonId personId, CancellationToken cancellationToken)
  {
    var todoResult = await toDoStore.GetAsync(toDoId, cancellationToken);
    if (todoResult.IsFailed)
    {
      return Result.Fail("To-Do not found");
    }

    return todoResult.Value.PersonId.Value != personId.Value
      ? Result.Fail("You don't have permission to update this to-do")
      : Result.Ok();
  }
}
