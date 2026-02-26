namespace Caramel.Twitch.Services;

public interface IAdsCoordinator
{
  bool IsOnCooldown();
  void SetCooldown(int retryAfterSeconds);
}

public sealed class AdsCoordinator : IAdsCoordinator
{
  private DateTimeOffset? _cooldownEndsAt;

  public bool IsOnCooldown()
  {
    return _cooldownEndsAt is not null && DateTimeOffset.UtcNow < _cooldownEndsAt.Value;
  }

  public void SetCooldown(int retryAfterSeconds)
  {
    _cooldownEndsAt = retryAfterSeconds > 0
      ? DateTimeOffset.UtcNow.AddSeconds(retryAfterSeconds)
      : null;
  }
}
