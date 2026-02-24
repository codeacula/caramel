using FluentResults;

namespace Caramel.Core.OBS;

public interface IOBSStatusProvider
{
  bool IsConnected { get; }
  Task<Result> SetCurrentProgramSceneAsync(string sceneName, CancellationToken cancellationToken = default);
  Task<Result<string>> GetCurrentProgramSceneAsync(CancellationToken cancellationToken = default);
}
