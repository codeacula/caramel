using System.ComponentModel.DataAnnotations;

namespace Caramel.Core.Configuration;

/// <summary>
/// Configuration options for database connections.
/// Supports both primary database and Quartz scheduler database connections.
/// </summary>
public sealed class DatabaseConfigOptions : ConfigurationOptions
{
  /// <summary>
  /// Configuration section name in appsettings.json
  /// </summary>
  public const string SectionName = "ConnectionStrings";

  /// <summary>
  /// Primary database connection string for application data.
  /// PostgreSQL connection string format:
  /// Host=localhost;Database=caramel_db;Username=user;Password=pass
  /// </summary>
  [Required(ErrorMessage = "Caramel connection string is required")]
  [StringLength(2000, MinimumLength = 10, ErrorMessage = "Caramel connection string must be between 10 and 2000 characters")]
  public string Caramel { get; set; } = string.Empty;

  /// <summary>
  /// Connection string for Quartz scheduler database.
  /// Stores job scheduling and execution data.
  /// Can be the same as the primary connection if sharing a database.
  /// </summary>
  [Required(ErrorMessage = "Quartz connection string is required")]
  [StringLength(2000, MinimumLength = 10, ErrorMessage = "Quartz connection string must be between 10 and 2000 characters")]
  public string Quartz { get; set; } = string.Empty;

  /// <summary>
  /// Redis connection string for caching and pub/sub operations.
  /// Format: localhost:6379 or with password: localhost:6379,password=secret
  /// </summary>
  [Required(ErrorMessage = "Redis connection string is required")]
  [StringLength(500, MinimumLength = 5, ErrorMessage = "Redis connection string must be between 5 and 500 characters")]
  public string Redis { get; set; } = string.Empty;

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

    // Custom validation for connection strings
    if (!string.IsNullOrWhiteSpace(Caramel))
    {
      errors.AddRange(ValidatePostgresConnectionString(Caramel, "Caramel"));
    }

    if (!string.IsNullOrWhiteSpace(Quartz))
    {
      errors.AddRange(ValidatePostgresConnectionString(Quartz, "Quartz"));
    }

    if (!string.IsNullOrWhiteSpace(Redis))
    {
      errors.AddRange(ValidateRedisConnectionString(Redis));
    }

    return errors;
  }

  /// <summary>
  /// Validates a PostgreSQL connection string format.
  /// </summary>
  private static IEnumerable<string> ValidatePostgresConnectionString(string connectionString, string name)
  {
    var errors = new List<string>();

    // Basic validation: should contain required components
    var lowerConnStr = connectionString.ToLower();
    if (!lowerConnStr.Contains("host") && !lowerConnStr.Contains("server"))
    {
      errors.Add($"{name} connection string must contain 'Host' parameter");
    }

    if (!lowerConnStr.Contains("database") && !lowerConnStr.Contains("db"))
    {
      errors.Add($"{name} connection string must contain 'Database' parameter");
    }

    if (!lowerConnStr.Contains("username") && !lowerConnStr.Contains("user"))
    {
      errors.Add($"{name} connection string must contain 'Username' parameter");
    }

    if (!lowerConnStr.Contains("password"))
    {
      errors.Add($"{name} connection string must contain 'Password' parameter");
    }

    return errors;
  }

  /// <summary>
  /// Validates a Redis connection string format.
  /// </summary>
  private static IEnumerable<string> ValidateRedisConnectionString(string connectionString)
  {
    var errors = new List<string>();

    // Basic validation: should be in format host:port
    if (!connectionString.Contains(":"))
    {
      errors.Add("Redis connection string must contain a port (format: host:port)");
    }

    // Try to extract host:port part
    var parts = connectionString.Split(',')[0].Split(':');
    if (parts.Length < 2)
    {
      errors.Add("Redis connection string must be in format: host:port");
    }
    else if (!int.TryParse(parts[parts.Length - 1], out var port) || port < 1 || port > 65535)
    {
      errors.Add("Redis connection string has invalid port number");
    }

    return errors;
  }

  public override string ToString()
  {
    return $"DatabaseConfig {{ Caramel = {MaskConnectionString(Caramel)}, " +
           $"Quartz = {MaskConnectionString(Quartz)}, " +
           $"Redis = {MaskConnectionString(Redis)} }}";
  }

  /// <summary>
  /// Masks sensitive information in a connection string for safe logging.
  /// </summary>
  private static string MaskConnectionString(string connectionString)
  {
    if (string.IsNullOrEmpty(connectionString))
      return string.Empty;

    try
    {
      // Simple approach: show host and database, hide password
      const string pattern = @"Password\s*=\s*[^;]*";
      return System.Text.RegularExpressions.Regex.Replace(connectionString, pattern, "Password=***", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
    }
    catch
    {
      return "***";
    }
  }

  public override bool IsRequired => true;
}
