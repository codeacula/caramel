using Caramel.Database.Twitch.Events;

using JasperFx.Events;

namespace Caramel.Database.Twitch;

public sealed record DbTwitchSetup
{
  /// <summary>
  /// Well-known identifier for the singleton Twitch setup stream.
  /// </summary>
  public static readonly Guid WellKnownId = new("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

  public required Guid Id { get; init; }

  public required string BotUserId { get; init; }
  public required string BotLogin { get; init; }

  public required List<DbTwitchChannel> Channels { get; init; }

  public required DateTime CreatedOn { get; init; }
  public required DateTime UpdatedOn { get; init; }

  // Token fields (encrypted in database)
  public string? BotAccessToken { get; init; }
  public string? BotRefreshToken { get; init; }
  public DateTime? BotTokenExpiresAt { get; init; }

  public string? BroadcasterUserId { get; init; }
  public string? BroadcasterLogin { get; init; }
  public string? BroadcasterAccessToken { get; init; }
  public string? BroadcasterRefreshToken { get; init; }
  public DateTime? BroadcasterTokenExpiresAt { get; init; }


  public static DbTwitchSetup Create(IEvent<TwitchSetupCreatedEvent> ev)
  {
    return new DbTwitchSetup
    {
      Id = WellKnownId,
      BotUserId = ev.Data.BotUserId,
      BotLogin = ev.Data.BotLogin,
      Channels = [.. ev.Data.Channels.Select(c => new DbTwitchChannel { UserId = c.UserId, Login = c.Login })],
      CreatedOn = ev.Data.CreatedOn,
      UpdatedOn = ev.Data.CreatedOn,
    };
  }

  public static DbTwitchSetup Apply(IEvent<TwitchSetupUpdatedEvent> ev, DbTwitchSetup current)
  {
    return current with
    {
      BotUserId = ev.Data.BotUserId,
      BotLogin = ev.Data.BotLogin,
      Channels = [.. ev.Data.Channels.Select(c => new DbTwitchChannel { UserId = c.UserId, Login = c.Login })],
      UpdatedOn = ev.Data.CreatedOn,
    };
  }

  public static DbTwitchSetup Apply(IEvent<TwitchBotTokensUpdatedEvent> ev, DbTwitchSetup current)
  {
    return current with
    {
      BotUserId = ev.Data.BotUserId,
      BotLogin = ev.Data.BotLogin,
      BotAccessToken = ev.Data.AccessToken,
      BotRefreshToken = ev.Data.RefreshToken,
      BotTokenExpiresAt = ev.Data.ExpiresAt,
      UpdatedOn = DateTime.UtcNow,
    };
  }

  public static DbTwitchSetup Apply(IEvent<TwitchBroadcasterTokensUpdatedEvent> ev, DbTwitchSetup current)
  {
    return current with
    {
      BroadcasterUserId = ev.Data.BroadcasterUserId,
      BroadcasterLogin = ev.Data.BroadcasterLogin,
      BroadcasterAccessToken = ev.Data.AccessToken,
      BroadcasterRefreshToken = ev.Data.RefreshToken,
      BroadcasterTokenExpiresAt = ev.Data.ExpiresAt,
      UpdatedOn = DateTime.UtcNow,
    };
  }

  public static explicit operator Domain.Twitch.TwitchSetup(DbTwitchSetup db)
  {
    return new Domain.Twitch.TwitchSetup
    {
      BotUserId = db.BotUserId,
      BotLogin = db.BotLogin,
      Channels = db.Channels
        .ConvertAll(c => new Domain.Twitch.TwitchChannel { UserId = c.UserId, Login = c.Login }),
      ConfiguredOn = db.CreatedOn,
      UpdatedOn = db.UpdatedOn,
      BotTokens = db.BotAccessToken != null ? new Domain.Twitch.TwitchAccountTokens
      {
        UserId = db.BotUserId,
        Login = db.BotLogin,
        AccessToken = db.BotAccessToken,
        RefreshToken = db.BotRefreshToken,
        ExpiresAt = db.BotTokenExpiresAt ?? DateTime.MinValue,
        LastRefreshedOn = db.UpdatedOn,
      } : null,
      BroadcasterTokens = db.BroadcasterAccessToken != null ? new Domain.Twitch.TwitchAccountTokens
      {
        UserId = db.BroadcasterUserId ?? "",
        Login = db.BroadcasterLogin ?? "",
        AccessToken = db.BroadcasterAccessToken,
        RefreshToken = db.BroadcasterRefreshToken,
        ExpiresAt = db.BroadcasterTokenExpiresAt ?? DateTime.MinValue,
        LastRefreshedOn = db.UpdatedOn,
      } : null,
    };
  }
}

public sealed record DbTwitchChannel
{
  public required string UserId { get; init; }

  public required string Login { get; init; }
}
