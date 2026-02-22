using Caramel.Core.API;
using Caramel.Domain.Twitch;
using Caramel.Twitch.Services;

using Microsoft.AspNetCore.Mvc;

namespace Caramel.Twitch.Controllers;

/// <summary>
/// Request body for <see cref="TwitchSetupController.SaveSetupAsync"/>.
/// </summary>
public sealed record SaveTwitchSetupRequest(string BotLogin, IReadOnlyList<string> ChannelLogins);

/// <summary>
/// Response body for <see cref="TwitchSetupController.GetSetupAsync"/>.
/// </summary>
public sealed record TwitchSetupStatusResponse(bool IsConfigured, string? BotLogin, IReadOnlyList<string>? ChannelLogins);

[ApiController]
[Route("twitch/setup")]
public sealed class TwitchSetupController(
  ITwitchSetupState setupState,
  ITwitchUserResolver userResolver,
  ICaramelServiceClient serviceClient,
  ITwitchChatBroadcaster broadcaster,
  ILogger<TwitchSetupController> logger) : ControllerBase
{
  /// <summary>
  /// Returns whether Twitch bot + channel setup is configured.
  /// </summary>
  [HttpGet]
  public IActionResult GetSetupAsync()
  {
    var current = setupState.Current;

    if (current is null)
    {
      return Ok(new TwitchSetupStatusResponse(false, null, null));
    }

    var channelLogins = current.Channels.Select(c => c.Login).ToList();
    return Ok(new TwitchSetupStatusResponse(true, current.BotLogin, channelLogins));
  }

  /// <summary>
  /// Resolves the provided login names to Twitch user IDs via Helix, persists
  /// the configuration via gRPC, and pushes a setup_status WebSocket notification.
  /// </summary>
  [HttpPost]
  public async Task<IActionResult> SaveSetupAsync(
    [FromBody] SaveTwitchSetupRequest request,
    CancellationToken cancellationToken)
  {
    if (string.IsNullOrWhiteSpace(request.BotLogin))
    {
      return BadRequest("BotLogin is required.");
    }

    if (request.ChannelLogins is not { Count: > 0 })
    {
      return BadRequest("At least one channel login is required.");
    }

    try
    {
      // Resolve all logins → numeric IDs in parallel
      var botUserIdTask = userResolver.ResolveUserIdAsync(request.BotLogin, cancellationToken);
      var channelLoginsList = request.ChannelLogins.ToList();
      var channelIdsTask = userResolver.ResolveUserIdsAsync(channelLoginsList, cancellationToken);

      await Task.WhenAll(botUserIdTask, channelIdsTask);

      var botUserId = botUserIdTask.Result;
      var channelIds = channelIdsTask.Result;

      if (channelIds.Count != channelLoginsList.Count)
      {
        return BadRequest("One or more channel logins could not be resolved.");
      }

      var channels = channelLoginsList
        .Zip(channelIds, (login, id) => new TwitchChannel { UserId = id, Login = login })
        .ToList();

      var setup = new TwitchSetup
      {
        BotUserId = botUserId,
        BotLogin = request.BotLogin,
        Channels = channels,
        ConfiguredOn = DateTimeOffset.UtcNow,
        UpdatedOn = DateTimeOffset.UtcNow,
      };

      var saveResult = await serviceClient.SaveTwitchSetupAsync(setup, cancellationToken);
      if (saveResult.IsFailed)
      {
        TwitchSetupControllerLogs.SaveFailed(logger, string.Join("; ", saveResult.Errors.Select(e => e.Message)));
        return Problem("Failed to persist Twitch setup.");
      }

      // Update in-memory state
      setupState.Update(saveResult.Value);

      // Notify connected WebSocket clients that setup is now complete
      await broadcaster.PublishSystemMessageAsync("setup_status", new { configured = true }, cancellationToken);

      TwitchSetupControllerLogs.SetupSaved(logger, request.BotLogin, channels.Count);
      return Ok(new TwitchSetupStatusResponse(true, saveResult.Value.BotLogin,
        saveResult.Value.Channels.Select(c => c.Login).ToList()));
    }
    catch (InvalidOperationException ex)
    {
      TwitchSetupControllerLogs.ResolveError(logger, ex.Message);
      return BadRequest($"Failed to resolve Twitch user: {ex.Message}");
    }
    catch (Exception ex)
    {
      TwitchSetupControllerLogs.UnexpectedError(logger, ex.Message);
      return Problem("An unexpected error occurred while saving the setup.");
    }
  }
}

internal static partial class TwitchSetupControllerLogs
{
  [LoggerMessage(Level = LogLevel.Error, Message = "Failed to save Twitch setup: {Error}")]
  public static partial void SaveFailed(ILogger logger, string error);

  [LoggerMessage(Level = LogLevel.Information, Message = "Twitch setup saved: bot={BotLogin}, channels={ChannelCount}")]
  public static partial void SetupSaved(ILogger logger, string botLogin, int channelCount);

  [LoggerMessage(Level = LogLevel.Warning, Message = "Failed to resolve Twitch user login: {Error}")]
  public static partial void ResolveError(ILogger logger, string error);

  [LoggerMessage(Level = LogLevel.Error, Message = "Unexpected error in Twitch setup: {Error}")]
  public static partial void UnexpectedError(ILogger logger, string error);
}
