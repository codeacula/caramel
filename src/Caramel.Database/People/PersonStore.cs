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
    try
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
        return Result.Fail<Person>($"Failed to create new user {platformId.Username}");
      }

      // Best-effort: populate PlatformId -> PersonId cache mapping after successful creation
      _ = await personCache.MapPlatformIdToPersonIdAsync(platformId, new PersonId(id));

      return Result.Ok((Person)newPerson);
    }
    catch (OperationCanceledException)
    {
      throw;
    }
    catch (InvalidOperationException ex)
    {
      return Result.Fail<Person>($"Invalid operation creating user {platformId.Username}: {ex.Message}");
    }
    catch (Exception ex)
    {
      return Result.Fail<Person>($"Unexpected error creating user {platformId.Username}: {ex.Message}");
    }
  }

  private bool IsSuperAdmin(PlatformId platformId)
  {
    return !string.IsNullOrWhiteSpace(SuperAdminConfig.DiscordUserId)
      && platformId.Platform == Platform.Discord
      && string.Equals(platformId.PlatformUserId, SuperAdminConfig.DiscordUserId, StringComparison.OrdinalIgnoreCase);
  }

  public async Task<Result<HasAccess>> GetAccessAsync(PersonId id, CancellationToken cancellationToken = default)
  {
    try
    {
      var dbUser = await GetAsync(id, cancellationToken);

      return dbUser.IsFailed ? Result.Fail<HasAccess>(dbUser.Errors) : Result.Ok(dbUser.Value.HasAccess);
    }
    catch (OperationCanceledException)
    {
      throw;
    }
    catch (InvalidOperationException ex)
    {
      return Result.Fail<HasAccess>($"Invalid operation checking access for {id}: {ex.Message}");
    }
    catch (Exception ex)
    {
      return Result.Fail<HasAccess>($"Unexpected error checking access: {ex.Message}");
    }
  }

  public async Task<Result<Person>> GetAsync(PersonId id, CancellationToken cancellationToken = default)
  {
    try
    {
      var dbUser = await session.Query<DbPerson>()
        .FirstOrDefaultAsync(u => u.Id == id.Value, cancellationToken);
      return dbUser is null ? Result.Fail<Person>($"User with ID {id} not found") : Result.Ok((Person)dbUser);
    }
    catch (OperationCanceledException)
    {
      throw;
    }
    catch (InvalidOperationException ex)
    {
      return Result.Fail<Person>($"Invalid operation retrieving user {id}: {ex.Message}");
    }
    catch (Exception ex)
    {
      return Result.Fail<Person>($"Unexpected error retrieving user: {ex.Message}");
    }
  }

  public async Task<Result<Person>> GetByPlatformIdAsync(PlatformId platformId, CancellationToken cancellationToken = default)
  {
    try
    {
      var dbUser = await session.Query<DbPerson>()
        .FirstOrDefaultAsync(u => u.PlatformUserId == platformId.PlatformUserId && u.Platform == platformId.Platform, cancellationToken);
      return dbUser is null ? Result.Fail<Person>($"User with ID {platformId.PlatformUserId} from {platformId.Platform} not found") : Result.Ok((Person)dbUser);
    }
    catch (OperationCanceledException)
    {
      throw;
    }
    catch (InvalidOperationException ex)
    {
      return Result.Fail<Person>($"Invalid operation retrieving user by platform ID: {ex.Message}");
    }
    catch (Exception ex)
    {
      return Result.Fail<Person>($"Unexpected error retrieving user by platform ID: {ex.Message}");
    }
  }

  public async Task<Result> GrantAccessAsync(PersonId id, CancellationToken cancellationToken = default)
  {
    try
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

      return Result.Ok();
    }
    catch (OperationCanceledException)
    {
      throw;
    }
    catch (InvalidOperationException ex)
    {
      return Result.Fail($"Invalid operation granting access to {id}: {ex.Message}");
    }
    catch (Exception ex)
    {
      return Result.Fail($"Unexpected error granting access: {ex.Message}");
    }
  }

  public async Task<Result> RevokeAccessAsync(PersonId id, CancellationToken cancellationToken = default)
  {
    try
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

      return Result.Ok();
    }
    catch (OperationCanceledException)
    {
      throw;
    }
    catch (InvalidOperationException ex)
    {
      return Result.Fail($"Invalid operation revoking access from {id}: {ex.Message}");
    }
    catch (Exception ex)
    {
      return Result.Fail($"Unexpected error revoking access: {ex.Message}");
    }
  }

  public async Task<Result> SetTimeZoneAsync(PersonId id, PersonTimeZoneId timeZoneId, CancellationToken cancellationToken = default)
  {
    try
    {
      var time = timeProvider.GetUtcDateTime();
      _ = session.Events.Append(id.Value, new PersonTimeZoneUpdatedEvent(timeZoneId.Value, time)
      {
        Id = id.Value,
        CreatedOn = time
      });

      await session.SaveChangesAsync(cancellationToken);

      return Result.Ok();
    }
    catch (OperationCanceledException)
    {
      throw;
    }
    catch (InvalidOperationException ex)
    {
      return Result.Fail($"Invalid operation setting timezone for {id}: {ex.Message}");
    }
    catch (Exception ex)
    {
      return Result.Fail($"Unexpected error setting timezone: {ex.Message}");
    }
  }

  public async Task<Result> SetDailyTaskCountAsync(PersonId id, DailyTaskCount dailyTaskCount, CancellationToken cancellationToken = default)
  {
    try
    {
      var time = timeProvider.GetUtcDateTime();
      _ = session.Events.Append(id.Value, new PersonDailyTaskCountUpdatedEvent(dailyTaskCount.Value, time)
      {
        Id = id.Value,
        CreatedOn = time
      });

      await session.SaveChangesAsync(cancellationToken);

      return Result.Ok();
    }
    catch (OperationCanceledException)
    {
      throw;
    }
    catch (InvalidOperationException ex)
    {
      return Result.Fail($"Invalid operation setting daily task count for {id}: {ex.Message}");
    }
    catch (Exception ex)
    {
      return Result.Fail($"Unexpected error setting daily task count: {ex.Message}");
    }
  }

  public async Task<Result> AddNotificationChannelAsync(Person person, NotificationChannel channel, CancellationToken cancellationToken = default)
  {
    try
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

      return Result.Ok();
    }
    catch (OperationCanceledException)
    {
      throw;
    }
    catch (InvalidOperationException ex)
    {
      return Result.Fail($"Invalid operation adding notification channel: {ex.Message}");
    }
    catch (Exception ex)
    {
      return Result.Fail($"Unexpected error adding notification channel: {ex.Message}");
    }
  }

  public async Task<Result> RemoveNotificationChannelAsync(Person person, NotificationChannel channel, CancellationToken cancellationToken = default)
  {
    try
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

      return Result.Ok();
    }
    catch (OperationCanceledException)
    {
      throw;
    }
    catch (InvalidOperationException ex)
    {
      return Result.Fail($"Invalid operation removing notification channel: {ex.Message}");
    }
    catch (Exception ex)
    {
      return Result.Fail($"Unexpected error removing notification channel: {ex.Message}");
    }
  }

  public async Task<Result> ToggleNotificationChannelAsync(Person person, NotificationChannel channel, CancellationToken cancellationToken = default)
  {
    try
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

      return Result.Ok();
    }
    catch (OperationCanceledException)
    {
      throw;
    }
    catch (InvalidOperationException ex)
    {
      return Result.Fail($"Invalid operation toggling notification channel: {ex.Message}");
    }
    catch (Exception ex)
    {
      return Result.Fail($"Unexpected error toggling notification channel: {ex.Message}");
    }
  }

  public async Task<Result> EnsureNotificationChannelAsync(Person person, NotificationChannel channel, CancellationToken cancellationToken = default)
  {
    try
    {
      // Check if person already has a channel of this type
      NotificationChannel? existingChannel = person.NotificationChannels.FirstOrDefault(c => c.Type == channel.Type);
      if (existingChannel is not null)
      {

        var ch = (NotificationChannel)existingChannel;
        // If the identifier is the same, nothing to do
        if (ch.Identifier == channel.Identifier)
        {
          return Result.Ok();
        }

        var removeResult = await RemoveNotificationChannelAsync(person, ch, cancellationToken);
        return removeResult.IsFailed ? removeResult : await AddNotificationChannelAsync(person, channel, cancellationToken);
      }

      // No channel of this type exists yet: add the channel
      return await AddNotificationChannelAsync(person, channel, cancellationToken);
    }
    catch (OperationCanceledException)
    {
      throw;
    }
    catch (InvalidOperationException ex)
    {
      return Result.Fail($"Invalid operation ensuring notification channel: {ex.Message}");
    }
    catch (Exception ex)
    {
      return Result.Fail($"Unexpected error ensuring notification channel: {ex.Message}");
    }
  }
}
