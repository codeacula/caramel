using Caramel.Domain.People.ValueObjects;

using NetCord;
using NetCord.Rest;
using NetCord.Services.ApplicationCommands;

using CaramelPlatform = Caramel.Domain.Common.Enums.Platform;

namespace Caramel.Discord.Modules;

public class AdminCommandModule(ICaramelServiceClient caramelServiceClient) : ApplicationCommandModule<ApplicationCommandContext>
{
  [SlashCommand("grant-access", "Grant a user access to Caramel (Admin only)")]
  public async Task GrantAccessAsync(
    [SlashCommandParameter(Name = "user", Description = "The Discord user to grant access to")] User targetUser
  )
  {
    _ = await RespondAsync(InteractionCallback.DeferredMessage(MessageFlags.Ephemeral));

    var adminPlatformId = new PlatformId(
      Context.User.Username,
      Context.User.Id.ToString(CultureInfo.InvariantCulture),
      CaramelPlatform.Discord
    );

    var targetPlatformId = new PlatformId(
      targetUser.Username,
      targetUser.Id.ToString(CultureInfo.InvariantCulture),
      CaramelPlatform.Discord
    );

    var result = await caramelServiceClient.GrantAccessAsync(adminPlatformId, targetPlatformId, CancellationToken.None);

    if (result.IsFailed)
    {
      _ = await ModifyResponseAsync(message => message.Content = $"Failed to grant access: {result.GetErrorMessages(", ")}");
      return;
    }

    _ = await ModifyResponseAsync(message => message.Content = $"Access granted to {targetUser.Username} ({targetUser.Id})");
  }

  [SlashCommand("revoke-access", "Revoke a user's access to Caramel (Admin only)")]
  public async Task RevokeAccessAsync(
    [SlashCommandParameter(Name = "user", Description = "The Discord user to revoke access from")] User targetUser
  )
  {
    _ = await RespondAsync(InteractionCallback.DeferredMessage(MessageFlags.Ephemeral));

    var adminPlatformId = new PlatformId(
      Context.User.Username,
      Context.User.Id.ToString(CultureInfo.InvariantCulture),
      CaramelPlatform.Discord
    );

    var targetPlatformId = new PlatformId(
      targetUser.Username,
      targetUser.Id.ToString(CultureInfo.InvariantCulture),
      CaramelPlatform.Discord
    );

    var result = await caramelServiceClient.RevokeAccessAsync(adminPlatformId, targetPlatformId, CancellationToken.None);

    if (result.IsFailed)
    {
      _ = await ModifyResponseAsync(message => message.Content = $"Failed to revoke access: {result.GetErrorMessages(", ")}");
      return;
    }

    _ = await ModifyResponseAsync(message => message.Content = $"Access revoked from {targetUser.Username} ({targetUser.Id})");
  }
}
