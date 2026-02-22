using Caramel.Core.Conversations;
using Caramel.Core.Data;
using Caramel.Core.People;
using Caramel.Core.ToDos;
using Caramel.Core.Twitch;
using Caramel.Database.Conversations;
using Caramel.Database.Conversations.Events;
using Caramel.Database.People;
using Caramel.Database.People.Events;
using Caramel.Database.ToDos;
using Caramel.Database.ToDos.Events;
using Caramel.Database.Twitch;
using Caramel.Database.Twitch.Events;

using Marten;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using DbPerson = Caramel.Database.People.DbPerson;

namespace Caramel.Database;

public static class ServiceCollectionExtensions
{
  public static IServiceCollection AddDatabaseServices(this IServiceCollection services, IConfiguration configuration)
  {
    const string connectionKey = "Caramel";
    var connectionString = configuration.GetConnectionString(connectionKey) ?? throw new MissingDatabaseStringException(connectionKey);
    var superAdminConfig = configuration.GetSection(nameof(SuperAdminConfig)).Get<SuperAdminConfig>() ?? new SuperAdminConfig();
    var personConfig = configuration.GetSection(nameof(PersonConfig)).Get<PersonConfig>() ?? new PersonConfig();

    _ = services.AddDbContextPool<CaramelDbContext>(options => options.UseNpgsql(connectionString));
    _ = services.AddSingleton(new CaramelConnectionString(connectionString));
    _ = services.AddSingleton(superAdminConfig);
    _ = services.AddSingleton(personConfig);
    _ = services.AddScoped<ICaramelDbContext, CaramelDbContext>();


    _ = services
      .AddMarten(options =>
      {
        options.Connection(connectionString);

        _ = options.Schema.For<DbPerson>()
          .Identity(x => x.Id)
          .Index(x => x.Username)
          .Index(x => new { x.Platform, x.PlatformUserId }, idx => idx.IsUnique = true);

        _ = options.Events.AddEventType<PersonCreatedEvent>();
        _ = options.Events.AddEventType<AccessGrantedEvent>();
        _ = options.Events.AddEventType<AccessRevokedEvent>();
        _ = options.Events.AddEventType<PersonUpdatedEvent>();
        _ = options.Events.AddEventType<PersonTimeZoneUpdatedEvent>();
        _ = options.Events.AddEventType<PersonDailyTaskCountUpdatedEvent>();

        _ = options.Schema.For<DbConversation>()
          .Identity(x => x.Id)
          .Index(x => new { x.PersonId });

        _ = options.Events.AddEventType<ConversationStartedEvent>();
        _ = options.Events.AddEventType<UserSentMessageEvent>();
        _ = options.Events.AddEventType<CaramelRepliedEvent>();

        _ = options.Schema.For<DbToDo>()
          .Identity(x => x.Id)
          .Index(x => x.PersonId);

        _ = options.Events.AddEventType<ToDoCreatedEvent>();
        _ = options.Events.AddEventType<ToDoUpdatedEvent>();
        _ = options.Events.AddEventType<ToDoCompletedEvent>();
        _ = options.Events.AddEventType<ToDoDeletedEvent>();
        _ = options.Events.AddEventType<ToDoReminderScheduledEvent>();
        _ = options.Events.AddEventType<ToDoReminderSetEvent>();

        _ = options.Schema.For<DbReminder>()
          .Identity(x => x.Id);

        _ = options.Events.AddEventType<ReminderCreatedEvent>();
        _ = options.Events.AddEventType<ReminderSentEvent>();
        _ = options.Events.AddEventType<ReminderAcknowledgedEvent>();
        _ = options.Events.AddEventType<ReminderDeletedEvent>();

        _ = options.Schema.For<DbToDoReminder>()
          .Identity(x => x.Id)
          .Index(x => x.ToDoId)
          .Index(x => x.ReminderId);

        _ = options.Events.AddEventType<ToDoReminderLinkedEvent>();
        _ = options.Events.AddEventType<ToDoReminderUnlinkedEvent>();

        _ = options.Schema.For<DbTwitchSetup>()
          .Identity(x => x.Id);

        _ = options.Events.AddEventType<TwitchSetupCreatedEvent>();
        _ = options.Events.AddEventType<TwitchSetupUpdatedEvent>();

        _ = options.Projections.Snapshot<DbPerson>(Marten.Events.Projections.SnapshotLifecycle.Inline);
        _ = options.Projections.Snapshot<DbConversation>(Marten.Events.Projections.SnapshotLifecycle.Inline);
        _ = options.Projections.Snapshot<DbToDo>(Marten.Events.Projections.SnapshotLifecycle.Inline);
        _ = options.Projections.Snapshot<DbReminder>(Marten.Events.Projections.SnapshotLifecycle.Inline);
        _ = options.Projections.Snapshot<DbToDoReminder>(Marten.Events.Projections.SnapshotLifecycle.Inline);
        _ = options.Projections.Snapshot<DbTwitchSetup>(Marten.Events.Projections.SnapshotLifecycle.Inline);
      })
      .UseLightweightSessions();

    _ = services
      .AddScoped<IConversationStore, ConversationStore>()
      .AddScoped<IPersonStore, PersonStore>()
      .AddScoped<IToDoStore, ToDoStore>()
      .AddScoped<IReminderStore, ReminderStore>()
      .AddScoped<ITwitchSetupStore, TwitchSetupStore>();

    return services;
  }

  public static async Task MigrateDatabaseAsync(this IServiceProvider serviceProvider, CancellationToken cancellationToken = default)
  {
    using var scope = serviceProvider.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<ICaramelDbContext>();
    await dbContext.MigrateAsync(cancellationToken);
  }
}
