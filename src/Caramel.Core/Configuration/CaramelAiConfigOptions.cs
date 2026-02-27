using System.ComponentModel.DataAnnotations;

namespace Caramel.Core.Configuration;

/// <summary>
/// Configuration options for Caramel AI integration.
/// Configures connection to AI service for model inference and prompt processing.
/// </summary>
public sealed class CaramelAiConfigOptions : ConfigurationOptions
{
  /// <summary>
  /// Configuration section name in appsettings.json
  /// </summary>
  public const string SectionName = "CaramelAiConfig";

  /// <summary>
  /// The model ID or name to use for AI inference (e.g., "gpt-4", "claude-3").
  /// Required for AI functionality.
  /// </summary>
  [Required(ErrorMessage = "ModelId is required")]
  public string ModelId { get; set; } = string.Empty;

  /// <summary>
  /// The API endpoint URL for the AI service.
  /// Must be a valid HTTPS URL for production environments.
  /// </summary>
  [Required(ErrorMessage = "Endpoint is required")]
  [Url(ErrorMessage = "Endpoint must be a valid URL")]
  public string Endpoint { get; set; } = string.Empty;

  /// <summary>
  /// API key for authenticating with the AI service.
  /// Sensitive data - never log or expose in output.
  /// </summary>
  [Required(ErrorMessage = "ApiKey is required")]
  [StringLength(1000, MinimumLength = 10, ErrorMessage = "ApiKey must be between 10 and 1000 characters")]
  public string ApiKey { get; set; } = string.Empty;

  /// <summary>
  /// System prompt for AI context and behavior.
  /// Used to guide AI responses in a consistent manner.
  /// Optional - if not provided, a default system prompt may be used.
  /// </summary>
  public string? SystemPrompt { get; set; }

  /// <summary>
  /// Maximum number of tokens to use per request.
  /// Helps control costs and response times.
  /// Default is 1000 if not specified.
  /// </summary>
  [Range(1, 10000, ErrorMessage = "MaxTokens must be between 1 and 10000")]
  public int? MaxTokens { get; set; }

  /// <summary>
  /// Temperature for AI response generation (controls creativity).
  /// Valid range is 0.0 to 2.0. Default is 0.7 if not specified.
  /// </summary>
  [Range(0.0, 2.0, ErrorMessage = "Temperature must be between 0.0 and 2.0")]
  public double? Temperature { get; set; }

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

    // Custom validation rules
    if (!string.IsNullOrWhiteSpace(Endpoint))
    {
      if (!Uri.TryCreate(Endpoint, UriKind.Absolute, out var uri))
      {
        errors.Add("Endpoint must be a valid absolute URI");
      }
      else if (uri.Scheme != Uri.UriSchemeHttps && uri.Scheme != Uri.UriSchemeHttp)
      {
        errors.Add("Endpoint must use HTTP or HTTPS protocol");
      }
    }

    return errors;
  }

  public override string ToString()
  {
    return $"CaramelAiConfig {{ ModelId = {ModelId}, Endpoint = {Endpoint}, " +
           $"SystemPrompt = {(string.IsNullOrEmpty(SystemPrompt) ? "(none)" : "***")}, " +
           $"MaxTokens = {MaxTokens}, Temperature = {Temperature} }}";
  }

  public override bool IsRequired => true;
}
