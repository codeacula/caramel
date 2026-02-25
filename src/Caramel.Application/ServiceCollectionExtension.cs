using Microsoft.Extensions.DependencyInjection;

namespace Caramel.Application;

public static class ServiceCollectionExtension
{
  public static IServiceCollection AddApplicationServices(this IServiceCollection services)
  {
    _ = services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<ICaramelApplication>());
    _ = services.AddTransient(_ => TimeProvider.System);

    return services;
  }
}
