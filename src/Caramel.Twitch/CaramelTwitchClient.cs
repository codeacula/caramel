using TwitchLib.Api;
using TwitchLib.Client;
using TwitchLib.Client.Events;
using TwitchLib.Client.Models;
using TwitchLib.Communication.Clients;

namespace Caramel.Twitch;

public class CaramelTwitchClient
{
    private readonly CaramelTwitchClientCredentials _credentials;
    private readonly TwitchAPI _twitchApi;
    private TwitchClient _twitchClient;

    public CaramelTwitchClient(CaramelTwitchClientCredentials credentials)
    {
        _credentials = credentials;
        _twitchApi = new TwitchAPI();
        _twitchApi.Settings.ClientId = _credentials.ClientId;
        _twitchApi.Settings.Secret = _credentials.ClientSecret;

        var websocketClient = new WebSocketClient();
        _twitchClient = new TwitchClient(websocketClient);
    }

    public async Task ConnectAsync()
    {
        var accessToken = await _twitchApi.Auth.GetAccessTokenAsync();
        _twitchClient.Initialize(new ConnectionCredentials(_credentials.Username, accessToken), _credentials.Username);

        _twitchClient.OnMessageReceived += OnMessageReceived;

        _twitchClient.Connect();
    }

    private void OnMessageReceived(object? sender, OnMessageReceivedArgs e)
    {
        Console.WriteLine($"Message received from {e.ChatMessage.Username}: {e.ChatMessage.Message}");
    }
}