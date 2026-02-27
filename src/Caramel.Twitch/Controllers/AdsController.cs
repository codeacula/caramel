using System.Text;

using Caramel.Twitch.Auth;

using Microsoft.AspNetCore.Mvc;

namespace Caramel.Twitch.Controllers;

[ApiController]
[Route("twitch/ads")]
public sealed class AdsController(
  ITwitchTokenManager tokenManager,
  TwitchConfig twitchConfig,
  ITwitchSetupState setupState,
  IHttpClientFactory httpClientFactory,
  IAdsCoordinator adsCoordinator,
  ILogger<AdsController> logger) : ControllerBase
{
  private static readonly int[] ValidDurations = [30, 60, 90, 120, 150, 180];

  [HttpPost("run")]
  public async Task<IActionResult> RunAdsAsync([FromBody] RunAdsRequest request, CancellationToken cancellationToken)
  {
    if (adsCoordinator.IsOnCooldown())
    {
      return StatusCode(409, new { message = "Ads are on cooldown." });
    }

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
        broadcaster_id = broadcasterId,
        length = request.Duration,
      };

      var json = JsonSerializer.Serialize(body);
      using var content = new StringContent(json, Encoding.UTF8, "application/json");
      var response = await httpClient.PostAsync("https://api.twitch.tv/helix/channels/commercial", content, cancellationToken);

      if (response.IsSuccessStatusCode)
      {
        AdsControllerLogs.AdsRun(logger, broadcasterId, request.Duration);

        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
        var retryAfter = 0;
        using var json2 = JsonDocument.Parse(responseBody);
        var dataArray = json2.RootElement.GetProperty("data");
        if (dataArray.GetArrayLength() > 0 &&
            dataArray[0].TryGetProperty("retry_after", out var retryAfterEl))
        {
          retryAfter = retryAfterEl.TryGetInt32(out var parsed) ? parsed : 0;
        }

        return Ok(new { message = $"Ads started for {request.Duration} seconds", retry_after = retryAfter });
      }

      var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
      AdsControllerLogs.AdsRunFailed(logger, (int)response.StatusCode, errorBody);

      return response.StatusCode switch
      {
        System.Net.HttpStatusCode.BadRequest => Problem("Invalid request to Twitch API. Please check the request parameters."),
        System.Net.HttpStatusCode.Unauthorized => Problem("Unauthorized: Twitch rejected the access token. Please re-authorize via /auth/twitch/login."),
        System.Net.HttpStatusCode.Forbidden => Problem("Twitch rejected the request: missing required scope 'channel:edit:commercial'. Please re-authorize via /auth/twitch/login."),
        _ => Problem("Twitch API rejected the request.")
      };
    }
    catch (OperationCanceledException)
    {
      throw;
    }
    catch (HttpRequestException ex)
    {
      AdsControllerLogs.AdsRunError(logger, $"Network error: {ex.Message}");
      return Problem("Network error while running ads.");
    }
    catch (InvalidOperationException ex)
    {
      AdsControllerLogs.AdsRunError(logger, $"Invalid state: {ex.Message}");
      return Problem("Invalid operation state while running ads.");
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
