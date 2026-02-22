using Caramel.Domain.People.Models;
using Caramel.Domain.People.ValueObjects;

using FluentResults;

namespace Caramel.Core.People;

public interface IPersonStore
{
  Task<Result> AddNotificationChannelAsync(Person person, NotificationChannel channel, CancellationToken cancellationToken = default);
  Task<Result<Person>> CreateByPlatformIdAsync(PlatformId platformId, CancellationToken cancellationToken = default);
  Task<Result> EnsureNotificationChannelAsync(Person person, NotificationChannel channel, CancellationToken cancellationToken = default);
  Task<Result<HasAccess>> GetAccessAsync(PersonId id, CancellationToken cancellationToken = default);
  Task<Result<Person>> GetAsync(PersonId id, CancellationToken cancellationToken = default);
  Task<Result<Person>> GetByPlatformIdAsync(PlatformId platformId, CancellationToken cancellationToken = default);
  Task<Result> GrantAccessAsync(PersonId id, CancellationToken cancellationToken = default);
  Task<Result> RevokeAccessAsync(PersonId id, CancellationToken cancellationToken = default);
  Task<Result> RemoveNotificationChannelAsync(Person person, NotificationChannel channel, CancellationToken cancellationToken = default);
  Task<Result> SetDailyTaskCountAsync(PersonId id, DailyTaskCount dailyTaskCount, CancellationToken cancellationToken = default);
  Task<Result> SetTimeZoneAsync(PersonId id, PersonTimeZoneId timeZoneId, CancellationToken cancellationToken = default);
  Task<Result> ToggleNotificationChannelAsync(Person person, NotificationChannel channel, CancellationToken cancellationToken = default);
}
