using Caramel.Application.ToDos;
using Caramel.Core.ToDos;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Caramel.Application;

public static class ServiceCollectionExtension
{
  public static IServiceCollection AddApplicationServices(this IServiceCollection services)
  {
    _ = services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<ICaramelApplication>());

    services.TryAddScoped<IToDoReminderScheduler, NoOpToDoReminderScheduler>();

    _ = services.AddTransient(_ => TimeProvider.System);

    _ = services.AddSingleton<IFuzzyTimeParser, FuzzyTimeParser>();

    return services;
  }
}
