using System.ComponentModel.DataAnnotations;

namespace Caramel.Core.Configuration;

/// <summary>
/// Configuration options for Twitch integration.
/// Configures OAuth credentials and Twitch API settings for bot and notification services.
/// </summary>
public sealed class TwitchConfigOptions : ConfigurationOptions
{
  /// <summary>
  /// Configuration section name in appsettings.json
  /// </summary>
  public const string SectionName = "TwitchConfig";

  /// <summary>
  /// Current OAuth access token for Twitch API authentication.
  /// Obtained through OAuth flow and is time-limited.
  /// Sensitive data - never log or expose in output.
  /// </summary>
  [Required(ErrorMessage = "AccessToken is required")]
  [StringLength(500, MinimumLength = 10, ErrorMessage = "AccessToken must be between 10 and 500 characters")]
  public string AccessToken { get; set; } = string.Empty;

  /// <summary>
  /// OAuth refresh token for obtaining new access tokens.
  /// Used to refresh the access token when it expires.
  /// Sensitive data - never log or expose in output.
  /// </summary>
  [Required(ErrorMessage = "RefreshToken is required")]
  [StringLength(500, MinimumLength = 10, ErrorMessage = "RefreshToken must be between 10 and 500 characters")]
  public string RefreshToken { get; set; } = string.Empty;

  /// <summary>
  /// Twitch application client ID from developer console.
  /// Obtained from https://dev.twitch.tv/console/apps
  /// </summary>
  [Required(ErrorMessage = "ClientId is required")]
  [StringLength(100, MinimumLength = 10, ErrorMessage = "ClientId must be between 10 and 100 characters")]
  public string ClientId { get; set; } = string.Empty;

  /// <summary>
  /// Twitch application client secret from developer console.
  /// Used for OAuth token validation and refresh.
  /// Sensitive data - never log or expose in output.
  /// </summary>
  [Required(ErrorMessage = "ClientSecret is required")]
  [StringLength(200, MinimumLength = 10, ErrorMessage = "ClientSecret must be between 10 and 200 characters")]
  public string ClientSecret { get; set; } = string.Empty;

  /// <summary>
  /// Encryption key for securing sensitive session data.
  /// Should be a cryptographically secure random string.
  /// Sensitive data - never log or expose in output.
  /// </summary>
  [Required(ErrorMessage = "EncryptionKey is required")]
  [StringLength(256, MinimumLength = 32, ErrorMessage = "EncryptionKey must be between 32 and 256 characters")]
  public string EncryptionKey { get; set; } = string.Empty;

  /// <summary>
  /// OAuth callback URL for the Twitch OAuth flow.
  /// Must match the redirect URI configured in Twitch Developer Console.
  /// </summary>
  [Required(ErrorMessage = "OAuthCallbackUrl is required")]
  [Url(ErrorMessage = "OAuthCallbackUrl must be a valid URL")]
  public string OAuthCallbackUrl { get; set; } = string.Empty;

  /// <summary>
  /// User ID of the bot account for Twitch notifications.
  /// This bot is used to send notifications and interact with chat.
  /// </summary>
  [Required(ErrorMessage = "BotUserId is required")]
  [StringLength(100, MinimumLength = 1, ErrorMessage = "BotUserId must be between 1 and 100 characters")]
  public string BotUserId { get; set; } = string.Empty;

  /// <summary>
  /// Comma-separated list of Twitch channel IDs to monitor.
  /// Used to configure which channels the bot listens to.
  /// </summary>
  [Required(ErrorMessage = "ChannelIds is required")]
  public string ChannelIds { get; set; } = string.Empty;

  /// <summary>
  /// Optional Twitch custom reward ID for "Message The AI" channel point redemption.
  /// If set, enables the feature. If empty, feature is disabled.
  /// </summary>
  public string? MessageTheAiRewardId { get; set; }

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

    // Custom validation for callback URL
    if (!string.IsNullOrWhiteSpace(OAuthCallbackUrl))
    {
      if (!Uri.TryCreate(OAuthCallbackUrl, UriKind.Absolute, out var uri))
      {
        errors.Add("OAuthCallbackUrl must be a valid absolute URI");
      }
      else if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
      {
        errors.Add("OAuthCallbackUrl must use HTTP or HTTPS protocol");
      }

      // Callback URL should not end with trailing slash
      if (OAuthCallbackUrl.EndsWith('/'))
      {
        errors.Add("OAuthCallbackUrl should not end with a trailing slash");
      }
    }

    // Validate channel IDs format
    if (!string.IsNullOrWhiteSpace(ChannelIds))
    {
      var channelList = ChannelIds.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
      if (channelList.Length == 0)
      {
        errors.Add("ChannelIds must contain at least one channel ID");
      }

      // Basic validation: channel IDs should be alphanumeric or numeric
      foreach (var channelId in channelList)
      {
        if (string.IsNullOrWhiteSpace(channelId) || !channelId.All(c => char.IsLetterOrDigit(c) || c == '_'))
        {
          errors.Add($"Invalid channel ID format: '{channelId}'");
        }
      }
    }

    return errors;
  }

  public override string ToString()
  {
    return $"TwitchConfig {{ ClientId = {ClientId}, BotUserId = {BotUserId}, " +
           $"ChannelCount = {(string.IsNullOrEmpty(ChannelIds) ? 0 : ChannelIds.Split(',').Length)}, " +
           $"MessageTheAiRewardId = {(string.IsNullOrEmpty(MessageTheAiRewardId) ? "(disabled)" : "***")} }}";
  }

  public override bool IsRequired => true;
}
