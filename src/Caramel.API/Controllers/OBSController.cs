using Caramel.Core.API;

using Microsoft.AspNetCore.Mvc;

namespace Caramel.API.Controllers;

/// <summary>
/// API controller for OBS (Open Broadcaster Software) integration and control.
/// Provides endpoints to query OBS status and control scenes.
/// </summary>
[ApiController]
[Route("api/obs")]
public sealed class OBSController(IOBSServiceClient caramelClient) : ControllerBase
{
  /// <summary>
  /// Gets the current status of the connected OBS instance.
  /// </summary>
  /// <param name="cancellationToken">Cancellation token for async operation.</param>
  /// <returns>HTTP 200 with OBS status if successful; HTTP 503 if OBS is unavailable.</returns>
  [HttpGet("status")]
  public async Task<IActionResult> GetStatusAsync(CancellationToken cancellationToken)
  {
    var result = await caramelClient.GetOBSStatusAsync(cancellationToken);
    return result.IsFailed
      ? StatusCode(StatusCodes.Status503ServiceUnavailable, result.Errors.Select(e => e.Message))
      : Ok(result.Value);
  }

  /// <summary>
  /// Sets the active scene in OBS by name.
  /// </summary>
  /// <param name="request">The request containing the scene name to activate.</param>
  /// <param name="cancellationToken">Cancellation token for async operation.</param>
  /// <returns>HTTP 200 with the result if successful; HTTP 502 if OBS communication failed.</returns>
  [HttpPost("scene")]
  public async Task<IActionResult> SetSceneAsync([FromBody] SetSceneRequest request, CancellationToken cancellationToken)
  {
    var result = await caramelClient.SetOBSSceneAsync(request.SceneName, cancellationToken);

    return result switch
    {
      { IsFailed: true } => StatusCode(StatusCodes.Status502BadGateway, result.Errors.Select(e => e.Message)),
      _ => Ok(result.Value)
    };
  }
}

/// <summary>
/// Request to set the active OBS scene.
/// </summary>
public sealed record SetSceneRequest
{
  /// <summary>
  /// Gets the name of the scene to activate.
  /// </summary>
  public required string SceneName { get; init; }
}
