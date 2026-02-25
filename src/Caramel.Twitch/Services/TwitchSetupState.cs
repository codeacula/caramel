namespace Caramel.Twitch.Services;

/// <summary>
/// Holds the in-memory Twitch setup state loaded from the database at startup.
/// Updated whenever the setup wizard completes or the setup is loaded on startup.
/// </summary>
public interface ITwitchSetupState
{
  TwitchSetup? Current { get; }
  bool IsConfigured { get; }
  void Update(TwitchSetup setup);
}

/// <summary>
/// Singleton in-memory store for the current Twitch setup.
/// </summary>
public sealed class TwitchSetupState : ITwitchSetupState
{
  public TwitchSetup? Current { get; private set; }
  public bool IsConfigured => Current is not null;

  public void Update(TwitchSetup setup)
  {
    Current = setup;
  }
}
