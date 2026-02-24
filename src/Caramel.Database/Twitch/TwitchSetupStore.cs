using Caramel.Core.Twitch;
using Caramel.Database.Twitch.Events;

using FluentResults;

using Marten;

namespace Caramel.Database.Twitch;

/// <summary>
/// Store for persisting and retrieving Twitch setup configuration using Marten event sourcing.
/// </summary>
/// <param name="session"></param>
public sealed class TwitchSetupStore(IDocumentSession session) : ITwitchSetupStore
{
  /// <inheritdoc/>
  public async Task<Result<Domain.Twitch.TwitchSetup?>> GetAsync(CancellationToken cancellationToken = default)
  {
    try
    {
      var db = await session.Query<DbTwitchSetup>()
        .FirstOrDefaultAsync(s => s.Id == DbTwitchSetup.WellKnownId, cancellationToken);

      return db is null ? Result.Ok<Domain.Twitch.TwitchSetup?>(null) : Result.Ok((Domain.Twitch.TwitchSetup?)db);
    }
    catch (Exception ex)
    {
      return Result.Fail<Domain.Twitch.TwitchSetup?>($"Failed to retrieve Twitch setup: {ex.Message}");
    }
  }

  /// <inheritdoc/>
  public async Task<Result<Domain.Twitch.TwitchSetup>> SaveAsync(
    Domain.Twitch.TwitchSetup setup,
    CancellationToken cancellationToken = default)
  {
    try
    {
      var now = DateTime.UtcNow;
      var channels = setup.Channels
        .Select(c => new TwitchChannelData { UserId = c.UserId, Login = c.Login })
        .ToList();

      // Check if this is a new setup or an update
      var existing = await session.Query<DbTwitchSetup>()
        .FirstOrDefaultAsync(s => s.Id == DbTwitchSetup.WellKnownId, cancellationToken);

      if (existing is null)
      {
        // Create new setup stream
        var createEvent = new TwitchSetupCreatedEvent
        {
          Id = DbTwitchSetup.WellKnownId,
          BotUserId = setup.BotUserId,
          BotLogin = setup.BotLogin,
          Channels = channels,
          CreatedOn = now,
        };

        _ = session.Events.StartStream<DbTwitchSetup>(DbTwitchSetup.WellKnownId, createEvent);
      }
      else
      {
        // Append update event to existing stream
        var updateEvent = new TwitchSetupUpdatedEvent
        {
          Id = DbTwitchSetup.WellKnownId,
          BotUserId = setup.BotUserId,
          BotLogin = setup.BotLogin,
          Channels = channels,
          CreatedOn = now,
        };

        _ = session.Events.Append(DbTwitchSetup.WellKnownId, updateEvent);
      }

      await session.SaveChangesAsync(cancellationToken);

      // Re-read the updated projection
      var saved = await session.Query<DbTwitchSetup>()
        .FirstAsync(s => s.Id == DbTwitchSetup.WellKnownId, cancellationToken);

      return Result.Ok((Domain.Twitch.TwitchSetup)saved);
    }
    catch (Exception ex)
    {
      return Result.Fail<Domain.Twitch.TwitchSetup>($"Failed to save Twitch setup: {ex.Message}");
    }
  }
}
