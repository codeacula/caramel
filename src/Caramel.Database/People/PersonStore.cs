using Caramel.Core;
using Caramel.Core.People;
using Caramel.Database.People.Events;
using Caramel.Domain.Common.Enums;
using Caramel.Domain.People.Models;
using Caramel.Domain.People.ValueObjects;

using FluentResults;

using Marten;

namespace Caramel.Database.People;

public sealed class PersonStore(SuperAdminConfig SuperAdminConfig, IDocumentSession session, TimeProvider timeProvider, IPersonCache personCache) : IPersonStore
{
  public async Task<Result<Person>> CreateByPlatformIdAsync(PlatformId platformId, CancellationToken cancellationToken = default)
  {
    return await ResultExtensions.ExecuteAsync(
      () => CreateByPlatformIdInternalAsync(platformId, cancellationToken),
      "Invalid operation creating user",
      "Unexpected error creating user");
  }

  private async Task<Person> CreateByPlatformIdInternalAsync(PlatformId platformId, CancellationToken cancellationToken)
  {
    var time = timeProvider.GetUtcDateTime();
    var id = Guid.NewGuid();
    var pce = new PersonCreatedEvent(platformId.Username, platformId.Platform, platformId.PlatformUserId)
    {
      Id = id,
      CreatedOn = time
    };

    var events = new List<object> { pce };

    if (IsSuperAdmin(platformId))
    {
      events.Add(new AccessGrantedEvent(time)
      {
        Id = id,
        CreatedOn = time
      });
    }

    _ = session.Events.StartStream<DbPerson>(id, events);
    await session.SaveChangesAsync(cancellationToken);

    var newPerson = await session.Events.AggregateStreamAsync<DbPerson>(id, token: cancellationToken);

    if (newPerson is null)
    {
      throw new InvalidOperationException($"Failed to create new user {platformId.Username}");
    }

    // Best-effort: populate PlatformId -> PersonId cache mapping after successful creation
    _ = await personCache.MapPlatformIdToPersonIdAsync(platformId, new PersonId(id));

    return (Person)newPerson;
  }

  private bool IsSuperAdmin(PlatformId platformId)
  {
    return !string.IsNullOrWhiteSpace(SuperAdminConfig.DiscordUserId)
      && platformId.Platform == Platform.Discord
      && string.Equals(platformId.PlatformUserId, SuperAdminConfig.DiscordUserId, StringComparison.OrdinalIgnoreCase);
  }

  public async Task<Result<HasAccess>> GetAccessAsync(PersonId id, CancellationToken cancellationToken = default)
  {
    return await ResultExtensions.ExecuteAsync(
      async () =>
      {
        var dbUser = await GetInternalAsync(id, cancellationToken);
        return dbUser.HasAccess;
      },
      "Invalid operation checking access",
      "Unexpected error checking access");
  }

  public async Task<Result<Person>> GetAsync(PersonId id, CancellationToken cancellationToken = default)
  {
    return await ResultExtensions.ExecuteAsync(
      () => GetInternalAsync(id, cancellationToken),
      $"Invalid operation retrieving user {id}",
      "Unexpected error retrieving user");
  }

  private async Task<Person> GetInternalAsync(PersonId id, CancellationToken cancellationToken)
  {
    var dbUser = await session.Query<DbPerson>()
      .FirstOrDefaultAsync(u => u.Id == id.Value, cancellationToken);
    return (Person)(dbUser ?? throw new InvalidOperationException($"User with ID {id} not found"));
  }

  public async Task<Result<Person>> GetByPlatformIdAsync(PlatformId platformId, CancellationToken cancellationToken = default)
  {
    return await ResultExtensions.ExecuteAsync(
      () => GetByPlatformIdInternalAsync(platformId, cancellationToken),
      "Invalid operation retrieving user by platform ID",
      "Unexpected error retrieving user by platform ID");
  }

  private async Task<Person> GetByPlatformIdInternalAsync(PlatformId platformId, CancellationToken cancellationToken)
  {
    var dbUser = await session.Query<DbPerson>()
      .FirstOrDefaultAsync(u => u.PlatformUserId == platformId.PlatformUserId && u.Platform == platformId.Platform, cancellationToken);
    return (Person)(dbUser ?? throw new InvalidOperationException($"User with ID {platformId.PlatformUserId} from {platformId.Platform} not found"));
  }

  public async Task<Result> GrantAccessAsync(PersonId id, CancellationToken cancellationToken = default)
  {
    return await ResultExtensions.ExecuteAsync(
      async () =>
      {
        var time = timeProvider.GetUtcDateTime();
        _ = session.Events.Append(id.Value, new AccessGrantedEvent(time)
        {
          Id = id.Value,
          CreatedOn = time
        });

        await session.SaveChangesAsync(cancellationToken);

        // Invalidate access cache after granting access
        _ = await personCache.InvalidateAccessAsync(id);
      },
      "Invalid operation granting access",
      "Unexpected error granting access");
  }

  public async Task<Result> RevokeAccessAsync(PersonId id, CancellationToken cancellationToken = default)
  {
    return await ResultExtensions.ExecuteAsync(
      async () =>
      {
        var time = timeProvider.GetUtcDateTime();
        _ = session.Events.Append(id.Value, new AccessRevokedEvent(time)
        {
          Id = id.Value,
          CreatedOn = time
        });

        await session.SaveChangesAsync(cancellationToken);

        // Invalidate access cache after revoking access
        _ = await personCache.InvalidateAccessAsync(id);
      },
      "Invalid operation revoking access",
      "Unexpected error revoking access");
  }

  public async Task<Result> SetTimeZoneAsync(PersonId id, PersonTimeZoneId timeZoneId, CancellationToken cancellationToken = default)
  {
    return await ResultExtensions.ExecuteAsync(
      async () =>
      {
        var time = timeProvider.GetUtcDateTime();
        _ = session.Events.Append(id.Value, new PersonTimeZoneUpdatedEvent(timeZoneId.Value, time)
        {
          Id = id.Value,
          CreatedOn = time
        });

        await session.SaveChangesAsync(cancellationToken);
      },
      "Invalid operation setting timezone",
      "Unexpected error setting timezone");
  }

  public async Task<Result> SetDailyTaskCountAsync(PersonId id, DailyTaskCount dailyTaskCount, CancellationToken cancellationToken = default)
  {
    return await ResultExtensions.ExecuteAsync(
      async () =>
      {
        var time = timeProvider.GetUtcDateTime();
        _ = session.Events.Append(id.Value, new PersonDailyTaskCountUpdatedEvent(dailyTaskCount.Value, time)
        {
          Id = id.Value,
          CreatedOn = time
        });

        await session.SaveChangesAsync(cancellationToken);
      },
      "Invalid operation setting daily task count",
      "Unexpected error setting daily task count");
  }

  public async Task<Result> AddNotificationChannelAsync(Person person, NotificationChannel channel, CancellationToken cancellationToken = default)
  {
    return await ResultExtensions.ExecuteAsync(
      async () =>
      {
        var time = timeProvider.GetUtcDateTime();
        _ = session.Events.Append(
          person.Id.Value,
          new NotificationChannelAddedEvent(person.PlatformId.Platform, person.PlatformId.PlatformUserId, channel.Type, channel.Identifier, time)
          {
            Id = person.Id.Value,
            CreatedOn = time
          });

        await session.SaveChangesAsync(cancellationToken);
      },
      "Invalid operation adding notification channel",
      "Unexpected error adding notification channel");
  }

  public async Task<Result> RemoveNotificationChannelAsync(Person person, NotificationChannel channel, CancellationToken cancellationToken = default)
  {
    return await ResultExtensions.ExecuteAsync(
      async () =>
      {
        var time = timeProvider.GetUtcDateTime();
        _ = session.Events.Append(
          person.Id.Value,
          new NotificationChannelRemovedEvent(person.PlatformId.Platform, person.PlatformId.PlatformUserId, channel.Type, channel.Identifier, time)
          {
            Id = person.Id.Value,
            CreatedOn = time
          });

        await session.SaveChangesAsync(cancellationToken);
      },
      "Invalid operation removing notification channel",
      "Unexpected error removing notification channel");
  }

  public async Task<Result> ToggleNotificationChannelAsync(Person person, NotificationChannel channel, CancellationToken cancellationToken = default)
  {
    return await ResultExtensions.ExecuteAsync(
      async () =>
      {
        var time = timeProvider.GetUtcDateTime();
        _ = session.Events.Append(
          person.Id.Value,
          new NotificationChannelToggledEvent(
            person.PlatformId.Platform,
            person.PlatformId.PlatformUserId,
            channel.Type,
            channel.Identifier,
            channel.IsEnabled,
            time)
          {
            Id = person.Id.Value,
            CreatedOn = time
          });

        await session.SaveChangesAsync(cancellationToken);
      },
      "Invalid operation toggling notification channel",
      "Unexpected error toggling notification channel");
  }

  public async Task<Result> EnsureNotificationChannelAsync(Person person, NotificationChannel channel, CancellationToken cancellationToken = default)
  {
    return await ResultExtensions.ExecuteAsync(
      () => EnsureNotificationChannelInternalAsync(person, channel, cancellationToken),
      "Invalid operation ensuring notification channel",
      "Unexpected error ensuring notification channel");
  }

  private async Task EnsureNotificationChannelInternalAsync(Person person, NotificationChannel channel, CancellationToken cancellationToken)
  {
    // Check if person already has a channel of this type
    NotificationChannel? existingChannel = person.NotificationChannels.FirstOrDefault(c => c.Type == channel.Type);
    if (existingChannel is not null)
    {
      var ch = (NotificationChannel)existingChannel;
      // If the identifier is the same, nothing to do
      if (ch.Identifier == channel.Identifier)
      {
        return;
      }

      // Remove old channel and add new one
      var removeResult = await RemoveNotificationChannelAsync(person, ch, cancellationToken);
      if (removeResult.IsFailed)
      {
        throw new InvalidOperationException($"Failed to remove old notification channel: {removeResult.GetErrorMessages()}");
      }

      var addResult = await AddNotificationChannelAsync(person, channel, cancellationToken);
      if (addResult.IsFailed)
      {
        throw new InvalidOperationException($"Failed to add new notification channel: {addResult.GetErrorMessages()}");
      }

      return;
    }

    // No channel of this type exists yet: add the channel
    var result = await AddNotificationChannelAsync(person, channel, cancellationToken);
    if (result.IsFailed)
    {
      throw new InvalidOperationException($"Failed to add notification channel: {result.GetErrorMessages()}");
    }
  }
}
