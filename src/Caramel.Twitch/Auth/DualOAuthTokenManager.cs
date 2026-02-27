using Caramel.Core.Security;
using Caramel.Core.Twitch;
using Caramel.Domain.Twitch;

namespace Caramel.Twitch.Auth;

/// <summary>
/// Manages OAuth tokens for dual accounts (bot and broadcaster).
/// Loads tokens from database on initialization, handles refresh, and persists updates back to database.
/// </summary>
public sealed class DualOAuthTokenManager(
  ITwitchSetupStore setupStore,
  ITokenEncryptionService encryptionService,
  IHttpClientFactory httpClientFactory,
  TwitchConfig twitchConfig,
  ILogger<DualOAuthTokenManager> logger
) : IDualOAuthTokenManager
{
  private readonly Lock _botLock = new();
  private readonly Lock _broadcasterLock = new();
  private string? _botAccessToken;
  private string? _botRefreshToken;
  private DateTime _botExpiresAt = DateTime.MinValue;
  private string? _broadcasterAccessToken;
  private string? _broadcasterRefreshToken;
  private DateTime _broadcasterExpiresAt = DateTime.MinValue;
  private const int RefreshThresholdSeconds = 300;

  /// <summary>
  /// Initializes the token manager by loading tokens from the database.
  /// </summary>
  public async Task InitializeAsync(CancellationToken cancellationToken = default)
  {
    try
    {
      var setupResult = await setupStore.GetAsync(cancellationToken);
      if (!setupResult.IsSuccess || setupResult.Value == null)
      {
        DualOAuthTokenManagerLogs.InitializationNoSetup(logger);
        return;
      }

      var setup = setupResult.Value;

      // Load bot tokens
      if (setup.BotTokens != null)
      {
        lock (_botLock)
        {
          _botAccessToken = setup.BotTokens.AccessToken;
          _botRefreshToken = setup.BotTokens.RefreshToken;
          // Treat loaded tokens as potentially stale, refresh on first use
          _botExpiresAt = DateTime.MinValue;
        }
        DualOAuthTokenManagerLogs.BotTokensLoaded(logger, setup.BotTokens.Login);
      }

      // Load broadcaster tokens
      if (setup.BroadcasterTokens != null)
      {
        lock (_broadcasterLock)
        {
          _broadcasterAccessToken = setup.BroadcasterTokens.AccessToken;
          _broadcasterRefreshToken = setup.BroadcasterTokens.RefreshToken;
          // Treat loaded tokens as potentially stale, refresh on first use
          _broadcasterExpiresAt = DateTime.MinValue;
        }
        DualOAuthTokenManagerLogs.BroadcasterTokensLoaded(logger, setup.BroadcasterTokens.Login);
      }
    }
    catch (OperationCanceledException)
    {
      throw;
    }
    catch (Exception ex)
    {
      DualOAuthTokenManagerLogs.InitializationFailed(logger, ex.Message);
    }
  }

  public async Task<string> GetValidBotTokenAsync(CancellationToken cancellationToken = default)
  {
    lock (_botLock)
    {
      if (!string.IsNullOrWhiteSpace(_botAccessToken) && DateTime.UtcNow.AddSeconds(RefreshThresholdSeconds) < _botExpiresAt)
      {
        return _botAccessToken;
      }
    }

    if (_botRefreshToken == null)
    {
      throw new InvalidOperationException(
        "Cannot refresh bot token: no refresh token available. Complete the OAuth flow via GET /auth/twitch/login/bot");
    }

    await RefreshBotTokenAsync(cancellationToken);
    lock (_botLock)
    {
      return _botAccessToken ?? throw new InvalidOperationException("Bot token refresh failed");
    }
  }

  public async Task<string> GetValidBroadcasterTokenAsync(CancellationToken cancellationToken = default)
  {
    lock (_broadcasterLock)
    {
      if (!string.IsNullOrWhiteSpace(_broadcasterAccessToken) && DateTime.UtcNow.AddSeconds(RefreshThresholdSeconds) < _broadcasterExpiresAt)
      {
        return _broadcasterAccessToken;
      }
    }

    if (_broadcasterRefreshToken == null)
    {
      throw new InvalidOperationException(
        "Cannot refresh broadcaster token: no refresh token available. Complete the OAuth flow via GET /auth/twitch/login/broadcaster");
    }

    await RefreshBroadcasterTokenAsync(cancellationToken);
    lock (_broadcasterLock)
    {
      return _broadcasterAccessToken ?? throw new InvalidOperationException("Broadcaster token refresh failed");
    }
  }

  public async Task SetBotTokensAsync(TwitchAccountTokens tokens, CancellationToken cancellationToken = default)
  {
    lock (_botLock)
    {
      _botAccessToken = tokens.AccessToken;
      _botRefreshToken = tokens.RefreshToken;
      _botExpiresAt = tokens.ExpiresAt;
    }

    // Persist to database
    var saveResult = await setupStore.SaveBotTokensAsync(tokens, cancellationToken);
    if (saveResult.IsSuccess)
    {
      DualOAuthTokenManagerLogs.BotTokensSaved(logger, tokens.Login);
    }
    else
    {
      DualOAuthTokenManagerLogs.BotTokensSaveFailed(logger, string.Join("; ", saveResult.Errors.Select(e => e.Message)));
      throw new InvalidOperationException($"Failed to save bot tokens: {string.Join("; ", saveResult.Errors.Select(e => e.Message))}");
    }
  }

  public async Task SetBroadcasterTokensAsync(TwitchAccountTokens tokens, CancellationToken cancellationToken = default)
  {
    lock (_broadcasterLock)
    {
      _broadcasterAccessToken = tokens.AccessToken;
      _broadcasterRefreshToken = tokens.RefreshToken;
      _broadcasterExpiresAt = tokens.ExpiresAt;
    }

    // Persist to database
    var saveResult = await setupStore.SaveBroadcasterTokensAsync(tokens, cancellationToken);
    if (saveResult.IsSuccess)
    {
      DualOAuthTokenManagerLogs.BroadcasterTokensSaved(logger, tokens.Login);
    }
    else
    {
      DualOAuthTokenManagerLogs.BroadcasterTokensSaveFailed(logger, string.Join("; ", saveResult.Errors.Select(e => e.Message)));
      throw new InvalidOperationException($"Failed to save broadcaster tokens: {string.Join("; ", saveResult.Errors.Select(e => e.Message))}");
    }
  }

  public string? GetCurrentBotAccessToken()
  {
    lock (_botLock)
    {
      return _botAccessToken;
    }
  }

  public string? GetCurrentBroadcasterAccessToken()
  {
    lock (_broadcasterLock)
    {
      return _broadcasterAccessToken;
    }
  }

  public bool CanRefreshBotToken()
  {
    lock (_botLock)
    {
      return _botRefreshToken != null;
    }
  }

  public bool CanRefreshBroadcasterToken()
  {
    lock (_broadcasterLock)
    {
      return _broadcasterRefreshToken != null;
    }
  }

  private async Task RefreshBotTokenAsync(CancellationToken cancellationToken)
  {
    DualOAuthTokenManagerLogs.RefreshingBotToken(logger);
    await RefreshTokenAsync(_botLock, () => _botRefreshToken, (at, rt, ex) =>
    {
      _botAccessToken = at;
      _botRefreshToken = rt;
      _botExpiresAt = ex;
    }, cancellationToken);
  }

  private async Task RefreshBroadcasterTokenAsync(CancellationToken cancellationToken)
  {
    DualOAuthTokenManagerLogs.RefreshingBroadcasterToken(logger);
    await RefreshTokenAsync(_broadcasterLock, () => _broadcasterRefreshToken, (at, rt, ex) =>
    {
      _broadcasterAccessToken = at;
      _broadcasterRefreshToken = rt;
      _broadcasterExpiresAt = ex;
    }, cancellationToken);
  }

  private async Task RefreshTokenAsync(Lock lockObj, Func<string?> getRefreshToken, Action<string, string?, DateTime> updateTokens, CancellationToken cancellationToken)
  {
    var refreshToken = getRefreshToken();
    if (string.IsNullOrWhiteSpace(refreshToken))
    {
      throw new InvalidOperationException("Cannot refresh token: no refresh token available");
    }

    using var httpClient = httpClientFactory.CreateClient("TwitchHelix");
    var requestBody = new FormUrlEncodedContent(
    [
      new KeyValuePair<string, string>("client_id", twitchConfig.ClientId),
      new KeyValuePair<string, string>("client_secret", twitchConfig.ClientSecret),
      new KeyValuePair<string, string>("grant_type", "refresh_token"),
      new KeyValuePair<string, string>("refresh_token", refreshToken),
    ]);

    var response = await httpClient.PostAsync(
      "https://id.twitch.tv/oauth2/token",
      requestBody,
      cancellationToken);

    if (!response.IsSuccessStatusCode)
    {
      var error = await response.Content.ReadAsStringAsync(cancellationToken);
      throw new InvalidOperationException(
        $"OAuth token refresh failed with status {response.StatusCode}: {error}");
    }

    var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
    var json = JsonDocument.Parse(responseContent);
    var root = json.RootElement;

    var accessToken = root.GetProperty("access_token").GetString()
      ?? throw new InvalidOperationException("Missing access_token in response");
    var expiresIn = root.GetProperty("expires_in").GetInt32();
    var newRefreshToken = root.TryGetProperty("refresh_token", out var rtElement)
      ? rtElement.GetString()
      : refreshToken; // Use existing refresh token if not provided

    lock (lockObj)
    {
      updateTokens(accessToken, newRefreshToken, DateTime.UtcNow.AddSeconds(expiresIn));
    }
  }
}

internal static partial class DualOAuthTokenManagerLogs
{
  [LoggerMessage(Level = LogLevel.Information, Message = "Initialized dual token manager, no setup found in database")]
  public static partial void InitializationNoSetup(ILogger logger);

  [LoggerMessage(Level = LogLevel.Information, Message = "Bot tokens loaded from database for user {BotLogin}")]
  public static partial void BotTokensLoaded(ILogger logger, string botLogin);

  [LoggerMessage(Level = LogLevel.Information, Message = "Broadcaster tokens loaded from database for user {BroadcasterLogin}")]
  public static partial void BroadcasterTokensLoaded(ILogger logger, string broadcasterLogin);

  [LoggerMessage(Level = LogLevel.Error, Message = "Failed to initialize dual token manager: {Error}")]
  public static partial void InitializationFailed(ILogger logger, string error);

  [LoggerMessage(Level = LogLevel.Information, Message = "Refreshing bot OAuth access token")]
  public static partial void RefreshingBotToken(ILogger logger);

  [LoggerMessage(Level = LogLevel.Information, Message = "Refreshing broadcaster OAuth access token")]
  public static partial void RefreshingBroadcasterToken(ILogger logger);

  [LoggerMessage(Level = LogLevel.Information, Message = "Bot tokens saved and persisted to database for user {BotLogin}")]
  public static partial void BotTokensSaved(ILogger logger, string botLogin);

  [LoggerMessage(Level = LogLevel.Error, Message = "Failed to save bot tokens to database: {Error}")]
  public static partial void BotTokensSaveFailed(ILogger logger, string error);

  [LoggerMessage(Level = LogLevel.Information, Message = "Broadcaster tokens saved and persisted to database for user {BroadcasterLogin}")]
  public static partial void BroadcasterTokensSaved(ILogger logger, string broadcasterLogin);

  [LoggerMessage(Level = LogLevel.Error, Message = "Failed to save broadcaster tokens to database: {Error}")]
  public static partial void BroadcasterTokensSaveFailed(ILogger logger, string error);
}
