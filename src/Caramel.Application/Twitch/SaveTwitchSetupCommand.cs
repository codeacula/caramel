using Caramel.Core.Twitch;
using Caramel.Domain.Twitch;

using FluentResults;

namespace Caramel.Application.Twitch;

/// <summary>
/// Saves or updates the Twitch bot and channel configuration.
/// </summary>
public sealed record SaveTwitchSetupCommand : IRequest<Result<TwitchSetup>>
{
  /// <summary>
  /// Gets the numeric Twitch user ID of the bot account.
  /// </summary>
  public required string BotUserId { get; init; }

  /// <summary>
  /// Gets the login name (username) of the bot account.
  /// </summary>
  public required string BotLogin { get; init; }

  /// <summary>
  /// Gets the list of Twitch channels to configure, each with a user ID and login name.
  /// </summary>
  public required IReadOnlyList<(string UserId, string Login)> Channels { get; init; }
}

/// <summary>
/// Handles the execution of SaveTwitchSetupCommand requests.
/// </summary>
public sealed class SaveTwitchSetupCommandHandler(ITwitchSetupStore store) : IRequestHandler<SaveTwitchSetupCommand, Result<TwitchSetup>>
{
  /// <summary>
  /// Handles the command to save or update the Twitch configuration.
  /// </summary>
  /// <param name="request">The save command containing bot and channel details.</param>
  /// <param name="cancellationToken">Cancellation token for async operation.</param>
  /// <returns>A Result containing the saved TwitchSetup, or an error if the operation failed.</returns>
  public async Task<Result<TwitchSetup>> Handle(SaveTwitchSetupCommand request, CancellationToken cancellationToken)
  {
    try
    {
      var now = DateTimeOffset.UtcNow;

      var setup = new TwitchSetup
      {
        BotUserId = request.BotUserId,
        BotLogin = request.BotLogin,
        Channels = [.. request.Channels.Select(c => new TwitchChannel { UserId = c.UserId, Login = c.Login })],
        ConfiguredOn = now,
        UpdatedOn = now,
      };

      return await store.SaveAsync(setup, cancellationToken);
    }
    catch (OperationCanceledException)
    {
      throw;
    }
    catch (InvalidOperationException ex)
    {
      return Result.Fail<TwitchSetup>($"Invalid operation state saving Twitch setup: {ex.Message}");
    }
    catch (Exception ex)
    {
      return Result.Fail<TwitchSetup>($"Unexpected error saving Twitch setup: {ex.Message}");
    }
  }
}
