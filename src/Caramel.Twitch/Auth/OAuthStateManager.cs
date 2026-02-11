using System.Security.Cryptography;
using System.Text;

namespace Caramel.Twitch.Auth;

/// <summary>
/// Manages OAuth state parameters for CSRF protection during the authorization code flow.
/// </summary>
public sealed class OAuthStateManager
{
  private readonly string _encryptionKey;
  private readonly Dictionary<string, DateTime> _activeStates = new();
  private readonly TimeSpan _stateTtl = TimeSpan.FromMinutes(10);

  public OAuthStateManager(TwitchConfig config)
  {
    _encryptionKey = config.EncryptionKey;
  }

  /// <summary>
  /// Generates a new OAuth state parameter and stores it for validation.
  /// State parameters are valid for 10 minutes.
  /// </summary>
  public string GenerateState()
  {
    var randomBytes = new byte[32];
    using (var rng = RandomNumberGenerator.Create())
    {
      rng.GetBytes(randomBytes);
    }

    var state = Convert.ToBase64String(randomBytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    lock (_activeStates)
    {
      _activeStates[state] = DateTime.UtcNow.Add(_stateTtl);
    }

    return state;
  }

  /// <summary>
  /// Validates an OAuth state parameter. Returns true if the state is valid and removes it from the active set.
  /// </summary>
  public bool ValidateAndConsumeState(string state)
  {
    lock (_activeStates)
    {
      if (!_activeStates.TryGetValue(state, out var expiry))
      {
        return false;
      }

      if (DateTime.UtcNow > expiry)
      {
        _activeStates.Remove(state);
        return false;
      }

      _activeStates.Remove(state);
      return true;
    }
  }

  /// <summary>
  /// Removes expired state parameters (called periodically for cleanup).
  /// </summary>
  public void CleanupExpiredStates()
  {
    lock (_activeStates)
    {
      var now = DateTime.UtcNow;
      foreach (var kvp in _activeStates.Where(x => x.Value <= now).ToList())
      {
        _activeStates.Remove(kvp.Key);
      }
    }
  }
}
