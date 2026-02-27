namespace Caramel.Domain.Common.ValueObjects;

/// <summary>
/// Represents a DateTime that is guaranteed to be in UTC.
/// Automatically converts local or unspecified datetimes to UTC.
/// </summary>
public readonly record struct UtcDateTime
{
  /// <summary>
  /// Gets the UTC DateTime value.
  /// </summary>
  public DateTime Value { get; }

  /// <summary>
  /// Initializes a new instance of the UtcDateTime struct, ensuring the datetime is in UTC.
  /// </summary>
  /// <param name="value">The datetime to convert to UTC if needed.</param>
  public UtcDateTime(DateTime value)
  {
    Value = value.Kind switch
    {
      DateTimeKind.Utc => value,
      DateTimeKind.Unspecified => DateTime.SpecifyKind(value, DateTimeKind.Utc),
      DateTimeKind.Local => value.ToUniversalTime(),
      _ => value.ToUniversalTime()
    };
  }

  /// <summary>
  /// Implicitly converts a UtcDateTime to a DateTime in UTC.
  /// </summary>
  /// <param name="utcDateTime">The UtcDateTime to convert.</param>
  /// <returns>The underlying UTC DateTime value.</returns>
  public static implicit operator DateTime(UtcDateTime utcDateTime)
  {
    return utcDateTime.Value;
  }
}
