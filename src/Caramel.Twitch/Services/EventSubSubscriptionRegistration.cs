namespace Caramel.Twitch.Services;

internal sealed record EventSubSubscriptionRegistrationContext(
  HttpClient HttpClient,
  string SessionId,
  string BotUserId,
  string? BroadcasterUserId,
  IReadOnlyList<string> ChannelUserIds,
  string BotAccessToken,
  string? BroadcasterAccessToken);

internal interface IEventSubSubscriptionRegistrar
{
  Task RegisterAsync(EventSubSubscriptionRegistrationContext context, CancellationToken cancellationToken);
}

internal interface IEventSubSubscriptionClient
{
  Task CreateSubscriptionAsync(
    HttpClient httpClient,
    string sessionId,
    string eventType,
    string version,
    IReadOnlyDictionary<string, string> condition,
    CancellationToken cancellationToken);
}

internal sealed class EventSubSubscriptionClient(
  ILogger<EventSubSubscriptionClient> logger) : IEventSubSubscriptionClient
{
  public async Task CreateSubscriptionAsync(
    HttpClient httpClient,
    string sessionId,
    string eventType,
    string version,
    IReadOnlyDictionary<string, string> condition,
    CancellationToken cancellationToken)
  {
    var conditionDisplay = string.Join(", ", condition.Select(kvp => $"{kvp.Key}={kvp.Value}"));

    try
    {
      var body = new
      {
        type = eventType,
        version,
        condition,
        transport = new
        {
          method = "websocket",
          session_id = sessionId,
        },
      };

      var json = JsonSerializer.Serialize(body);
      using var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
      var response = await httpClient.PostAsync("https://api.twitch.tv/helix/eventsub/subscriptions", content, cancellationToken);

      if (response.IsSuccessStatusCode)
      {
        CaramelTwitchProgramLogs.EventSubSubscribed(logger, eventType, conditionDisplay);
      }
      else
      {
        var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
        CaramelTwitchProgramLogs.EventSubSubscriptionFailed(logger, eventType, conditionDisplay, $"{(int)response.StatusCode}: {errorBody}");
      }
    }
    catch (OperationCanceledException)
    {
      throw;
    }
    catch (HttpRequestException ex)
    {
      CaramelTwitchProgramLogs.EventSubSubscriptionFailed(logger, eventType, conditionDisplay, $"Network error: {ex.Message}");
    }
    catch (InvalidOperationException ex)
    {
      CaramelTwitchProgramLogs.EventSubSubscriptionFailed(logger, eventType, conditionDisplay, $"Invalid state: {ex.Message}");
    }
    catch (Exception ex)
    {
      CaramelTwitchProgramLogs.EventSubSubscriptionFailed(logger, eventType, conditionDisplay, ex.Message);
    }
  }
}
