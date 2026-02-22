using Caramel.Core.People;
using Caramel.Domain.People.Models;
using Caramel.Domain.People.ValueObjects;

using FluentResults;

namespace Caramel.Application.People;

public sealed record GetOrCreatePersonByPlatformIdQuery(PlatformId PlatformId) : IRequest<Result<Person>>;

public sealed class GetOrCreatePersonByPlatformIdQueryHandler(IPersonStore personStore)
  : IRequestHandler<GetOrCreatePersonByPlatformIdQuery, Result<Person>>
{
  public async Task<Result<Person>> Handle(GetOrCreatePersonByPlatformIdQuery request, CancellationToken cancellationToken)
  {
    var userResult = await personStore.GetByPlatformIdAsync(request.PlatformId, cancellationToken);
    return userResult.IsSuccess ? userResult : await personStore.CreateByPlatformIdAsync(request.PlatformId, cancellationToken);
  }
}
