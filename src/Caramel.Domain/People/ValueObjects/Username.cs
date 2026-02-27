namespace Caramel.Domain.People.ValueObjects;

/// <summary>
/// Represents a person's username on a platform.
/// </summary>
public readonly record struct Username(string Value)
{
  /// <summary>
  /// Gets a value indicating whether this username is valid (not null or whitespace).
  /// </summary>
  public bool IsValid => !string.IsNullOrWhiteSpace(Value);

  /// <summary>
  /// Implicitly converts a Username to its string value.
  /// </summary>
  /// <param name="username">The username to convert.</param>
  /// <returns>The string value of the username.</returns>
  public static implicit operator string(Username username)
  {
    return username.Value;
  }
}
