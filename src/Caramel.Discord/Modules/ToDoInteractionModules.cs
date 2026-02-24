using Caramel.Discord.Components;

using NetCord.Services.ComponentInteractions;

namespace Caramel.Discord.Modules;

public class ToDoPriorityInteractionModule : ComponentInteractionModule<StringMenuInteractionContext>
{
  [ComponentInteraction(ToDoQuickCreateComponent.PrioritySelectCustomId)]
  public string HandlePriority()
  {
    return $"Priority noted: {string.Join(", ", Context.SelectedValues)} (updates coming soon).";
  }
}

public class ToDoEnergyInteractionModule : ComponentInteractionModule<StringMenuInteractionContext>
{
  [ComponentInteraction(ToDoQuickCreateComponent.EnergySelectCustomId)]
  public string HandleEnergy()
  {
    return $"Energy level noted: {string.Join(", ", Context.SelectedValues)} (updates coming soon).";
  }
}

public class ToDoInterestInteractionModule : ComponentInteractionModule<StringMenuInteractionContext>
{
  [ComponentInteraction(ToDoQuickCreateComponent.InterestSelectCustomId)]
  public string HandleInterest()
  {
    return $"Interest noted: {string.Join(", ", Context.SelectedValues)} (updates coming soon).";
  }
}

public class ToDoReminderInteractionModule : ComponentInteractionModule<ButtonInteractionContext>
{
  [ComponentInteraction(ToDoQuickCreateComponent.ReminderButtonCustomId)]
  public string HandleReminderButton()
  {
    return "Reminder editing is coming soon. For now, reply with a time and I'll log it!";
  }
}

public class ToDoEditInteractionModule : ComponentInteractionModule<ButtonInteractionContext>
{
  [ComponentInteraction(ToDoListComponent.EditButtonCustomId)]
  public string HandleEditButton()
  {
    return "Select which todo you'd like to edit via a select menu (coming soon!)";
  }
}

public class ToDoDeleteInteractionModule : ComponentInteractionModule<ButtonInteractionContext>
{
  [ComponentInteraction(ToDoListComponent.DeleteButtonCustomId)]
  public string HandleDeleteButton()
  {
    return "Select which todo you'd like to delete via a select menu (coming soon!)";
  }
}
