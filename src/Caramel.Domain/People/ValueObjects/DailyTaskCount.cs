namespace Caramel.Domain.People.ValueObjects;

/// <summary>
/// Represents the maximum number of tasks a person can have per day.
/// Must be between 1 and 20.
/// </summary>
public readonly record struct DailyTaskCount
{
  /// <summary>
  /// Gets the daily task count value.
  /// </summary>
  public int Value { get; init; }

  /// <summary>
  /// Initializes a new instance of the DailyTaskCount struct.
  /// </summary>
  /// <param name="value">The daily task count value.</param>
  public DailyTaskCount(int value)
  {
    Value = value;
  }

  /// <summary>
  /// Parses an integer into a DailyTaskCount, validating that it falls within the allowed range (1-20).
  /// </summary>
  /// <param name="count">The count value to parse.</param>
  /// <param name="result">The parsed DailyTaskCount, if successful.</param>
  /// <param name="error">An error message if parsing failed; otherwise null.</param>
  /// <returns>True if parsing succeeded; false if count is outside the valid range.</returns>
  public static bool TryParse(int count, out DailyTaskCount result, out string? error)
  {
    error = null;
    result = default;

    if (count is < 1 or > 20)
    {
      error = "Daily task count must be between 1 and 20";
      return false;
    }

    result = new DailyTaskCount(count);
    return true;
  }
}
