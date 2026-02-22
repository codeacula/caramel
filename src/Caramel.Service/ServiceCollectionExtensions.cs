using Caramel.Core.Data;
using Caramel.Core.ToDos;
using Caramel.Notifications;
using Caramel.Service.Jobs;

using NetCord;
using NetCord.Rest;

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

    _ = services.AddScoped<IToDoReminderScheduler, QuartzToDoReminderScheduler>();

    // Register Discord REST client for notifications
    var discordToken = configuration["Discord:Token"];
    if (!string.IsNullOrWhiteSpace(discordToken))
    {
      _ = services.AddSingleton(new RestClient(new BotToken(discordToken)));
      _ = services.AddNotificationsWithChannels();
    }
    else
    {
      _ = services.AddNotifications();
    }

    // Register Twitch notification channel if configured
    var twitchAccessToken = configuration["Twitch:AccessToken"];
    var twitchBotUserId = configuration["Twitch:BotUserId"];
    if (!string.IsNullOrWhiteSpace(twitchAccessToken) && !string.IsNullOrWhiteSpace(twitchBotUserId))
    {
      // For now, we register a placeholder whisper delegate. 
      // The actual implementation will be in Caramel.Twitch host which will communicate via gRPC.
      // This service is optional and can be replaced with a real TwitchLib client when needed.
      Func<string, string, string, CancellationToken, Task<bool>> sendWhisperAsync = async (botId, recipientId, message, ct) =>
      {
        // Placeholder: would use TwitchLib API client to send whisper
        // For now, just log and return success - actual whispers will be sent by Caramel.Twitch host
        await Task.CompletedTask;
        return true;
      };

      _ = services.AddTwitchNotificationChannel(sendWhisperAsync, twitchBotUserId);
    }

    return services;
  }
}

