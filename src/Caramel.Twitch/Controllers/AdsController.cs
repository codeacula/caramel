using System.Text;

using Caramel.Twitch.Auth;

using Microsoft.AspNetCore.Mvc;

namespace Caramel.Twitch.Controllers;

[ApiController]
[Route("twitch/ads")]
public sealed class AdsController(
  TwitchTokenManager tokenManager,
  TwitchConfig twitchConfig,
  ITwitchSetupState setupState,
  IHttpClientFactory httpClientFactory,
  ILogger<AdsController> logger) : ControllerBase
{
  private static readonly int[] ValidDurations = [30, 60, 90, 120, 150, 180];

  [HttpPost("run")]
  public async Task<IActionResult> RunAdsAsync([FromBody] RunAdsRequest request, CancellationToken cancellationToken)
  {
    if (!ValidDurations.Contains(request.Duration))
    {
      return BadRequest($"Duration must be one of: {string.Join(", ", ValidDurations)}");
    }

    var setup = setupState.Current;
    if (setup is null)
    {
      AdsControllerLogs.SetupNotConfigured(logger);
      return StatusCode(503, "Twitch setup has not been completed. Visit /twitch/setup to configure.");
    }

    var broadcasterId = setup.Channels.Count > 0 ? setup.Channels[0].UserId : null;
    if (broadcasterId is null)
    {
      AdsControllerLogs.NoBroadcasterConfigured(logger);
      return BadRequest("No channels are configured.");
    }

    try
    {
      var accessToken = await tokenManager.GetValidAccessTokenAsync(cancellationToken);

      using var httpClient = httpClientFactory.CreateClient("TwitchHelix");
      httpClient.DefaultRequestHeaders.Add("Client-Id", twitchConfig.ClientId);
      httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {accessToken}");

      var body = new
      {
        duration = request.Duration,
      };

      var json = JsonSerializer.Serialize(body);
      using var content = new StringContent(json, Encoding.UTF8, "application/json");
      var response = await httpClient.PostAsync($"https://api.twitch.tv/helix/channels/{broadcasterId}/ads", content, cancellationToken);

      if (response.IsSuccessStatusCode)
      {
        AdsControllerLogs.AdsRun(logger, broadcasterId, request.Duration);
        return Ok(new { message = $"Ads started for {request.Duration} seconds" });
      }

      var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
      AdsControllerLogs.AdsRunFailed(logger, (int)response.StatusCode, errorBody);
      return Problem("Twitch API rejected the request.");
    }
    catch (Exception ex)
    {
      AdsControllerLogs.AdsRunError(logger, ex.Message);
      return Problem("An internal error occurred while running ads.");
    }
  }
}

public sealed record RunAdsRequest
{
  public required int Duration { get; init; }
}

internal static partial class AdsControllerLogs
{
  [LoggerMessage(Level = LogLevel.Warning, Message = "Cannot run ads: Twitch setup has not been completed")]
  public static partial void SetupNotConfigured(ILogger logger);

  [LoggerMessage(Level = LogLevel.Warning, Message = "Cannot run ads: no broadcaster channel IDs are configured")]
  public static partial void NoBroadcasterConfigured(ILogger logger);

  [LoggerMessage(Level = LogLevel.Information, Message = "Ads started on channel {ChannelId} for {Duration} seconds")]
  public static partial void AdsRun(ILogger logger, string channelId, int duration);

  [LoggerMessage(Level = LogLevel.Warning, Message = "Twitch API rejected ads request with status {StatusCode}: {Error}")]
  public static partial void AdsRunFailed(ILogger logger, int statusCode, string error);

  [LoggerMessage(Level = LogLevel.Error, Message = "Error running ads: {Error}")]
  public static partial void AdsRunError(ILogger logger, string error);
}
