using Caramel.Domain.People.ValueObjects;

using NetCord.Gateway;

using CaramelPlatform = Caramel.Domain.Common.Enums.Platform;

namespace Caramel.Discord.Extensions;

public static class NetcordMessageExtension
{
  public static PlatformId GetDiscordPlatformId(this Message message)
  {
    return new PlatformId(message.Author.Username, message.Author.Id.ToString(CultureInfo.InvariantCulture), CaramelPlatform.Discord);
  }
}
