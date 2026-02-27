using Caramel.Domain.Common.ValueObjects;
using Caramel.Domain.People.ValueObjects;

namespace Caramel.Domain.People.Models;

/// <summary>
/// Represents a person in the system with platform identity, access permissions, and notification preferences.
/// </summary>
public sealed record Person
{
  /// <summary>
  /// Gets the unique identifier for this person.
  /// </summary>
  public required PersonId Id { get; init; }

  /// <summary>
  /// Gets the platform-specific identifier linking this person to their platform account.
  /// </summary>
  public required PlatformId PlatformId { get; init; }

  /// <summary>
  /// Gets the person's username on their platform.
  /// </summary>
  public required Username Username { get; init; }

  /// <summary>
  /// Gets a value indicating whether this person has access to the system.
  /// </summary>
  public required HasAccess HasAccess { get; init; }

  /// <summary>
  /// Gets the person's timezone identifier for scheduling and time-related operations.
  /// </summary>
  public PersonTimeZoneId? TimeZoneId { get; init; }

  /// <summary>
  /// Gets the maximum number of tasks this person can have per day.
  /// </summary>
  public DailyTaskCount? DailyTaskCount { get; init; }

  /// <summary>
  /// Gets the collection of notification channels configured for this person.
  /// </summary>
  public ICollection<NotificationChannel> NotificationChannels { get; init; } = [];

  /// <summary>
  /// Gets the UTC timestamp when this person record was created.
  /// </summary>
  public required CreatedOn CreatedOn { get; init; }

  /// <summary>
  /// Gets the UTC timestamp when this person record was last updated.
  /// </summary>
  public required UpdatedOn UpdatedOn { get; init; }
}
