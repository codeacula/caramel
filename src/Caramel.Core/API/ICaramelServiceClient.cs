using Caramel.Core.Conversations;
using Caramel.Domain.Twitch;

using FluentResults;

namespace Caramel.Core.API;

public interface ICaramelServiceClient
{
  Task<Result<string>> SendMessageAsync(ProcessMessageRequest request, CancellationToken cancellationToken = default);
  Task<Result<string>> AskTheOrbAsync(AskTheOrbRequest request, CancellationToken cancellationToken = default);
  Task<Result<TwitchSetup?>> GetTwitchSetupAsync(CancellationToken cancellationToken = default);
  Task<Result<TwitchSetup>> SaveTwitchSetupAsync(TwitchSetup setup, CancellationToken cancellationToken = default);
}
