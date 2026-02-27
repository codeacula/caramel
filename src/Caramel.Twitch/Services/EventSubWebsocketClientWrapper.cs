using TwitchLib.EventSub.Core;
using TwitchLib.EventSub.Core.EventArgs.Channel;
using TwitchLib.EventSub.Core.EventArgs.User;
using TwitchLib.EventSub.Websockets.Core.EventArgs;

namespace Caramel.Twitch.Services;

internal sealed class EventSubWebsocketClientWrapper(EventSubWebsocketClient client) : IEventSubWebsocketClientWrapper
{
  public string SessionId => client.SessionId;

  public event AsyncEventHandler<WebsocketConnectedArgs> WebsocketConnected
  {
    add => client.WebsocketConnected += value;
    remove => client.WebsocketConnected -= value;
  }

  public event AsyncEventHandler<WebsocketDisconnectedArgs> WebsocketDisconnected
  {
    add => client.WebsocketDisconnected += value;
    remove => client.WebsocketDisconnected -= value;
  }

  public event AsyncEventHandler<WebsocketReconnectedArgs> WebsocketReconnected
  {
    add => client.WebsocketReconnected += value;
    remove => client.WebsocketReconnected -= value;
  }

  public event AsyncEventHandler<ChannelChatMessageArgs> ChannelChatMessage
  {
    add => client.ChannelChatMessage += value;
    remove => client.ChannelChatMessage -= value;
  }

  public event AsyncEventHandler<UserWhisperMessageArgs> UserWhisperMessage
  {
    add => client.UserWhisperMessage += value;
    remove => client.UserWhisperMessage -= value;
  }

  public event AsyncEventHandler<ChannelPointsCustomRewardRedemptionArgs> ChannelPointsCustomRewardRedemptionAdd
  {
    add => client.ChannelPointsCustomRewardRedemptionAdd += value;
    remove => client.ChannelPointsCustomRewardRedemptionAdd -= value;
  }

  public Task<bool> ConnectAsync() => client.ConnectAsync();

  public Task<bool> ReconnectAsync() => client.ReconnectAsync();
}
