namespace Caramel.Twitch;

public sealed record CaramelTwitchClientCredentials
{
    public string Username { get; init; } = string.Empty;
    public string ClientId { get; init; } = string.Empty;
    public string ClientSecret { get; init; } = string.Empty;
}
