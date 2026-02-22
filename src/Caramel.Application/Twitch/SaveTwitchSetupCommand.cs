using Caramel.Core.Twitch;
using Caramel.Domain.Twitch;

using FluentResults;

using MediatR;

namespace Caramel.Application.Twitch;

public sealed record SaveTwitchSetupCommand : IRequest<Result<TwitchSetup>>
{
  public required string BotUserId { get; init; }
  public required string BotLogin { get; init; }
  public required IReadOnlyList<(string UserId, string Login)> Channels { get; init; }
}

public sealed class SaveTwitchSetupCommandHandler(ITwitchSetupStore store) : IRequestHandler<SaveTwitchSetupCommand, Result<TwitchSetup>>
{
  public async Task<Result<TwitchSetup>> Handle(SaveTwitchSetupCommand request, CancellationToken cancellationToken)
  {
    try
    {
      var now = DateTimeOffset.UtcNow;

      var setup = new TwitchSetup
      {
        BotUserId = request.BotUserId,
        BotLogin = request.BotLogin,
        Channels = request.Channels
          .Select(c => new TwitchChannel { UserId = c.UserId, Login = c.Login })
          .ToList(),
        ConfiguredOn = now,
        UpdatedOn = now,
      };

      return await store.SaveAsync(setup, cancellationToken);
    }
    catch (Exception ex)
    {
      return Result.Fail(ex.Message);
    }
  }
}
