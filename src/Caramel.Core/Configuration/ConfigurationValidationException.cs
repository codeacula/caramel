namespace Caramel.Core.Configuration;

/// <summary>
/// Exception thrown when configuration validation fails during application startup.
/// This helps identify missing or invalid configuration values before the application runs.
/// </summary>
public sealed class ConfigurationValidationException : Exception
{
  /// <summary>
  /// Initializes a new instance of the <see cref="ConfigurationValidationException"/> class.
  /// </summary>
  /// <param name="message">The error message that describes the validation failure.</param>
  public ConfigurationValidationException(string message) : base(message) { }

  /// <summary>
  /// Initializes a new instance of the <see cref="ConfigurationValidationException"/> class.
  /// </summary>
  /// <param name="message">The error message that describes the validation failure.</param>
  /// <param name="innerException">The exception that is the cause of the current exception.</param>
  public ConfigurationValidationException(string message, Exception innerException)
    : base(message, innerException) { }

  /// <summary>
  /// Creates a ConfigurationValidationException from multiple validation errors.
  /// </summary>
  /// <param name="configSectionName">The name of the configuration section that failed validation.</param>
  /// <param name="errors">Collection of validation error messages.</param>
  /// <returns>A new ConfigurationValidationException with formatted error messages.</returns>
  public static ConfigurationValidationException CreateFromErrors(
    string configSectionName,
    IEnumerable<string> errors)
  {
    var errorList = string.Join("\n  - ", errors);
    var message = $"Configuration validation failed for '{configSectionName}':\n  - {errorList}";
    return new ConfigurationValidationException(message);
  }
}
