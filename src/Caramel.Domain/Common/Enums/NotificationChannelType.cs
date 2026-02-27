namespace Caramel.Domain.Common.Enums;

/// <summary>
/// Represents the type of notification channel through which messages can be sent to a person.
/// </summary>
public enum NotificationChannelType
{
  /// <summary>Discord direct message or channel notification.</summary>
  Discord = 0,

  /// <summary>Email notification.</summary>
  Email = 1,

  /// <summary>Push notification to a mobile or web client.</summary>
  Push = 2,

  /// <summary>Twitch chat or notification.</summary>
  Twitch = 3
}
