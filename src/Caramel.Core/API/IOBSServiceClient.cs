using Caramel.Core.OBS;

using FluentResults;

namespace Caramel.Core.API;

public interface IOBSServiceClient
{
  Task<Result<OBSStatus>> GetOBSStatusAsync(CancellationToken cancellationToken = default);
  Task<Result<string>> SetOBSSceneAsync(string sceneName, CancellationToken cancellationToken = default);
}
