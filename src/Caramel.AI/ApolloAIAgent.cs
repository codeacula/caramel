using Caramel.AI.Config;
using Caramel.AI.DTOs;
using Caramel.AI.Prompts;
using Caramel.AI.Requests;

using Microsoft.Extensions.Logging;

namespace Caramel.AI;

public sealed class CaramelAIAgent(
  CaramelAIConfig config,
  IPromptLoader promptLoader,
  IPromptTemplateProcessor templateProcessor,
  ILogger<AIRequestBuilder> logger) : ICaramelAIAgent
{
  private const string ToolPlanningPromptName = "CaramelToolPlanning";
  private const string ResponsePromptName = "CaramelResponse";
  private const string ReminderPromptName = "CaramelReminder";
  private const string DailyPlanningPromptName = "CaramelDailyPlanning";

  public IAIRequestBuilder CreateRequest()
  {
    return new AIRequestBuilder(config, templateProcessor, logger);
  }

  public IAIRequestBuilder CreateToolPlanningRequest(
    IEnumerable<ChatMessageDTO> messages,
    string userTimezone,
    string activeTodos)
  {
    var prompt = promptLoader.Load(ToolPlanningPromptName);
    var currentDateTime = DateTimeOffset.Now.ToString("yyyy-MM-ddTHH:mm:sszzz", System.Globalization.CultureInfo.InvariantCulture);

    var variables = new Dictionary<string, string>
    {
      ["current_datetime"] = currentDateTime,
      ["user_timezone"] = userTimezone,
      ["active_todos"] = activeTodos
    };

    return CreateRequest()
      .FromPromptDefinition(prompt)
      .WithMessages(messages)
      .WithToolCalling(enabled: false)
      .WithJsonMode(enabled: true)
      .WithTemplateVariables(variables);
  }

  public IAIRequestBuilder CreateResponseRequest(
    IEnumerable<ChatMessageDTO> messages,
    string actionsSummary,
    string userTimezone)
  {
    var prompt = promptLoader.Load(ResponsePromptName);

    var variables = new Dictionary<string, string>
    {
      ["actions_taken"] = actionsSummary,
      ["user_timezone"] = userTimezone
    };

    return CreateRequest()
      .FromPromptDefinition(prompt)
      .WithMessages(messages)
      .WithToolCalling(enabled: false)
      .WithTemplateVariables(variables);
  }

  public IAIRequestBuilder CreateReminderRequest(
    string userTimezone,
    string currentTime,
    string reminderItems)
  {
    var prompt = promptLoader.Load(ReminderPromptName);

    var variables = new Dictionary<string, string>
    {
      ["user_timezone"] = userTimezone,
      ["current_time"] = currentTime,
      ["reminder_items"] = reminderItems
    };

    return CreateRequest()
      .FromPromptDefinition(prompt)
      .WithTemplateVariables(variables);
  }

  public IAIRequestBuilder CreateDailyPlanRequest(
    string userTimezone,
    string currentTime,
    string activeTodos,
    int taskCount)
  {
    var prompt = promptLoader.Load(DailyPlanningPromptName);

    var variables = new Dictionary<string, string>
    {
      ["user_timezone"] = userTimezone,
      ["current_time"] = currentTime,
      ["active_todos"] = activeTodos,
      ["task_count"] = taskCount.ToString(System.Globalization.CultureInfo.InvariantCulture)
    };

    return CreateRequest()
      .FromPromptDefinition(prompt)
      .WithToolCalling(enabled: false)
      .WithJsonMode(enabled: true)
      .WithTemplateVariables(variables);
  }
}
