using System.ComponentModel.DataAnnotations;

namespace Caramel.Core.Configuration;

/// <summary>
/// Configuration options for Discord bot integration.
/// Configures authentication and behavior for Discord.NET based bot.
/// </summary>
public sealed class DiscordConfigOptions : ConfigurationOptions
{
  /// <summary>
  /// Configuration section name in appsettings.json
  /// </summary>
  public const string SectionName = "DiscordConfig";

  /// <summary>
  /// Discord bot token for authentication.
  /// Obtained from Discord Developer Portal at https://discord.com/developers/applications
  /// Sensitive data - never log or expose in output.
  /// </summary>
  [Required(ErrorMessage = "Token is required")]
  [StringLength(500, MinimumLength = 10, ErrorMessage = "Token must be between 10 and 500 characters")]
  public string Token { get; set; } = string.Empty;

  /// <summary>
  /// Discord application public key for verifying interactions.
  /// Obtained from Discord Developer Portal and used for webhook signature verification.
  /// Required for interaction endpoints.
  /// </summary>
  [Required(ErrorMessage = "PublicKey is required")]
  [StringLength(256, MinimumLength = 32, ErrorMessage = "PublicKey must be between 32 and 256 characters")]
  public string PublicKey { get; set; } = string.Empty;

  /// <summary>
  /// Optional bot name for logging and identification purposes.
  /// If not provided, will be fetched from Discord API on startup.
  /// </summary>
  [StringLength(100, ErrorMessage = "BotName must not exceed 100 characters")]
  public string? BotName { get; set; }

  /// <summary>
  /// Intents that the bot will subscribe to.
  /// For example: "message_content,guild_messages,direct_messages"
  /// If not specified, a sensible default set of intents will be used.
  /// </summary>
  public string? Intents { get; set; }

  /// <summary>
  /// Command prefix for text-based commands (e.g., "!").
  /// Optional - interactions are preferred over prefix commands.
  /// </summary>
  [StringLength(5, MinimumLength = 1, ErrorMessage = "CommandPrefix must be 1-5 characters")]
  public string? CommandPrefix { get; set; }

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

    // Custom validation for token format (Discord tokens have a specific structure)
    if (!string.IsNullOrWhiteSpace(Token))
    {
      var parts = Token.Split('.');
      if (parts.Length != 3)
      {
        errors.Add("Token appears to be invalid (Discord tokens should have 3 parts separated by dots)");
      }
    }

    // Validate public key hex format
    if (!string.IsNullOrWhiteSpace(PublicKey))
    {
      if (!PublicKey.All(c => "0123456789abcdefABCDEF".Contains(c)))
      {
        errors.Add("PublicKey must be a valid hexadecimal string");
      }
    }

    return errors;
  }

  public override string ToString()
  {
    return $"DiscordConfig {{ Token = ***, PublicKey = ***, " +
           $"BotName = {(string.IsNullOrEmpty(BotName) ? "(unset)" : BotName)}, " +
           $"CommandPrefix = {(string.IsNullOrEmpty(CommandPrefix) ? "(none)" : CommandPrefix)} }}";
  }

  public override bool IsRequired => true;
}
