using Caramel.Domain.Common.Enums;

namespace Caramel.GRPC.Contracts;

public interface IAuthenticatedRequest
{
  Platform Platform { get; }
  string PlatformUserId { get; }
  string Username { get; }
}
