using System.Text;

using Caramel.Twitch.Auth;

namespace Caramel.Twitch.Services;

public interface ITwitchWhisperService
{
  Task<bool> SendWhisperAsync(string botUserId, string recipientUserId, string message, CancellationToken cancellationToken = default);
}

public sealed class TwitchWhisperService(
  IHttpClientFactory httpClientFactory,
  TwitchConfig twitchConfig,
  IDualOAuthTokenManager tokenManager,
  ILogger<TwitchWhisperService> logger) : ITwitchWhisperService
{
  public async Task<bool> SendWhisperAsync(
    string botUserId,
    string recipientUserId,
    string message,
    CancellationToken cancellationToken = default)
  {
    try
    {
      var accessToken = await tokenManager.GetValidBotTokenAsync(cancellationToken);

      using var httpClient = httpClientFactory.CreateClient("TwitchHelix");
      httpClient.DefaultRequestHeaders.Add("Client-Id", twitchConfig.ClientId);
      httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {accessToken}");

      var url = $"https://api.twitch.tv/helix/whispers?from_user_id={Uri.EscapeDataString(botUserId)}&to_user_id={Uri.EscapeDataString(recipientUserId)}";

      var body = new { message };
      var json = JsonSerializer.Serialize(body);
      using var content = new StringContent(json, Encoding.UTF8, "application/json");

      var response = await httpClient.PostAsync(url, content, cancellationToken);

      if (response.IsSuccessStatusCode)
      {
        TwitchWhisperServiceLogs.WhisperSent(logger, botUserId, recipientUserId);
        return true;
      }

      var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
      TwitchWhisperServiceLogs.WhisperSendFailed(logger, recipientUserId, (int)response.StatusCode, errorBody);
      return false;
    }
    catch (OperationCanceledException)
    {
      throw;
    }
    catch (HttpRequestException ex)
    {
      TwitchWhisperServiceLogs.WhisperSendError(logger, recipientUserId, $"Network error: {ex.Message}");
      return false;
    }
    catch (InvalidOperationException ex)
    {
      TwitchWhisperServiceLogs.WhisperSendError(logger, recipientUserId, $"Invalid state: {ex.Message}");
      return false;
    }
    catch (Exception ex)
    {
      TwitchWhisperServiceLogs.WhisperSendError(logger, recipientUserId, ex.Message);
      return false;
    }
  }
}

internal static partial class TwitchWhisperServiceLogs
{
  [LoggerMessage(Level = LogLevel.Information, Message = "Whisper sent from {BotUserId} to {RecipientUserId}")]
  public static partial void WhisperSent(ILogger logger, string botUserId, string recipientUserId);

  [LoggerMessage(Level = LogLevel.Warning, Message = "Twitch API rejected whisper to {RecipientUserId} with status {StatusCode}: {Error}")]
  public static partial void WhisperSendFailed(ILogger logger, string recipientUserId, int statusCode, string error);

  [LoggerMessage(Level = LogLevel.Error, Message = "Error sending whisper to {RecipientUserId}: {Error}")]
  public static partial void WhisperSendError(ILogger logger, string recipientUserId, string error);
}
