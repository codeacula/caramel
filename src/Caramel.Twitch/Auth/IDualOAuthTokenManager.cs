using Caramel.Domain.Twitch;

namespace Caramel.Twitch.Auth;

/// <summary>
/// Manages OAuth tokens for dual accounts (bot and broadcaster).
/// Loads tokens from database on initialization, handles refresh, and persists updates.
/// </summary>
public interface IDualOAuthTokenManager
{
  /// <summary>
  /// Initializes the token manager by loading tokens from the database.
  /// </summary>
  Task InitializeAsync(CancellationToken cancellationToken = default);

  /// <summary>
  /// Returns a valid access token for the bot account, refreshing if near expiry.
  /// Throws <see cref="InvalidOperationException"/> if no bot token is available.
  /// </summary>
  Task<string> GetValidBotTokenAsync(CancellationToken cancellationToken = default);

  /// <summary>
  /// Returns a valid access token for the broadcaster account, refreshing if near expiry.
  /// Throws <see cref="InvalidOperationException"/> if no broadcaster token is available.
  /// </summary>
  Task<string> GetValidBroadcasterTokenAsync(CancellationToken cancellationToken = default);

  /// <summary>
  /// Stores new bot account tokens (loaded from OAuth or refreshed).
  /// </summary>
  Task SetBotTokensAsync(TwitchAccountTokens tokens, CancellationToken cancellationToken = default);

  /// <summary>
  /// Stores new broadcaster account tokens (loaded from OAuth or refreshed).
  /// </summary>
  Task SetBroadcasterTokensAsync(TwitchAccountTokens tokens, CancellationToken cancellationToken = default);

  /// <summary>
  /// Returns the current bot access token without triggering a refresh.
  /// Returns null if no bot token is configured.
  /// </summary>
  string? GetCurrentBotAccessToken();

  /// <summary>
  /// Returns the current broadcaster access token without triggering a refresh.
  /// Returns null if no broadcaster token is configured.
  /// </summary>
  string? GetCurrentBroadcasterAccessToken();

  /// <summary>
  /// Returns true if the bot account can be refreshed (has a refresh token).
  /// </summary>
  bool CanRefreshBotToken();

  /// <summary>
  /// Returns true if the broadcaster account can be refreshed (has a refresh token).
  /// </summary>
  bool CanRefreshBroadcasterToken();
}
