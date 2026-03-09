using Caramel.AI.Config;
using Caramel.AI.Prompts;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Caramel.AI;

public static class ServiceCollectionExtensions
{
  public static IServiceCollection AddAiServices(this IServiceCollection services, IConfiguration configuration)
  {
    var caramelAiConfig = configuration.GetSection(nameof(CaramelAiConfig)).Get<CaramelAiConfig>();
    var config = caramelAiConfig ?? new CaramelAiConfig();

    _ = services
      .AddSingleton(config)
      .AddSingleton<IPromptLoader, PromptLoader>()
      .AddSingleton<IPromptTemplateProcessor, PromptTemplateProcessor>()
      .AddTransient<ICaramelAIAgent, CaramelAIAgent>();

    return services;
  }
}
