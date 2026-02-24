namespace Caramel.Twitch.Loggers;

internal static partial class AuthControllerLogs
{
  [LoggerMessage(Level = LogLevel.Information, Message = "Auth login endpoint requested")]
  public static partial void LoginRequested(ILogger logger);
}
