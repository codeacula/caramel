using Caramel.Core.API;
using Caramel.Core.OBS;
using Caramel.GRPC.Contracts;
using Caramel.GRPC.Interceptors;
using Caramel.GRPC.Service;

using FluentResults;

using Grpc.Core.Interceptors;
using Grpc.Net.Client;

using ProtoBuf.Grpc.Client;

namespace Caramel.GRPC.Client;

public class OBSServiceClient : IOBSServiceClient, IDisposable
{
  public ICaramelGrpcService CaramelGrpcService { get; }
  private readonly GrpcChannel _channel;

  public OBSServiceClient(GrpcChannel channel, GrpcClientLoggingInterceptor GrpcClientLoggingInterceptor, GrpcHostConfig grpcHostConfig)
  {
    _channel = channel;
    var invoker = _channel.Intercept(GrpcClientLoggingInterceptor)
      .Intercept(metadata =>
      {
        metadata.Add("X-API-Token", grpcHostConfig.ApiToken);
        return metadata;
      });
    CaramelGrpcService = invoker.CreateGrpcService<ICaramelGrpcService>();
  }

  public void Dispose()
  {
    _channel.Dispose();
    GC.SuppressFinalize(this);
  }

  public async Task<Result<OBSStatus>> GetOBSStatusAsync(CancellationToken cancellationToken = default)
  {
    var grpcResult = await CaramelGrpcService.GetOBSStatusAsync();
    return !grpcResult.IsSuccess || grpcResult.Data is null
      ? Result.Fail<OBSStatus>(string.Join("; ", grpcResult.Errors.Select(e => e.Message)))
      : Result.Ok(new OBSStatus
      {
        IsConnected = grpcResult.Data.IsConnected,
        CurrentScene = grpcResult.Data.CurrentScene
      });
  }

  public async Task<Result<string>> SetOBSSceneAsync(string sceneName, CancellationToken cancellationToken = default)
  {
    var grpcRequest = new SetOBSSceneRequest { SceneName = sceneName };
    var grpcResult = await CaramelGrpcService.SetOBSSceneAsync(grpcRequest);
    return grpcResult.IsSuccess
      ? Result.Ok(grpcResult.Data ?? string.Empty)
      : Result.Fail(string.Join("; ", grpcResult.Errors.Select(e => e.Message)));
  }
}
