using Caramel.Core.API;
using Caramel.Core.Conversations;
using Caramel.Core.OBS;
using Caramel.Domain.Twitch;
using Caramel.GRPC.Interceptors;
using Caramel.GRPC.Service;

using FluentResults;

using Grpc.Core.Interceptors;
using Grpc.Net.Client;

using ProtoBuf.Grpc.Client;

namespace Caramel.GRPC.Client;

public class CaramelGrpcClient : ICaramelServiceClient, IDisposable
{
  public ICaramelGrpcService CaramelGrpcService { get; }
  private readonly GrpcChannel _channel;

  public CaramelGrpcClient(GrpcChannel channel, GrpcClientLoggingInterceptor GrpcClientLoggingInterceptor, GrpcHostConfig grpcHostConfig)
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

  public async Task<Result<string>> SendMessageAsync(ProcessMessageRequest request, CancellationToken cancellationToken = default)
  {
    var grpcRequest = new Contracts.NewMessageRequest
    {
      Platform = request.Platform,
      PlatformUserId = request.PlatformUserId,
      Username = request.Username,
      Content = request.Content
    };

    var grpcResult = await CaramelGrpcService.SendCaramelMessageAsync(grpcRequest);

    return grpcResult.IsSuccess ?
      Result.Ok(grpcResult.Data ?? string.Empty) :
      Result.Fail(string.Join("; ", grpcResult.Errors.Select(e => e.Message)));
  }

  public async Task<Result<TwitchSetup?>> GetTwitchSetupAsync(CancellationToken cancellationToken = default)
  {
    var grpcResult = await CaramelGrpcService.GetTwitchSetupAsync();

    if (!grpcResult.IsSuccess)
    {
      return Result.Fail<TwitchSetup?>(string.Join("; ", grpcResult.Errors.Select(e => e.Message)));
    }
    else if (grpcResult.Data is null)
    {
      return Result.Ok<TwitchSetup?>(null);
    }
    else
    {
      return Result.Ok<TwitchSetup?>(MapTwitchSetupToDomain(grpcResult.Data));
    }
  }

  public async Task<Result<TwitchSetup>> SaveTwitchSetupAsync(TwitchSetup setup, CancellationToken cancellationToken = default)
  {
    var grpcRequest = new Contracts.SaveTwitchSetupRequest
    {
      BotUserId = setup.BotUserId,
      BotLogin = setup.BotLogin,
      Channels = [.. setup.Channels.Select(c => new Contracts.TwitchChannelDTO { UserId = c.UserId, Login = c.Login })]
    };

    var grpcResult = await CaramelGrpcService.SaveTwitchSetupAsync(grpcRequest);

    return !grpcResult.IsSuccess || grpcResult.Data is null
      ? Result.Fail<TwitchSetup>(string.Join("; ", grpcResult.Errors.Select(e => e.Message)))
      : Result.Ok(MapTwitchSetupToDomain(grpcResult.Data));
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
    var grpcRequest = new Contracts.SetOBSSceneRequest { SceneName = sceneName };
    var grpcResult = await CaramelGrpcService.SetOBSSceneAsync(grpcRequest);
    return grpcResult.IsSuccess
      ? Result.Ok(grpcResult.Data ?? string.Empty)
      : Result.Fail(string.Join("; ", grpcResult.Errors.Select(e => e.Message)));
  }

  private static TwitchSetup MapTwitchSetupToDomain(Contracts.TwitchSetupDTO dto)
  {
    return new TwitchSetup
    {
      BotUserId = dto.BotUserId,
      BotLogin = dto.BotLogin,
      Channels = dto.Channels
        .ConvertAll(c => new TwitchChannel { UserId = c.UserId, Login = c.Login })
,
      ConfiguredOn = DateTimeOffset.UtcNow,
      UpdatedOn = DateTimeOffset.UtcNow
    };
  }
}
