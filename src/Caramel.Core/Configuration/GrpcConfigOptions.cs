using System.ComponentModel.DataAnnotations;

namespace Caramel.Core.Configuration;

/// <summary>
/// Configuration options for gRPC client and server connections.
/// Used by API, Discord, and Twitch services to communicate with Caramel.Service.
/// </summary>
public sealed class GrpcConfigOptions : ConfigurationOptions
{
  /// <summary>
  /// Configuration section name in appsettings.json
  /// </summary>
  public const string SectionName = "GrpcHostConfig";

  /// <summary>
  /// Hostname or IP address of the gRPC server.
  /// For Docker: use service name from compose.yaml (e.g., 'caramel-service')
  /// For local development: use 'localhost'
  /// </summary>
  [Required(ErrorMessage = "Host is required")]
  [StringLength(256, MinimumLength = 1, ErrorMessage = "Host must be between 1 and 256 characters")]
  public string Host { get; set; } = string.Empty;

  /// <summary>
  /// Port number for the gRPC server.
  /// Standard gRPC port is 5270, but this can be customized.
  /// </summary>
  [Required(ErrorMessage = "Port is required")]
  [Range(1, 65535, ErrorMessage = "Port must be between 1 and 65535")]
  public int Port { get; set; }

  /// <summary>
  /// API token for authenticating gRPC requests.
  /// Used to verify that requests come from authorized clients.
  /// Sensitive data - never log or expose in output.
  /// </summary>
  [Required(ErrorMessage = "ApiToken is required")]
  [StringLength(500, MinimumLength = 10, ErrorMessage = "ApiToken must be between 10 and 500 characters")]
  public string ApiToken { get; set; } = string.Empty;

  /// <summary>
  /// Whether to use HTTPS (TLS) for gRPC connections.
  /// Should be true in production environments.
  /// </summary>
  public bool UseHttps { get; set; } = true;

  /// <summary>
  /// Whether to validate SSL certificates when using HTTPS.
  /// Set to false only for development with self-signed certificates.
  /// Never set to false in production.
  /// </summary>
  public bool ValidateSslCertificate { get; set; } = true;

  /// <summary>
  /// Connection timeout in seconds for gRPC operations.
  /// Default is 30 seconds if not specified.
  /// </summary>
  [Range(1, 300, ErrorMessage = "ConnectionTimeoutSeconds must be between 1 and 300")]
  public int ConnectionTimeoutSeconds { get; set; } = 30;

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

    // Custom validation: warn about SSL validation disabled in production
    if (!ValidateSslCertificate && !Host.Equals("localhost", StringComparison.OrdinalIgnoreCase))
    {
      errors.Add("ValidateSslCertificate should not be disabled for non-localhost hosts");
    }

    // Validate host is not empty or whitespace
    if (string.IsNullOrWhiteSpace(Host))
    {
      errors.Add("Host cannot be empty or whitespace");
    }

    return errors;
  }

  public override string ToString()
  {
    var protocol = UseHttps ? "https" : "http";
    return $"GrpcConfig {{ Url = {protocol}://{Host}:{Port}, " +
           $"ValidateSslCertificate = {ValidateSslCertificate}, " +
           $"ConnectionTimeoutSeconds = {ConnectionTimeoutSeconds} }}";
  }

  public override bool IsRequired => true;
}
