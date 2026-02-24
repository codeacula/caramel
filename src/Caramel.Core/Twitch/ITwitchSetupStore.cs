using Caramel.Domain.Twitch;

using FluentResults;

namespace Caramel.Core.Twitch;

/// <summary>
/// Store interface for Twitch setup configuration.
/// </summary>
public interface ITwitchSetupStore
{
  /// <summary>
  /// Retrieves the current Twitch setup configuration, if it exists.
  /// </summary>
  /// <param name="cancellationToken"></param>
  Task<Result<TwitchSetup?>> GetAsync(CancellationToken cancellationToken = default);

  /// <summary>
  /// Saves a new or updated Twitch setup configuration.
  /// </summary>
  /// <param name="setup"></param>
  /// <param name="cancellationToken"></param>
  Task<Result<TwitchSetup>> SaveAsync(
    TwitchSetup setup,
    CancellationToken cancellationToken = default);
}
