using Microsoft.AspNetCore.Mvc;

namespace Caramel.Twitch.Controllers;

public sealed record TwitchSetupStatusResponse(bool IsConfigured, string? BotLogin, string? ChannelLogin);

[ApiController]
[Route("twitch/setup")]
public sealed class TwitchSetupController(ITwitchSetupState setupState) : ControllerBase
{
  /// <summary>
  /// Returns whether Twitch bot + channel setup is configured.
  /// Setup is automatically applied after completing OAuth via GET /auth/twitch/login.
  /// </summary>
  [HttpGet]
  public IActionResult GetSetup()
  {
    var current = setupState.Current;

    if (current is null)
    {
      return Ok(new TwitchSetupStatusResponse(false, null, null));
    }

    var channel = current.Channels.Count > 0 ? current.Channels[0].Login : null;
    return Ok(new TwitchSetupStatusResponse(true, current.BotLogin, channel));
  }
}
