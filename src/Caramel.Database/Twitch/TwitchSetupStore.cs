using Caramel.Core.Security;
using Caramel.Core.Twitch;
using Caramel.Database.Twitch.Events;
using Caramel.Domain.Twitch;

using FluentResults;

using Marten;

namespace Caramel.Database.Twitch;

public sealed class TwitchSetupStore(
  IDocumentSession session,
  ITokenEncryptionService encryptionService,
  ITwitchSetupChangedNotifier setupChangedNotifier) : ITwitchSetupStore
{
  public async Task<Result<TwitchSetup?>> GetAsync(CancellationToken cancellationToken = default)
  {
    try
    {
      var db = await session.Query<DbTwitchSetup>()
        .FirstOrDefaultAsync(s => s.Id == DbTwitchSetup.WellKnownId, cancellationToken);

      return db is null ? Result.Ok<TwitchSetup?>(null) : Result.Ok((TwitchSetup?)db);
    }
    catch (OperationCanceledException)
    {
      throw;
    }
    catch (InvalidOperationException ex)
    {
      return Result.Fail<TwitchSetup?>($"Invalid operation retrieving Twitch setup: {ex.Message}");
    }
    catch (Exception ex)
    {
      return Result.Fail<TwitchSetup?>($"Unexpected error retrieving Twitch setup: {ex.Message}");
    }
  }

  public async Task<Result<TwitchSetup>> SaveAsync(
    TwitchSetup setup,
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

      var savedSetup = (TwitchSetup)saved;
      await setupChangedNotifier.PublishAsync(savedSetup, cancellationToken);

      return Result.Ok(savedSetup);
    }
    catch (OperationCanceledException)
    {
      throw;
    }
    catch (InvalidOperationException ex)
    {
      return Result.Fail<TwitchSetup>($"Invalid operation saving Twitch setup: {ex.Message}");
    }
    catch (Exception ex)
    {
      return Result.Fail<TwitchSetup>($"Unexpected error saving Twitch setup: {ex.Message}");
    }
  }

  public async Task<Result<TwitchSetup>> SaveBotTokensAsync(
    TwitchAccountTokens tokens,
    CancellationToken cancellationToken = default)
  {
    try
    {
      // Encrypt token data
      var encryptedAccessToken = encryptionService.Encrypt(tokens.AccessToken);
      var encryptedRefreshToken = tokens.RefreshToken is not null
        ? encryptionService.Encrypt(tokens.RefreshToken)
        : null;

      var now = DateTime.UtcNow;

      // Append bot token update event
      var tokenEvent = new TwitchBotTokensUpdatedEvent
      {
        Id = DbTwitchSetup.WellKnownId,
        CreatedOn = now,
        BotUserId = tokens.UserId,
        BotLogin = tokens.Login,
        AccessToken = encryptedAccessToken,
        RefreshToken = encryptedRefreshToken,
        ExpiresAt = tokens.ExpiresAt,
      };

      _ = session.Events.Append(DbTwitchSetup.WellKnownId, tokenEvent);
      await session.SaveChangesAsync(cancellationToken);

      // Re-read the updated projection
      var updated = await session.Query<DbTwitchSetup>()
        .FirstAsync(s => s.Id == DbTwitchSetup.WellKnownId, cancellationToken);

      var updatedSetup = (TwitchSetup)updated;
      await setupChangedNotifier.PublishAsync(updatedSetup, cancellationToken);

      return Result.Ok(updatedSetup);
    }
    catch (OperationCanceledException)
    {
      throw;
    }
    catch (InvalidOperationException ex)
    {
      return Result.Fail<TwitchSetup>($"Invalid operation saving bot tokens: {ex.Message}");
    }
    catch (Exception ex)
    {
      return Result.Fail<TwitchSetup>($"Unexpected error saving bot tokens: {ex.Message}");
    }
  }

  public async Task<Result<TwitchSetup>> SaveBroadcasterTokensAsync(
    TwitchAccountTokens tokens,
    CancellationToken cancellationToken = default)
  {
    try
    {
      // Encrypt token data
      var encryptedAccessToken = encryptionService.Encrypt(tokens.AccessToken);
      var encryptedRefreshToken = tokens.RefreshToken is not null
        ? encryptionService.Encrypt(tokens.RefreshToken)
        : null;

      var now = DateTime.UtcNow;

      // Append broadcaster token update event
      var tokenEvent = new TwitchBroadcasterTokensUpdatedEvent
      {
        Id = DbTwitchSetup.WellKnownId,
        CreatedOn = now,
        BroadcasterUserId = tokens.UserId,
        BroadcasterLogin = tokens.Login,
        AccessToken = encryptedAccessToken,
        RefreshToken = encryptedRefreshToken,
        ExpiresAt = tokens.ExpiresAt,
      };

      _ = session.Events.Append(DbTwitchSetup.WellKnownId, tokenEvent);
      await session.SaveChangesAsync(cancellationToken);

      // Re-read the updated projection
      var updated = await session.Query<DbTwitchSetup>()
        .FirstAsync(s => s.Id == DbTwitchSetup.WellKnownId, cancellationToken);

      var updatedSetup = (TwitchSetup)updated;
      await setupChangedNotifier.PublishAsync(updatedSetup, cancellationToken);

      return Result.Ok(updatedSetup);
    }
    catch (OperationCanceledException)
    {
      throw;
    }
    catch (InvalidOperationException ex)
    {
      return Result.Fail<TwitchSetup>($"Invalid operation saving broadcaster tokens: {ex.Message}");
    }
    catch (Exception ex)
    {
      return Result.Fail<TwitchSetup>($"Unexpected error saving broadcaster tokens: {ex.Message}");
    }
  }
}
