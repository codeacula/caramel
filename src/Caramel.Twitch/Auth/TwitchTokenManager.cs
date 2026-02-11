using TwitchLib.Api;

namespace Caramel.Twitch.Auth;

/// <summary>
/// Manages the bot's OAuth tokens (access and refresh).
/// Handles automatic token refresh when expiry approaches.
/// </summary>
public sealed class TwitchTokenManager
{
  private readonly TwitchConfig _config;
  private readonly object _lock = new();
  private string _accessToken;
  private string? _refreshToken;
  private DateTime _expiresAt = DateTime.MinValue;
  private const int RefreshThresholdSeconds = 300; // Refresh if expires within 5 minutes

  public TwitchTokenManager(TwitchConfig config)
  {
    _config = config;
    _accessToken = config.AccessToken;
    _refreshToken = config.RefreshToken;
    // Assume initial token from config is valid for 1 hour
    _expiresAt = DateTime.UtcNow.AddHours(1);
  }

  /// <summary>
  /// Gets a valid access token, refreshing if necessary.
  /// </summary>
  public async Task<string> GetValidAccessTokenAsync(CancellationToken cancellationToken = default)
  {
    lock (_lock)
    {
      if (_accessToken != null && DateTime.UtcNow.AddSeconds(RefreshThresholdSeconds) < _expiresAt)
      {
        return _accessToken;
      }
    }

    if (_refreshToken == null)
    {
      throw new InvalidOperationException(
        "Cannot refresh token: no refresh token available. Complete the OAuth flow via GET /auth/login");
    }

    await RefreshAccessTokenAsync(cancellationToken);
    lock (_lock)
    {
      return _accessToken;
    }
  }

  /// <summary>
  /// Updates the tokens after a successful OAuth exchange (authorization code flow or refresh).
  /// Sets expiry time based on Twitch's typical 1-hour expiration.
  /// </summary>
  public void SetTokens(string accessToken, string? refreshToken = null, int expiresInSeconds = 3600)
  {
    lock (_lock)
    {
      _accessToken = accessToken;
      if (refreshToken != null)
      {
        _refreshToken = refreshToken;
      }

      _expiresAt = DateTime.UtcNow.AddSeconds(expiresInSeconds);
    }
  }

  /// <summary>
  /// Refreshes the access token using the refresh token.
  /// Throws if refresh fails (invalid refresh token, network error, etc).
  /// </summary>
  private async Task RefreshAccessTokenAsync(CancellationToken cancellationToken)
  {
    var httpClient = new HttpClient();
    var requestBody = new FormUrlEncodedContent(new[]
    {
      new KeyValuePair<string, string>("client_id", _config.ClientId),
      new KeyValuePair<string, string>("client_secret", _config.ClientSecret),
      new KeyValuePair<string, string>("grant_type", "refresh_token"),
      new KeyValuePair<string, string>("refresh_token", _refreshToken ?? string.Empty),
    });

    var response = await httpClient.PostAsync(
      "https://id.twitch.tv/oauth2/token",
      requestBody,
      cancellationToken);

    if (!response.IsSuccessStatusCode)
    {
      throw new InvalidOperationException(
        $"OAuth token refresh failed with status {response.StatusCode}: {await response.Content.ReadAsStringAsync(cancellationToken)}");
    }

    var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
    var json = System.Text.Json.JsonDocument.Parse(responseContent);
    var root = json.RootElement;

    var accessToken = root.GetProperty("access_token").GetString() ?? throw new InvalidOperationException("Missing access_token in response");
    var expiresIn = root.GetProperty("expires_in").GetInt32();
    var refreshToken = root.TryGetProperty("refresh_token", out var rtElement)
      ? rtElement.GetString()
      : null;

    SetTokens(accessToken, refreshToken, expiresIn);
  }

  /// <summary>
  /// Returns the current access token without validation (for read-only checks).
  /// </summary>
  public string GetCurrentAccessToken()
  {
    lock (_lock)
    {
      return _accessToken;
    }
  }

  /// <summary>
  /// Returns true if a refresh token exists and tokens can be refreshed.
  /// </summary>
  public bool CanRefresh()
  {
    lock (_lock)
    {
      return _refreshToken != null;
    }
  }
}
