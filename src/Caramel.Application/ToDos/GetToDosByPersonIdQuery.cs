using Caramel.Core.ToDos;
using Caramel.Domain.People.ValueObjects;
using Caramel.Domain.ToDos.Models;

using FluentResults;

namespace Caramel.Application.ToDos;

public sealed record GetToDosByPersonIdQuery(PersonId PersonId, bool IncludeCompleted = false) : IRequest<Result<IEnumerable<ToDo>>>;

public sealed class GetToDosByPersonIdQueryHandler(IToDoStore toDoStore) : IRequestHandler<GetToDosByPersonIdQuery, Result<IEnumerable<ToDo>>>
{
  public async Task<Result<IEnumerable<ToDo>>> Handle(GetToDosByPersonIdQuery request, CancellationToken cancellationToken)
  {
    try
    {
      return await toDoStore.GetByPersonIdAsync(request.PersonId, request.IncludeCompleted, cancellationToken);
    }
    catch (Exception ex)
    {
      return Result.Fail(ex.Message);
    }
  }
}
