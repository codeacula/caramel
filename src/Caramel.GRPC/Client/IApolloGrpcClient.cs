using Caramel.GRPC.Service;

namespace Caramel.GRPC.Client;

public interface ICaramelGrpcClient
{
  ICaramelGrpcService CaramelGrpcService { get; }
}
