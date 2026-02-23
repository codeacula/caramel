using Caramel.Database.Twitch.Events;

using JasperFx.Events;

namespace Caramel.Database.Twitch;

/// <summary>
/// Marten inline snapshot projection for Twitch setup configuration.
/// This is a singleton aggregate identified by a well-known GUID.
/// </summary>
public sealed record DbTwitchSetup
{
  /// <summary>
  /// Well-known identifier for the singleton Twitch setup stream.
  /// </summary>
  public static readonly Guid WellKnownId = new("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

  /// <summary>
  /// Unique identifier for this Twitch setup record.
  /// </summary>
  public required Guid Id { get; init; }

  /// <summary>
  /// Numeric Twitch user ID of the bot.
  /// </summary>
  public required string BotUserId { get; init; }

  /// <summary>
  /// Login name of the bot.
  /// </summary>
  public required string BotLogin { get; init; }

  /// <summary>
  /// Configured channels with their IDs and login names.
  /// </summary>
  public required List<DbTwitchChannel> Channels { get; init; }

  /// <summary>
  /// UTC timestamp when the configuration was created.
  /// </summary>
  public required DateTime CreatedOn { get; init; }

  /// <summary>
  /// UTC timestamp when the configuration was last updated.
  /// </summary>
  public required DateTime UpdatedOn { get; init; }

  /// <summary>
  /// Creates a new DbTwitchSetup from the initial event.
  /// </summary>
  /// <param name="ev"></param>
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

  /// <summary>
  /// Applies a TwitchSetupUpdatedEvent to the current state.
  /// </summary>
  /// <param name="ev"></param>
  /// <param name="current"></param>
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

  /// <summary>
  /// Explicit conversion from DbTwitchSetup to domain model TwitchSetup.
  /// </summary>
  /// <param name="db"></param>
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

/// <summary>
/// Channel data stored in the Twitch setup aggregate.
/// </summary>
public sealed record DbTwitchChannel
{
  /// <summary>
  /// Numeric Twitch user ID of the channel.
  /// </summary>
  public required string UserId { get; init; }

  /// <summary>
  /// Login name of the channel.
  /// </summary>
  public required string Login { get; init; }
}
