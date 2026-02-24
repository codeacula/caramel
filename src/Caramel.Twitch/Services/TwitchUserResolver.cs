using Caramel.Twitch.Auth;

namespace Caramel.Twitch.Services;

/// <summary>
/// Contract for resolving Twitch usernames to numeric user IDs via the Helix API.
/// </summary>
public interface ITwitchUserResolver
{
  /// <summary>
  /// Resolves a Twitch login name or numeric ID to a numeric user ID.
  /// If the input is already numeric, it is returned as-is.
  /// Results are cached in-memory for the lifetime of the service.
  /// </summary>
  /// <param name="loginOrId"></param>
  /// <param name="cancellationToken"></param>
  Task<string> ResolveUserIdAsync(string loginOrId, CancellationToken cancellationToken = default);

  /// <summary>
  /// Resolves multiple logins/IDs in a single batch call.
  /// </summary>
  /// <param name="loginsOrIds"></param>
  /// <param name="cancellationToken"></param>
  Task<IReadOnlyList<string>> ResolveUserIdsAsync(IEnumerable<string> loginsOrIds, CancellationToken cancellationToken = default);

  /// <summary>
  /// Returns the identity (numeric ID and login) of the user who owns the current access token
  /// by calling <c>GET /helix/users</c> with no parameters.
  /// </summary>
  /// <param name="cancellationToken"></param>
  Task<(string UserId, string Login)> ResolveCurrentUserAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Resolves Twitch usernames to numeric user IDs via the Helix "Get Users" API.
/// Caches results in-memory so that repeated lookups for the same user avoid extra API calls.
/// </summary>
/// <param name="httpClientFactory"></param>
/// <param name="twitchConfig"></param>
/// <param name="tokenManager"></param>
/// <param name="logger"></param>
public sealed class TwitchUserResolver(
  IHttpClientFactory httpClientFactory,
  TwitchConfig twitchConfig,
  TwitchTokenManager tokenManager,
  ILogger<TwitchUserResolver> logger) : ITwitchUserResolver
{
  private readonly ConcurrentDictionary<string, string> _cache = new(StringComparer.OrdinalIgnoreCase);

  /// <inheritdoc/>
  public async Task<string> ResolveUserIdAsync(string loginOrId, CancellationToken cancellationToken = default)
  {
    if (string.IsNullOrWhiteSpace(loginOrId))
    {
      throw new ArgumentException("Login or ID must not be empty", nameof(loginOrId));
    }

    // If it's already a numeric ID, return as-is
    if (loginOrId.All(char.IsDigit))
    {
      return loginOrId;
    }

    // Check cache
    if (_cache.TryGetValue(loginOrId, out var cached))
    {
      return cached;
    }

    // Resolve via Helix API
    var resolved = await FetchUserIdAsync(loginOrId, cancellationToken);
    _ = _cache.TryAdd(loginOrId, resolved);
    return resolved;
  }

  /// <inheritdoc/>
  public async Task<IReadOnlyList<string>> ResolveUserIdsAsync(
    IEnumerable<string> loginsOrIds,
    CancellationToken cancellationToken = default)
  {
    var results = new List<string>();
    var toResolve = new List<string>();

    foreach (var loginOrId in loginsOrIds)
    {
      if (loginOrId.All(char.IsDigit))
      {
        results.Add(loginOrId);
      }
      else if (_cache.TryGetValue(loginOrId, out var cached))
      {
        results.Add(cached);
      }
      else
      {
        toResolve.Add(loginOrId);
      }
    }

    if (toResolve.Count > 0)
    {
      var resolved = await FetchUserIdsAsync(toResolve, cancellationToken);
      results.AddRange(resolved);
    }

    return results;
  }

  /// <inheritdoc/>
  public async Task<(string UserId, string Login)> ResolveCurrentUserAsync(CancellationToken cancellationToken = default)
  {
    var accessToken = await tokenManager.GetValidAccessTokenAsync(cancellationToken);

    using var httpClient = httpClientFactory.CreateClient("TwitchHelix");
    httpClient.DefaultRequestHeaders.Add("Client-Id", twitchConfig.ClientId);
    httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {accessToken}");

    var response = await httpClient.GetAsync("https://api.twitch.tv/helix/users", cancellationToken);
    if (!response.IsSuccessStatusCode)
    {
      var error = await response.Content.ReadAsStringAsync(cancellationToken);
      throw new InvalidOperationException(
        $"Twitch Helix 'Get Users' (current user) failed with status {(int)response.StatusCode}: {error}");
    }

    var content = await response.Content.ReadAsStringAsync(cancellationToken);
    var json = JsonDocument.Parse(content);
    var data = json.RootElement.GetProperty("data");

    if (data.GetArrayLength() == 0)
    {
      throw new InvalidOperationException("Twitch Helix returned no user data for the current access token.");
    }

    var user = data[0];
    var userId = user.GetProperty("id").GetString()!;
    var login = user.GetProperty("login").GetString()!;

    _ = _cache.TryAdd(login, userId);
    TwitchUserResolverLogs.UserResolved(logger, login, userId);

    return (userId, login);
  }

  private async Task<string> FetchUserIdAsync(string login, CancellationToken cancellationToken)
  {
    var ids = await FetchUserIdsAsync([login], cancellationToken);
    return ids.Count > 0
      ? ids[0]
      : throw new InvalidOperationException($"Twitch user '{login}' not found");
  }

  private async Task<List<string>> FetchUserIdsAsync(List<string> logins, CancellationToken cancellationToken)
  {
    var accessToken = await tokenManager.GetValidAccessTokenAsync(cancellationToken);

    using var httpClient = httpClientFactory.CreateClient("TwitchHelix");
    httpClient.DefaultRequestHeaders.Add("Client-Id", twitchConfig.ClientId);
    httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {accessToken}");

    // Build query string: ?login=user1&login=user2 (Helix supports up to 100 per call)
    var queryParams = string.Join("&", logins.Select(l => $"login={Uri.EscapeDataString(l)}"));
    var url = $"https://api.twitch.tv/helix/users?{queryParams}";

    var response = await httpClient.GetAsync(url, cancellationToken);
    if (!response.IsSuccessStatusCode)
    {
      var error = await response.Content.ReadAsStringAsync(cancellationToken);
      throw new InvalidOperationException(
        $"Twitch Helix 'Get Users' failed with status {(int)response.StatusCode}: {error}");
    }

    var content = await response.Content.ReadAsStringAsync(cancellationToken);
    var json = JsonDocument.Parse(content);
    var results = new List<string>();

    foreach (var user in json.RootElement.GetProperty("data").EnumerateArray())
    {
      var id = user.GetProperty("id").GetString()!;
      var login = user.GetProperty("login").GetString()!;

      _ = _cache.TryAdd(login, id);
      results.Add(id);

      TwitchUserResolverLogs.UserResolved(logger, login, id);
    }

    // Check for any logins that weren't found
    var resolvedLogins = new HashSet<string>(results.Count);
    foreach (var user in json.RootElement.GetProperty("data").EnumerateArray())
    {
      _ = resolvedLogins.Add(user.GetProperty("login").GetString()!);
    }

    foreach (var login2 in logins)
    {
      if (!resolvedLogins.Contains(login2.ToLowerInvariant()))
      {
        TwitchUserResolverLogs.UserNotFound(logger, login2);
      }
    }

    return results;
  }
}

/// <summary>
/// Structured logging for <see cref="TwitchUserResolver"/>.
/// </summary>
internal static partial class TwitchUserResolverLogs
{
  [LoggerMessage(Level = LogLevel.Information, Message = "Resolved Twitch user '{Login}' to ID {UserId}")]
  public static partial void UserResolved(ILogger logger, string login, string userId);

  [LoggerMessage(Level = LogLevel.Warning, Message = "Twitch user '{Login}' not found via Helix API")]
  public static partial void UserNotFound(ILogger logger, string login);
}
