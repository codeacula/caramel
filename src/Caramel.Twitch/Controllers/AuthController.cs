using Caramel.Twitch.Auth;

using Microsoft.AspNetCore.Mvc;

namespace Caramel.Twitch.Controllers;

[ApiController]
[Route("/auth/twitch")]
public sealed class AuthController(
  ITwitchChatBroadcaster broadcaster,
  IHttpClientFactory httpClientFactory,
  ILogger<AuthController> logger,
  ICaramelServiceClient serviceClient,
  ITwitchSetupState setupState,
  OAuthStateManager stateManager,
  TwitchTokenManager tokenManager,
  TwitchConfig twitchConfig,
  ITwitchUserResolver userResolver
) : ControllerBase
{

  private const string _scopes = "chat:read chat:edit whispers:read whispers:edit moderator:manage:banned_users moderator:manage:chat_messages channel:moderate user:bot user:read:chat user:write:chat user:manage:whispers channel:read:redemptions channel:edit:commercial";

  [Route("callback")]
  [HttpGet]
  public async Task<IResult> CallbackAsync([FromQuery] string code, [FromQuery] string state, CancellationToken ct)
  {
    try
    {
      if (!stateManager.ValidateAndConsumeState(state))
      {
        CaramelTwitchProgramLogs.OAuthStateMismatch(logger);
        return Results.BadRequest("Invalid or expired state parameter");
      }

      using var httpClient = httpClientFactory.CreateClient("TwitchHelix");
      var tokenRequest = new FormUrlEncodedContent(
      [
        new KeyValuePair<string, string>("client_id", twitchConfig.ClientId),
        new KeyValuePair<string, string>("client_secret", twitchConfig.ClientSecret),
        new KeyValuePair<string, string>("code", code),
        new KeyValuePair<string, string>("grant_type", "authorization_code"),
        new KeyValuePair<string, string>("redirect_uri", twitchConfig.OAuthCallbackUrl),
      ]);

      var tokenResponse = await httpClient.PostAsync("https://id.twitch.tv/oauth2/token", tokenRequest, ct);
      if (!tokenResponse.IsSuccessStatusCode)
      {
        var error = await tokenResponse.Content.ReadAsStringAsync(ct);
        CaramelTwitchProgramLogs.OAuthTokenExchangeFailed(logger, (int)tokenResponse.StatusCode, error);
        return Results.BadRequest($"Token exchange failed: {error}");
      }

      var responseContent = await tokenResponse.Content.ReadAsStringAsync(ct);
      var json = JsonDocument.Parse(responseContent);
      var root = json.RootElement;

      var accessToken = root.GetProperty("access_token").GetString() ?? throw new InvalidOperationException("Missing access_token");
      var expiresIn = root.GetProperty("expires_in").GetInt32();
      var refreshToken = root.TryGetProperty("refresh_token", out var rtElement) ? rtElement.GetString() : null;

      tokenManager.SetTokens(accessToken, refreshToken, expiresIn);
      CaramelTwitchProgramLogs.OAuthSucceeded(logger);

      // Resolve the authorized user's identity and auto-configure setup (bot = user, channel = their own channel)
      try
      {
        var (userId, login) = await userResolver.ResolveCurrentUserAsync(ct);
        var setup = new TwitchSetup
        {
          BotUserId = userId,
          BotLogin = login,
          Channels = [new TwitchChannel { UserId = userId, Login = login }],
          ConfiguredOn = DateTimeOffset.UtcNow,
          UpdatedOn = DateTimeOffset.UtcNow,
        };

        var saveResult = await serviceClient.SaveTwitchSetupAsync(setup, ct);
        if (saveResult.IsSuccess)
        {
          setupState.Update(saveResult.Value);
          CaramelTwitchProgramLogs.OAuthSetupConfigured(logger, login);
          await broadcaster.PublishSystemMessageAsync("setup_status", new { configured = true }, ct);
        }
        else
        {
          CaramelTwitchProgramLogs.OAuthSetupFailed(logger, string.Join("; ", saveResult.Errors.Select(e => e.Message)));
        }
      }
      catch (Exception ex)
      {
        CaramelTwitchProgramLogs.OAuthSetupFailed(logger, ex.Message);
      }

      // Push auth_status notification to all connected WebSocket clients
      await broadcaster.PublishSystemMessageAsync("auth_status", new { authorized = true }, ct);

      return Results.Content("""
      <!doctype html>
      <html><head><title>Caramel - Authorized</title></head>
      <body style="font-family:sans-serif;text-align:center;padding:4rem">
        <h1>Twitch authorized successfully</h1>
        <p>You can close this window and return to Caramel.</p>
      </body></html>
      """, "text/html");
    }
    catch (Exception ex)
    {
      CaramelTwitchProgramLogs.OAuthCallbackError(logger, ex.Message);
      return Results.StatusCode(500);
    }
  }

  [Route("login")]
  [HttpGet]
  public IResult Login()
  {
    var state = stateManager.GenerateState();
    var oauthUrl = "https://id.twitch.tv/oauth2/authorize?" +
      $"client_id={Uri.EscapeDataString(twitchConfig.ClientId)}&" +
      $"redirect_uri={Uri.EscapeDataString(twitchConfig.OAuthCallbackUrl)}&" +
      "response_type=code&" +
      $"scope={Uri.EscapeDataString(_scopes)}&" +
      $"state={Uri.EscapeDataString(state)}";

    return Results.Redirect(oauthUrl);
  }

  [Route("status")]
  [HttpGet]
  public IResult Status()
  {
    var hasToken = tokenManager.CanRefresh() || tokenManager.GetCurrentAccessToken() is { Length: > 0 };
    return Results.Ok(new { authorized = hasToken });
  }
}
