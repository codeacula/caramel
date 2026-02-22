using Caramel.Core.Twitch;
using Caramel.Domain.Twitch;

using FluentResults;

using MediatR;

namespace Caramel.Application.Twitch;

public sealed record GetTwitchSetupQuery : IRequest<Result<TwitchSetup?>>;

public sealed class GetTwitchSetupQueryHandler(ITwitchSetupStore store) : IRequestHandler<GetTwitchSetupQuery, Result<TwitchSetup?>>
{
  public async Task<Result<TwitchSetup?>> Handle(GetTwitchSetupQuery request, CancellationToken cancellationToken)
  {
    try
    {
      return await store.GetAsync(cancellationToken);
    }
    catch (Exception ex)
    {
      return Result.Fail(ex.Message);
    }
  }
}
