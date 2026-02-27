using Caramel.Domain.Twitch;

using FluentResults;

namespace Caramel.Core.Twitch;

public interface ITwitchSetupStore
{
  Task<Result<TwitchSetup?>> GetAsync(CancellationToken cancellationToken = default);
  Task<Result<TwitchSetup>> SaveAsync(
    TwitchSetup setup,
    CancellationToken cancellationToken = default);

  /// <summary>
  /// Persists bot account tokens to the database with encryption.
  /// </summary>
  Task<Result<TwitchSetup>> SaveBotTokensAsync(
    TwitchAccountTokens tokens,
    CancellationToken cancellationToken = default);

  /// <summary>
  /// Persists broadcaster account tokens to the database with encryption.
  /// </summary>
  Task<Result<TwitchSetup>> SaveBroadcasterTokensAsync(
    TwitchAccountTokens tokens,
    CancellationToken cancellationToken = default);
}
