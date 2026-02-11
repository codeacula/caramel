using Caramel.AI.Config;
using Caramel.AI.Prompts;
using Caramel.Core.ToDos;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Caramel.AI;

public static class ServiceCollectionExtensions
{
  public static IServiceCollection AddAiServices(this IServiceCollection services, IConfiguration configuration)
  {
    var caramelAiConfig = configuration.GetSection(nameof(CaramelAIConfig)).Get<CaramelAIConfig>();

    if (caramelAiConfig == null)
    {
      Console.WriteLine("No AI configuration set; using default settings.");
    }

    var config = caramelAiConfig ?? new CaramelAIConfig();

    _ = services
      .AddSingleton(config)
      .AddSingleton<IPromptLoader, PromptLoader>()
      .AddSingleton<IPromptTemplateProcessor, PromptTemplateProcessor>()
      .AddTransient<ICaramelAIAgent, CaramelAIAgent>()
      .AddTransient<IReminderMessageGenerator, CaramelReminderMessageGenerator>();

    return services;
  }
}
