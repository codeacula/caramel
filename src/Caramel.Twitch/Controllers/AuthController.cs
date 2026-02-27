using Caramel.Domain.Twitch;
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
  DualOAuthStateManager stateManager,
  IDualOAuthTokenManager tokenManager,
  TwitchConfig twitchConfig,
  ITwitchUserResolver userResolver
) : ControllerBase
{
  private const string BotScopes = "user:bot user:read:chat user:write:chat user:manage:whispers chat:read chat:edit whispers:read whispers:edit";
  private const string BroadcasterScopes = "channel:read:redemptions channel:edit:commercial moderator:manage:banned_users moderator:manage:chat_messages channel:moderate";

  [Route("callback")]
  [HttpGet]
  public async Task<IResult> CallbackAsync([FromQuery] string code, [FromQuery] string state, CancellationToken ct)
  {
    try
    {
      var accountType = stateManager.ValidateAndConsumeState(state);
      if (accountType == null)
      {
        AuthControllerLogs.OAuthStateMismatch(logger);
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
        AuthControllerLogs.OAuthTokenExchangeFailed(logger, (int)tokenResponse.StatusCode, error);
        return Results.BadRequest($"Token exchange failed: {error}");
      }

      var responseContent = await tokenResponse.Content.ReadAsStringAsync(ct);
      var json = JsonDocument.Parse(responseContent);
      var root = json.RootElement;

      var accessToken = root.GetProperty("access_token").GetString() ?? throw new InvalidOperationException("Missing access_token");
      var expiresIn = root.GetProperty("expires_in").GetInt32();
      var refreshToken = root.TryGetProperty("refresh_token", out var rtElement) ? rtElement.GetString() : null;

      // Resolve the authorized user's identity
      var (userId, login) = await userResolver.ResolveCurrentUserAsync(ct);

      // Create token account object and route to correct account type
      var tokens = new TwitchAccountTokens
      {
        UserId = userId,
        Login = login,
        AccessToken = accessToken,
        RefreshToken = refreshToken,
        ExpiresAt = DateTime.UtcNow.AddSeconds(expiresIn),
        LastRefreshedOn = DateTimeOffset.UtcNow,
      };

      if (accountType == TwitchAccountType.Bot)
      {
        await tokenManager.SetBotTokensAsync(tokens, ct);
        AuthControllerLogs.OAuthSucceeded(logger, "Bot");
      }
      else if (accountType == TwitchAccountType.Broadcaster)
      {
        await tokenManager.SetBroadcasterTokensAsync(tokens, ct);
        AuthControllerLogs.OAuthSucceeded(logger, "Broadcaster");
      }

      // Publish notification
      await broadcaster.PublishSystemMessageAsync("auth_status", new { authorized = true, accountType = accountType.ToString() }, ct);

      return Results.Content("""
       <!doctype html>
       <html><head><title>Caramel - Authorized</title></head>
       <body style="font-family:sans-serif;text-align:center;padding:4rem">
         <h1>Twitch authorized successfully</h1>
         <p>You can close this window and return to Caramel.</p>
       </body></html>
       """, "text/html");
    }
    catch (OperationCanceledException)
    {
      throw;
    }
    catch (HttpRequestException ex)
    {
      AuthControllerLogs.OAuthCallbackError(logger, $"Network error: {ex.Message}");
      return Results.StatusCode(500);
    }
    catch (InvalidOperationException ex)
    {
      AuthControllerLogs.OAuthCallbackError(logger, $"Invalid state: {ex.Message}");
      return Results.StatusCode(500);
    }
    catch (Exception ex)
    {
      AuthControllerLogs.OAuthCallbackError(logger, ex.Message);
      return Results.StatusCode(500);
    }
  }

  /// <summary>
  /// Initiates OAuth flow for bot account with bot-specific scopes.
  /// </summary>
  [Route("login/bot")]
  [HttpGet]
  public IResult LoginBot()
  {
    var state = stateManager.GenerateState(TwitchAccountType.Bot);
    var oauthUrl = "https://id.twitch.tv/oauth2/authorize?" +
      $"client_id={Uri.EscapeDataString(twitchConfig.ClientId)}&" +
      $"redirect_uri={Uri.EscapeDataString(twitchConfig.OAuthCallbackUrl)}&" +
      "response_type=code&" +
      $"scope={Uri.EscapeDataString(BotScopes)}&" +
      $"state={Uri.EscapeDataString(state)}";

    return Results.Redirect(oauthUrl);
  }

  /// <summary>
  /// Initiates OAuth flow for broadcaster account with broadcaster-specific scopes.
  /// </summary>
  [Route("login/broadcaster")]
  [HttpGet]
  public IResult LoginBroadcaster()
  {
    var state = stateManager.GenerateState(TwitchAccountType.Broadcaster);
    var oauthUrl = "https://id.twitch.tv/oauth2/authorize?" +
      $"client_id={Uri.EscapeDataString(twitchConfig.ClientId)}&" +
      $"redirect_uri={Uri.EscapeDataString(twitchConfig.OAuthCallbackUrl)}&" +
      "response_type=code&" +
      $"scope={Uri.EscapeDataString(BroadcasterScopes)}&" +
      $"state={Uri.EscapeDataString(state)}";

    return Results.Redirect(oauthUrl);
  }

  [Route("status")]
  [HttpGet]
  public IResult Status()
  {
    var botToken = tokenManager.GetCurrentBotAccessToken();
    var broadcasterToken = tokenManager.GetCurrentBroadcasterAccessToken();
    
    return Results.Ok(new
    {
      bot = new { authorized = !string.IsNullOrWhiteSpace(botToken), canRefresh = tokenManager.CanRefreshBotToken() },
      broadcaster = new { authorized = !string.IsNullOrWhiteSpace(broadcasterToken), canRefresh = tokenManager.CanRefreshBroadcasterToken() },
    });
  }
}

internal static partial class AuthControllerLogs
{
  [LoggerMessage(Level = LogLevel.Warning, Message = "OAuth state validation failed - state mismatch or expired")]
  public static partial void OAuthStateMismatch(ILogger logger);

  [LoggerMessage(Level = LogLevel.Error, Message = "OAuth token exchange failed with status {StatusCode}: {Error}")]
  public static partial void OAuthTokenExchangeFailed(ILogger logger, int statusCode, string error);

  [LoggerMessage(Level = LogLevel.Information, Message = "OAuth authorization succeeded for {AccountType} account")]
  public static partial void OAuthSucceeded(ILogger logger, string accountType);

  [LoggerMessage(Level = LogLevel.Error, Message = "OAuth callback processing error: {Error}")]
  public static partial void OAuthCallbackError(ILogger logger, string error);
}
