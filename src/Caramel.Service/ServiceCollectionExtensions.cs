using Caramel.Core.Data;
using Caramel.Core.OBS;
using Caramel.Service.OBS;

using Quartz;

namespace Caramel.Service;

public static class ServiceCollectionExtensions
{
  public static IServiceCollection AddRequiredServices(this IServiceCollection services, IConfiguration configuration)
  {
    // Register Redis for session management
    string redisConnectionString = configuration.GetConnectionString("Redis") ?? throw new MissingDatabaseStringException("Redis");

    _ = services.AddSingleton<StackExchange.Redis.IConnectionMultiplexer>(_ =>
        StackExchange.Redis.ConnectionMultiplexer.Connect(redisConnectionString));

    string quartzConnectionString = configuration.GetConnectionString("Quartz") ?? throw new MissingDatabaseStringException("Quartz");
    _ = services
        .AddQuartz(q =>
        {
          q.UsePersistentStore(s =>
            {
              s.UseProperties = true;
              s.UsePostgres(options =>
                {
                  options.ConnectionString = quartzConnectionString;
                  options.TablePrefix = "QRTZ_";
                });
              s.UseSystemTextJsonSerializer();
            });

        })
        .AddQuartzHostedService(opt => opt.WaitForJobsToComplete = true);

    // Register OBS service
    _ = services.Configure<ObsConfig>(configuration.GetSection(nameof(ObsConfig)));
    _ = services.AddSingleton<IOBSService, OBSService>();
    _ = services.AddSingleton<IOBSStatusProvider>(sp => sp.GetRequiredService<IOBSService>());
    _ = services.AddHostedService(sp => (OBSService)sp.GetRequiredService<IOBSService>());

    return services;
  }
}
