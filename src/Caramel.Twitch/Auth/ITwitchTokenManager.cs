namespace Caramel.Twitch.Auth;

/// <summary>
/// Manages the bot's OAuth tokens (access and refresh).
/// </summary>
public interface ITwitchTokenManager
{
  /// <summary>
  /// Returns a valid access token, refreshing it first if it is near expiry.
  /// Throws <see cref="InvalidOperationException"/> when no refresh token is available.
  /// </summary>
  Task<string> GetValidAccessTokenAsync(CancellationToken cancellationToken = default);

  /// <summary>
  /// Stores a new access token (and optionally a new refresh token) with the given expiry.
  /// </summary>
  void SetTokens(string accessToken, string? refreshToken = null, int expiresInSeconds = 3600);

  /// <summary>
  /// Returns the current access token without triggering a refresh.
  /// </summary>
  string GetCurrentAccessToken();

  /// <summary>
  /// Returns <c>true</c> when a refresh token is present and a silent refresh is possible.
  /// </summary>
  bool CanRefresh();
}
