using TwitchLib.EventSub.Core;
using TwitchLib.EventSub.Core.EventArgs.Channel;
using TwitchLib.EventSub.Core.EventArgs.User;

namespace Caramel.Twitch.Services;

internal interface IEventSubWebsocketClientWrapper
{
  string SessionId { get; }

  event AsyncEventHandler<WebsocketConnectedArgs> WebsocketConnected;
  event AsyncEventHandler<WebsocketDisconnectedArgs> WebsocketDisconnected;
  event AsyncEventHandler<WebsocketReconnectedArgs> WebsocketReconnected;
  event AsyncEventHandler<ChannelChatMessageArgs> ChannelChatMessage;
  event AsyncEventHandler<UserWhisperMessageArgs> UserWhisperMessage;
  event AsyncEventHandler<ChannelPointsCustomRewardRedemptionArgs> ChannelPointsCustomRewardRedemptionAdd;

  Task<bool> ConnectAsync();
  Task<bool> ReconnectAsync();
}
