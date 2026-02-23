namespace Caramel.Domain.Common.ValueObjects;

public readonly record struct UtcDateTime
{
  public DateTime Value { get; }

  public UtcDateTime(DateTime value)
  {
    Value = value.Kind switch
    {
      DateTimeKind.Utc => value,
      DateTimeKind.Unspecified => DateTime.SpecifyKind(value, DateTimeKind.Utc),
      DateTimeKind.Local => throw new NotImplementedException(),
      _ => value.ToUniversalTime()
    };
  }

  public static implicit operator DateTime(UtcDateTime utcDateTime)
  {
    return utcDateTime.Value;
  }
}
