using System.Security.Cryptography;

namespace Caramel.Twitch.Auth;

public sealed class OAuthStateManager()
{
  private readonly Dictionary<string, DateTime> _activeStates = [];
  private readonly TimeSpan _stateTtl = TimeSpan.FromMinutes(10);

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
        _ = _activeStates.Remove(state);
        return false;
      }

      _ = _activeStates.Remove(state);
      return true;
    }
  }

  public void CleanupExpiredStates()
  {
    lock (_activeStates)
    {
      var now = DateTime.UtcNow;
      foreach (var kvp in _activeStates.Where(x => x.Value <= now).ToList())
      {
        _ = _activeStates.Remove(kvp.Key);
      }
    }
  }
}
