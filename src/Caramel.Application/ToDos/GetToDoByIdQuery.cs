using Caramel.Core.ToDos;
using Caramel.Domain.ToDos.Models;
using Caramel.Domain.ToDos.ValueObjects;

using FluentResults;

namespace Caramel.Application.ToDos;

public sealed record GetToDoByIdQuery(ToDoId ToDoId) : IRequest<Result<ToDo>>;

public sealed class GetToDoByIdQueryHandler(IToDoStore toDoStore) : IRequestHandler<GetToDoByIdQuery, Result<ToDo>>
{
  public async Task<Result<ToDo>> Handle(GetToDoByIdQuery request, CancellationToken cancellationToken)
  {
    try
    {
      return await toDoStore.GetAsync(request.ToDoId, cancellationToken);
    }
    catch (Exception ex)
    {
      return Result.Fail(ex.Message);
    }
  }
}
