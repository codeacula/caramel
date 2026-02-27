using Caramel.Domain.Twitch;

using FluentResults;

namespace Caramel.Core.Twitch;

public interface ITwitchSetupStore
{
  Task<Result<TwitchSetup?>> GetAsync(CancellationToken cancellationToken = default);
  Task<Result<TwitchSetup>> SaveAsync(
    TwitchSetup setup,
    CancellationToken cancellationToken = default);
}
