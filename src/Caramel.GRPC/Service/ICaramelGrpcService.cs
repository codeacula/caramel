using System.ServiceModel;

using Caramel.GRPC.Contracts;

namespace Caramel.GRPC.Service;

[ServiceContract]
public interface ICaramelGrpcService
{
  [OperationContract]
  Task<GrpcResult<string>> SendCaramelMessageAsync(NewMessageRequest message);

  [OperationContract]
  Task<GrpcResult<TwitchSetupDTO>> GetTwitchSetupAsync();

  [OperationContract]
  Task<GrpcResult<TwitchSetupDTO>> SaveTwitchSetupAsync(SaveTwitchSetupRequest request);

  [OperationContract]
  Task<GrpcResult<OBSStatusDTO>> GetOBSStatusAsync();

  [OperationContract]
  Task<GrpcResult<string>> SetOBSSceneAsync(SetOBSSceneRequest request);
}
