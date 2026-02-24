namespace Caramel.Twitch.Auth;

/// <summary>
/// Manages the bot's OAuth tokens (access and refresh).
/// Handles automatic token refresh when expiry approaches.
/// </summary>
public sealed class TwitchTokenManager
{
  private readonly TwitchConfig _config;
  private readonly IHttpClientFactory _httpClientFactory;
  private readonly ILogger<TwitchTokenManager> _logger;
  private readonly Lock _lock = new();
  private string _accessToken;
  private string? _refreshToken;
  private DateTime _expiresAt = DateTime.MinValue;
  private const int RefreshThresholdSeconds = 300;

  public TwitchTokenManager(TwitchConfig config, IHttpClientFactory httpClientFactory, ILogger<TwitchTokenManager> logger)
  {
    _config = config;
    _httpClientFactory = httpClientFactory;
    _logger = logger;
    _accessToken = config.AccessToken;
    _refreshToken = config.RefreshToken;

    // If we have an initial token, assume it is valid for 1 hour.
    // Otherwise, set to MinValue to force a refresh attempt/error if refresh token exists,
    // or to signal that OAuth is needed.
    _expiresAt = !string.IsNullOrWhiteSpace(_accessToken)
      ? DateTime.UtcNow.AddHours(1)
      : DateTime.MinValue;
  }

  public async Task<string> GetValidAccessTokenAsync(CancellationToken cancellationToken = default)
  {
    lock (_lock)
    {
      if (!string.IsNullOrWhiteSpace(_accessToken) && DateTime.UtcNow.AddSeconds(RefreshThresholdSeconds) < _expiresAt)
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

    TwitchTokenManagerLogs.TokensUpdated(_logger, expiresInSeconds);
  }

  private async Task RefreshAccessTokenAsync(CancellationToken cancellationToken)
  {
    TwitchTokenManagerLogs.RefreshingToken(_logger);

    using var httpClient = _httpClientFactory.CreateClient("TwitchHelix");
    var requestBody = new FormUrlEncodedContent(
    [
      new KeyValuePair<string, string>("client_id", _config.ClientId),
      new KeyValuePair<string, string>("client_secret", _config.ClientSecret),
      new KeyValuePair<string, string>("grant_type", "refresh_token"),
      new KeyValuePair<string, string>("refresh_token", _refreshToken ?? string.Empty),
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
    var refreshToken = root.TryGetProperty("refresh_token", out var rtElement)
      ? rtElement.GetString()
      : null;

    SetTokens(accessToken, refreshToken, expiresIn);
  }

  public string GetCurrentAccessToken()
  {
    lock (_lock)
    {
      return _accessToken;
    }
  }

  public bool CanRefresh()
  {
    lock (_lock)
    {
      return _refreshToken != null;
    }
  }
}

internal static partial class TwitchTokenManagerLogs
{
  [LoggerMessage(Level = LogLevel.Information, Message = "Refreshing Twitch OAuth access token")]
  public static partial void RefreshingToken(ILogger logger);

  [LoggerMessage(Level = LogLevel.Information, Message = "Twitch tokens updated, expires in {ExpiresInSeconds}s")]
  public static partial void TokensUpdated(ILogger logger, int expiresInSeconds);
}
