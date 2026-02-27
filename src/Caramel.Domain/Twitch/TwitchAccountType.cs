namespace Caramel.Domain.Twitch;

/// <summary>
/// Identifies which Twitch account type a token or authentication belongs to.
/// </summary>
public enum TwitchAccountType
{
    /// <summary>
    /// The bot account — used for sending messages, reading chat, managing whispers.
    /// Scopes: user:bot, user:read:chat, user:write:chat, user:manage:whispers, chat:read, chat:edit, whispers:read, whispers:edit
    /// </summary>
    Bot = 0,

    /// <summary>
    /// The broadcaster/channel owner account — used for channel features, moderator actions, redemptions.
    /// Scopes: channel:read:redemptions, channel:edit:commercial, moderator:manage:banned_users, moderator:manage:chat_messages, channel:moderate
    /// </summary>
    Broadcaster = 1,
}
