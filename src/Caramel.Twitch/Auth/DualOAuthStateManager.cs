using System.Security.Cryptography;
using Caramel.Domain.Twitch;

namespace Caramel.Twitch.Auth;

/// <summary>
/// Manages OAuth state tokens for dual-account authorization flows.
/// Encodes the account type (Bot/Broadcaster) in the state to route callback to correct handler.
/// </summary>
public sealed class DualOAuthStateManager()
{
  private readonly Dictionary<string, (TwitchAccountType accountType, DateTime expiry)> _activeStates = [];
  private readonly TimeSpan _stateTtl = TimeSpan.FromMinutes(10);

  /// <summary>
  /// Generates a state token for the given account type.
  /// State includes the account type so it can be routed correctly on callback.
  /// </summary>
  public string GenerateState(TwitchAccountType accountType)
  {
    var randomBytes = new byte[32];
    using (var rng = RandomNumberGenerator.Create())
    {
      rng.GetBytes(randomBytes);
    }

    var state = Convert.ToBase64String(randomBytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    lock (_activeStates)
    {
      _activeStates[state] = (accountType, DateTime.UtcNow.Add(_stateTtl));
    }

    return state;
  }

  /// <summary>
  /// Validates and consumes a state token, returning the associated account type.
  /// Returns null if state is invalid, expired, or missing.
  /// </summary>
  public TwitchAccountType? ValidateAndConsumeState(string state)
  {
    lock (_activeStates)
    {
      if (!_activeStates.TryGetValue(state, out var entry))
      {
        return null;
      }

      if (DateTime.UtcNow > entry.expiry)
      {
        _ = _activeStates.Remove(state);
        return null;
      }

      _ = _activeStates.Remove(state);
      return entry.accountType;
    }
  }

  /// <summary>
  /// Removes expired state tokens from the active states dictionary.
  /// </summary>
  public void CleanupExpiredStates()
  {
    lock (_activeStates)
    {
      var now = DateTime.UtcNow;
      foreach (var kvp in _activeStates.Where(x => x.Value.expiry <= now).ToList())
      {
        _ = _activeStates.Remove(kvp.Key);
      }
    }
  }
}
