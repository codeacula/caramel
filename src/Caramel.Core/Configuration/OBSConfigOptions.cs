using System.ComponentModel.DataAnnotations;

namespace Caramel.Core.Configuration;

/// <summary>
/// Configuration options for OBS (Open Broadcaster Software) integration.
/// Configures WebSocket connection to OBS for controlling streaming settings and scenes.
/// </summary>
public sealed class OBSConfigOptions : ConfigurationOptions
{
  /// <summary>
  /// Configuration section name in appsettings.json
  /// </summary>
  public const string SectionName = "ObsConfig";

  /// <summary>
  /// WebSocket URL for connecting to OBS.
  /// Format: ws://hostname:port or wss://hostname:port for secure connections.
  /// Example: ws://localhost:4455
  /// Validation: Must use 'ws' or 'wss' protocol (custom validation in Validate() method)
  /// </summary>
  [Required(ErrorMessage = "Url is required")]
  public string Url { get; set; } = string.Empty;

  /// <summary>
  /// WebSocket password configured in OBS settings.
  /// Required for authentication with OBS WebSocket server.
  /// Sensitive data - never log or expose in output.
  /// </summary>
  [Required(ErrorMessage = "Password is required")]
  [StringLength(256, MinimumLength = 1, ErrorMessage = "Password must be between 1 and 256 characters")]
  public string Password { get; set; } = string.Empty;

  /// <summary>
  /// Connection timeout in seconds for WebSocket operations.
  /// Default is 10 seconds if not specified.
  /// </summary>
  [Range(1, 120, ErrorMessage = "ConnectionTimeoutSeconds must be between 1 and 120")]
  public int ConnectionTimeoutSeconds { get; set; } = 10;

  /// <summary>
  /// Whether to automatically reconnect if the WebSocket connection is lost.
  /// Default is true.
  /// </summary>
  public bool AutoReconnect { get; set; } = true;

  /// <summary>
  /// Maximum number of reconnection attempts before giving up.
  /// Default is 5 if not specified. Only used if AutoReconnect is true.
  /// </summary>
  [Range(1, 100, ErrorMessage = "MaxReconnectAttempts must be between 1 and 100")]
  public int MaxReconnectAttempts { get; set; } = 5;

  public override IEnumerable<string> Validate()
  {
    var errors = new List<string>();

    // Use data annotations validation
    var context = new ValidationContext(this);
    var results = new List<ValidationResult>();
    if (!Validator.TryValidateObject(this, context, results, validateAllProperties: true))
    {
      errors.AddRange(results.Select(r => r.ErrorMessage ?? "Unknown validation error"));
    }

    // Custom validation for WebSocket URL
    if (!string.IsNullOrWhiteSpace(Url))
    {
      if (!Uri.TryCreate(Url, UriKind.Absolute, out var uri))
      {
        errors.Add("Url must be a valid absolute URI");
      }
      else if (uri.Scheme != "ws" && uri.Scheme != "wss")
      {
        errors.Add("Url must use 'ws' or 'wss' (WebSocket) protocol, not HTTP/HTTPS");
      }
    }

    return errors;
  }

  public override string ToString()
  {
    return $"ObsConfig {{ Url = {Url}, Password = ***, " +
           $"ConnectionTimeoutSeconds = {ConnectionTimeoutSeconds}, " +
           $"AutoReconnect = {AutoReconnect}, " +
           $"MaxReconnectAttempts = {MaxReconnectAttempts} }}";
  }

  /// <summary>
  /// OBS configuration is optional - functionality is disabled if not configured.
  /// </summary>
  public override bool IsRequired => false;
}
