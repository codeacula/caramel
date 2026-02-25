namespace Caramel.Twitch.Extensions;

public static class TwitchPlatformExtension
{
  public static PlatformId GetTwitchPlatformId(string twitchUsername, string twitchUserId)
  {
    return new PlatformId(twitchUsername, twitchUserId, Platform.Twitch);
  }
}
