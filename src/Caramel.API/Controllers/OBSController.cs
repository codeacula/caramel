using Caramel.Core.API;

using Microsoft.AspNetCore.Mvc;

namespace Caramel.API.Controllers;

[ApiController]
[Route("api/obs")]
public sealed class OBSController(IOBSServiceClient caramelClient) : ControllerBase
{
  [HttpGet("status")]
  public async Task<IActionResult> GetStatusAsync(CancellationToken cancellationToken)
  {
    var result = await caramelClient.GetOBSStatusAsync(cancellationToken);
    return result.IsFailed
      ? StatusCode(StatusCodes.Status503ServiceUnavailable, result.Errors.Select(e => e.Message))
      : Ok(result.Value);
  }

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

public sealed record SetSceneRequest
{
  public required string SceneName { get; init; }
}
