namespace Caramel.Core.Twitch;

/// <summary>
/// Publishes notifications when the persisted Twitch setup changes.
/// Implementations are responsible for propagating the latest setup state to
/// interested runtime services so they can reload and reprovision themselves.
/// </summary>
public interface ITwitchSetupChangedNotifier
{
  /// <summary>
  /// Publishes a Twitch setup changed notification containing the latest
  /// persisted setup snapshot.
  /// </summary>
  /// <param name="setup">The latest persisted Twitch setup.</param>
  /// <param name="cancellationToken">Cancellation token for the operation.</param>
  Task PublishAsync(
    Domain.Twitch.TwitchSetup setup,
    CancellationToken cancellationToken = default);
}
