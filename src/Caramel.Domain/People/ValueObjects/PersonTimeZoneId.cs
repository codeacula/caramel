namespace Caramel.Domain.People.ValueObjects;

/// <summary>
/// Represents a person's timezone for scheduling and time-aware operations.
/// Supports IANA timezone IDs and common timezone abbreviations.
/// </summary>
public readonly record struct PersonTimeZoneId
{
  private static readonly Dictionary<string, string> CommonAbbreviations = new(StringComparer.OrdinalIgnoreCase)
  {
    // US Timezones (preferred for ambiguous cases)
    { "EST", "America/New_York" },
    { "EDT", "America/New_York" },
    { "Eastern", "America/New_York" },
    { "CST", "America/Chicago" },
    { "CDT", "America/Chicago" },
    { "Central", "America/Chicago" },
    { "MST", "America/Denver" },
    { "MDT", "America/Denver" },
    { "Mountain", "America/Denver" },
    { "PST", "America/Los_Angeles" },
    { "PDT", "America/Los_Angeles" },
    { "Pacific", "America/Los_Angeles" },
    // International Timezones
    { "GMT", "Europe/London" },
    { "UTC", "UTC" },
    { "BST", "Europe/London" },
    { "CET", "Europe/Paris" },
    { "CEST", "Europe/Paris" },
    { "JST", "Asia/Tokyo" },
    { "AEST", "Australia/Sydney" },
    { "AEDT", "Australia/Sydney" }
  };

  /// <summary>
  /// Gets the IANA timezone ID string (e.g., "America/New_York").
  /// </summary>
  public string Value { get; init; }

  private PersonTimeZoneId(string value)
  {
    Value = value;
  }

  /// <summary>
  /// Parses a timezone string into a PersonTimeZoneId, supporting both IANA IDs and common abbreviations.
  /// </summary>
  /// <param name="input">The timezone string to parse (e.g., "America/New_York" or "EST").</param>
  /// <param name="timeZoneId">The parsed timezone ID, if successful.</param>
  /// <param name="error">An error message if parsing failed; otherwise null.</param>
  /// <returns>True if parsing succeeded; false otherwise.</returns>
  public static bool TryParse(string input, out PersonTimeZoneId timeZoneId, out string? error)
  {
    timeZoneId = default;
    error = null;

    if (string.IsNullOrWhiteSpace(input))
    {
      error = "Timezone cannot be empty";
      return false;
    }

    var trimmedInput = input.Trim();

    // Try to map common abbreviations
    var timeZoneIdString = CommonAbbreviations.TryGetValue(trimmedInput, out var mapped)
      ? mapped
      : trimmedInput;

    // Validate the timezone ID exists
    try
    {
      _ = TimeZoneInfo.FindSystemTimeZoneById(timeZoneIdString);
      timeZoneId = new PersonTimeZoneId(timeZoneIdString);
      return true;
    }
    catch (TimeZoneNotFoundException)
    {
      error = $"Timezone '{input}' is not recognized. Please use a valid IANA timezone ID (e.g., 'America/New_York') or common abbreviation (e.g., 'EST', 'CST', 'PST')";
      return false;
    }
    catch (InvalidTimeZoneException)
    {
      error = $"Timezone '{input}' is invalid";
      return false;
    }
  }

  /// <summary>
  /// Implicitly converts a PersonTimeZoneId to its IANA timezone string.
  /// </summary>
  /// <param name="value">The timezone ID to convert.</param>
  /// <returns>The IANA timezone string.</returns>
  public static implicit operator string(PersonTimeZoneId value)
  {
    return value.Value;
  }

  /// <summary>
  /// Gets the system TimeZoneInfo for this timezone.
  /// </summary>
  /// <returns>The TimeZoneInfo object representing this timezone.</returns>
  public TimeZoneInfo GetTimeZoneInfo()
  {
    return TimeZoneInfo.FindSystemTimeZoneById(Value);
  }

  /// <summary>
  /// Gets the display name for this timezone.
  /// </summary>
  /// <returns>The localized display name of the timezone.</returns>
  public string GetDisplayName()
  {
    return GetTimeZoneInfo().DisplayName;
  }
}
