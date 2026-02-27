using Caramel.Core.Twitch;
using Caramel.Domain.Twitch;

using FluentResults;

namespace Caramel.Application.Twitch;

/// <summary>
/// Retrieves the current Twitch bot and channel configuration.
/// </summary>
public sealed record GetTwitchSetupQuery : IRequest<Result<TwitchSetup?>>;

/// <summary>
/// Handles the execution of GetTwitchSetupQuery requests.
/// </summary>
public sealed class GetTwitchSetupQueryHandler(ITwitchSetupStore store) : IRequestHandler<GetTwitchSetupQuery, Result<TwitchSetup?>>
{
  /// <summary>
  /// Handles the query to retrieve the Twitch setup configuration.
  /// </summary>
  /// <param name="request">The query request.</param>
  /// <param name="cancellationToken">Cancellation token for async operation.</param>
  /// <returns>A Result containing the TwitchSetup if configured, null if not configured, or an error if retrieval failed.</returns>
  public async Task<Result<TwitchSetup?>> Handle(GetTwitchSetupQuery request, CancellationToken cancellationToken)
  {
    try
    {
      return await store.GetAsync(cancellationToken);
    }
    catch (OperationCanceledException)
    {
      throw;
    }
    catch (InvalidOperationException ex)
    {
      return Result.Fail<TwitchSetup?>($"Invalid operation state retrieving Twitch setup: {ex.Message}");
    }
    catch (Exception ex)
    {
      return Result.Fail<TwitchSetup?>($"Unexpected error retrieving Twitch setup: {ex.Message}");
    }
  }
}
