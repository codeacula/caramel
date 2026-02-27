namespace Caramel.Core.Configuration;

/// <summary>
/// Base class for all configuration option classes using the IOptions&lt;T&gt; pattern.
/// Provides common validation infrastructure for application configuration.
/// </summary>
public abstract class ConfigurationOptions
{
  /// <summary>
  /// Validates the configuration options and returns any errors found.
  /// Override this method in derived classes to implement custom validation logic.
  /// </summary>
  /// <returns>A collection of validation error messages. Empty if valid.</returns>
  public virtual IEnumerable<string> Validate()
  {
    return Enumerable.Empty<string>();
  }

  /// <summary>
  /// Returns a string representation of the configuration suitable for logging,
  /// with sensitive information (like API keys) masked out.
  /// </summary>
  /// <returns>A safe-to-log string representation of the configuration.</returns>
  public abstract override string ToString();

  /// <summary>
  /// Determines if this configuration section is required for the application to run.
  /// Default is true. Override to return false if the section is optional.
  /// </summary>
  public virtual bool IsRequired => true;
}
