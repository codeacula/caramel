using Caramel.Core.People;
using Caramel.Domain.People.Models;
using Caramel.Domain.People.ValueObjects;

using FluentResults;

namespace Caramel.Application.People;

/// <summary>
/// Retrieves an existing person by their platform ID, or creates a new person if they don't exist.
/// </summary>
/// <param name="PlatformId">The platform-specific identifier to look up or create.</param>
public sealed record GetOrCreatePersonByPlatformIdQuery(PlatformId PlatformId) : IRequest<Result<Person>>;

/// <summary>
/// Handles the execution of GetOrCreatePersonByPlatformIdQuery requests.
/// </summary>
public sealed class GetOrCreatePersonByPlatformIdQueryHandler(IPersonStore personStore)
  : IRequestHandler<GetOrCreatePersonByPlatformIdQuery, Result<Person>>
{
  /// <summary>
  /// Handles the query to get or create a person by their platform ID.
  /// </summary>
  /// <param name="request">The query containing the platform ID.</param>
  /// <param name="cancellationToken">Cancellation token for async operation.</param>
  /// <returns>A Result containing the Person if found or created, or an error if the operation failed.</returns>
  public async Task<Result<Person>> Handle(GetOrCreatePersonByPlatformIdQuery request, CancellationToken cancellationToken)
  {
    var userResult = await personStore.GetByPlatformIdAsync(request.PlatformId, cancellationToken);
    return userResult.IsSuccess ? userResult : await personStore.CreateByPlatformIdAsync(request.PlatformId, cancellationToken);
  }
}
