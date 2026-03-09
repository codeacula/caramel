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
  /// Optional at startup because runtime tokens may be provided later through the UI.
  /// Sensitive data - never log or expose in output.
  /// </summary>
  [StringLength(500, MinimumLength = 10, ErrorMessage = "AccessToken must be between 10 and 500 characters")]
  public string AccessToken { get; set; } = string.Empty;

  /// <summary>
  /// OAuth refresh token for obtaining new access tokens.
  /// Used to refresh the access token when it expires.
  /// Optional at startup because runtime tokens may be provided later through the UI.
  /// Sensitive data - never log or expose in output.
  /// </summary>
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
  /// This may be provisioned later through the UI-driven setup flow.
  /// </summary>
  [StringLength(100, MinimumLength = 1, ErrorMessage = "BotUserId must be between 1 and 100 characters")]
  public string BotUserId { get; set; } = string.Empty;

  /// <summary>
  /// Comma-separated list of Twitch channel IDs to monitor.
  /// This may be provisioned later through the UI-driven setup flow.
  /// </summary>
  public string ChannelIds { get; set; } = string.Empty;

  /// <summary>
  /// Optional Twitch custom reward ID for "Message The AI" channel point redemption.
  /// If set, enables the feature. If empty, feature is disabled.
  /// </summary>
  public string? MessageTheAiRewardId { get; set; }

  public override IEnumerable<string> Validate()
  {
    var errors = new List<string>();

    // Use data annotations validation for required fields and values that are present.
    // Optional runtime fields are validated manually below so empty values can be skipped.
    var context = new ValidationContext(this);
    var results = new List<ValidationResult>();
    if (!Validator.TryValidateObject(this, context, results, validateAllProperties: false))
    {
      errors.AddRange(results.Select(r => r.ErrorMessage ?? "Unknown validation error"));
    }

    if (!string.IsNullOrWhiteSpace(AccessToken) && (AccessToken.Length < 10 || AccessToken.Length > 500))
    {
      errors.Add("AccessToken must be between 10 and 500 characters");
    }

    if (!string.IsNullOrWhiteSpace(RefreshToken) && (RefreshToken.Length < 10 || RefreshToken.Length > 500))
    {
      errors.Add("RefreshToken must be between 10 and 500 characters");
    }

    if (!string.IsNullOrWhiteSpace(BotUserId) && (BotUserId.Length < 1 || BotUserId.Length > 100))
    {
      errors.Add("BotUserId must be between 1 and 100 characters");
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

    // Validate channel IDs format when runtime setup has provided them
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
    return $"TwitchConfig {{ ClientId = {ClientId}, BotUserId = {(string.IsNullOrEmpty(BotUserId) ? "(runtime)" : BotUserId)}, " +
           $"ChannelCount = {(string.IsNullOrEmpty(ChannelIds) ? 0 : ChannelIds.Split(',').Length)}, " +
           $"MessageTheAiRewardId = {(string.IsNullOrEmpty(MessageTheAiRewardId) ? "(disabled)" : "***")} }}";
  }

  public override bool IsRequired => true;
}
