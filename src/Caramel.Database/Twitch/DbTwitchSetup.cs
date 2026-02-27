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

  public static explicit operator Domain.Twitch.TwitchSetup(DbTwitchSetup db)
  {
    return new Domain.Twitch.TwitchSetup
    {
      BotUserId = db.BotUserId,
      BotLogin = db.BotLogin,
      Channels = db.Channels
        .ConvertAll(c => new Domain.Twitch.TwitchChannel { UserId = c.UserId, Login = c.Login })
,
      ConfiguredOn = db.CreatedOn,
      UpdatedOn = db.UpdatedOn,
    };
  }
}

public sealed record DbTwitchChannel
{
  public required string UserId { get; init; }

  public required string Login { get; init; }
}
