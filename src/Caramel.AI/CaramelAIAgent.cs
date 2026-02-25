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

  public IAIRequestBuilder CreateRequest()
  {
    return new AIRequestBuilder(config, templateProcessor, logger);
  }

  public IAIRequestBuilder CreateToolPlanningRequest(
    IEnumerable<ChatMessageDTO> messages,
    string userTimezone)
  {
    var prompt = promptLoader.Load(ToolPlanningPromptName);
    var currentDateTime = DateTimeOffset.Now.ToString("yyyy-MM-ddTHH:mm:sszzz", System.Globalization.CultureInfo.InvariantCulture);

    var variables = new Dictionary<string, string>
    {
      ["current_datetime"] = currentDateTime,
      ["user_timezone"] = userTimezone
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
}
