using Caramel.Core.Twitch;
using Caramel.Domain.Twitch;

using FluentResults;

namespace Caramel.Application.Twitch;

/// <summary>
/// Command to add or update the broadcaster account OAuth token for an existing Twitch setup.
/// </summary>
public sealed record LinkBroadcasterTokenCommand : IRequest<Result<TwitchSetup>>
{
  /// <summary>
  /// Gets the numeric Twitch user ID of the broadcaster account.
  /// </summary>
  public required string BroadcasterUserId { get; init; }

  /// <summary>
  /// Gets the login name (username) of the broadcaster account.
  /// </summary>
  public required string BroadcasterLogin { get; init; }

  /// <summary>
  /// Gets the OAuth access token for the broadcaster account.
  /// </summary>
  public required string AccessToken { get; init; }

  /// <summary>
  /// Gets the OAuth refresh token for the broadcaster account, if available.
  /// </summary>
  public string? RefreshToken { get; init; }

  /// <summary>
  /// Gets the expiry time (UTC) of the access token.
  /// </summary>
  public required DateTime ExpiresAt { get; init; }
}

/// <summary>
/// Handles the execution of LinkBroadcasterTokenCommand requests.
/// </summary>
public sealed class LinkBroadcasterTokenCommandHandler(ITwitchSetupStore store) : IRequestHandler<LinkBroadcasterTokenCommand, Result<TwitchSetup>>
{
  /// <summary>
  /// Handles the command to add or update broadcaster tokens for an existing Twitch setup.
  /// </summary>
  /// <param name="request">The link broadcaster token command.</param>
  /// <param name="cancellationToken">Cancellation token for async operation.</param>
  /// <returns>A Result containing the updated TwitchSetup, or an error if the operation failed.</returns>
  public async Task<Result<TwitchSetup>> Handle(LinkBroadcasterTokenCommand request, CancellationToken cancellationToken)
  {
    try
    {
      // Get the existing setup
      var getResult = await store.GetAsync(cancellationToken);

      if (getResult.IsFailed)
      {
        return Result.Fail<TwitchSetup>("Unable to retrieve existing Twitch setup");
      }

      if (getResult.Value is null)
      {
        return Result.Fail<TwitchSetup>("Twitch setup not configured. Please configure the bot setup first.");
      }

      var existingSetup = getResult.Value;

      // Create broadcaster tokens
      var broadcasterTokens = new TwitchAccountTokens
      {
        UserId = request.BroadcasterUserId,
        Login = request.BroadcasterLogin,
        AccessToken = request.AccessToken,
        RefreshToken = request.RefreshToken,
        ExpiresAt = request.ExpiresAt,
        LastRefreshedOn = DateTimeOffset.UtcNow,
      };

      // Update the setup with broadcaster tokens
      var updatedSetup = existingSetup with
      {
        BroadcasterTokens = broadcasterTokens,
        UpdatedOn = DateTimeOffset.UtcNow,
      };

      // Save the updated setup
      return await store.SaveAsync(updatedSetup, cancellationToken);
    }
    catch (OperationCanceledException)
    {
      throw;
    }
    catch (InvalidOperationException ex)
    {
      return Result.Fail<TwitchSetup>($"Invalid operation state linking broadcaster token: {ex.Message}");
    }
    catch (Exception ex)
    {
      return Result.Fail<TwitchSetup>($"Unexpected error linking broadcaster token: {ex.Message}");
    }
  }
}
