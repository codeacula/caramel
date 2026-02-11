namespace Caramel.Twitch.Extensions;

/// <summary>
/// Extension methods for converting Twitch data to platform identifiers.
/// </summary>
public static class TwitchPlatformExtension
{
  /// <summary>
  /// Converts Twitch user data to a PlatformId value object.
  /// </summary>
  public static PlatformId GetTwitchPlatformId(string twitchUsername, string twitchUserId)
  {
    return new PlatformId(twitchUsername, twitchUserId, Platform.Twitch);
  }
}
