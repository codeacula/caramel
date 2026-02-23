using Caramel.Core.ToDos;
using Caramel.Domain.People.ValueObjects;
using Caramel.Domain.ToDos.ValueObjects;

using FluentResults;

namespace Caramel.Application.ToDos;

public sealed record SetToDoPriorityCommand(
  PersonId PersonId,
  ToDoId ToDoId,
  Priority Priority
) : IRequest<Result>;

public sealed class SetToDoPriorityCommandHandler(IToDoStore toDoStore) : IRequestHandler<SetToDoPriorityCommand, Result>
{
  public async Task<Result> Handle(SetToDoPriorityCommand request, CancellationToken cancellationToken)
  {
    try
    {
      var ownershipResult = await VerifyOwnershipAsync(request.ToDoId, request.PersonId, cancellationToken);
      return ownershipResult.IsFailed
        ? ownershipResult
        : await toDoStore.UpdatePriorityAsync(request.ToDoId, request.Priority, cancellationToken);
    }
    catch (Exception ex)
    {
      return Result.Fail(ex.Message);
    }
  }

  private async Task<Result> VerifyOwnershipAsync(ToDoId toDoId, PersonId personId, CancellationToken cancellationToken)
  {
    var todoResult = await toDoStore.GetAsync(toDoId, cancellationToken);
    return todoResult switch
    {
      { IsFailed: true } => Result.Fail("To-Do not found"),
      { Value.PersonId.Value: var ownerId } when ownerId != personId.Value =>
        Result.Fail("You don't have permission to update this to-do"),
      _ => Result.Ok(),
    };
  }
}
