using Caramel.Domain.ToDos.Models;

using NetCord.Rest;

namespace Caramel.Discord.Components;

public class ReminderCreatedComponent : ComponentContainerProperties
{
  public ReminderCreatedComponent(Reminder reminder)
  {
    AccentColor = Constants.Colors.CaramelGreen;

    var unix = new DateTimeOffset(reminder.ReminderTime.Value).ToUnixTimeSeconds();

    Components =
    [
      new TextDisplayProperties("# Reminder Set"),
      new TextDisplayProperties($"**{reminder.Details.Value}**"),
      new TextDisplayProperties($"I'll remind you <t:{unix}:R> (<t:{unix}:F>)"),
      new TextDisplayProperties($"ID: `{reminder.Id.Value}`")
    ];
  }
}
