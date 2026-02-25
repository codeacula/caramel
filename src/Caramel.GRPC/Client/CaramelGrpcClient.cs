using Caramel.Core.API;
using Caramel.Core.Conversations;
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

  public async Task<Result<TwitchSetup?>> GetTwitchSetupAsync(CancellationToken cancellationToken = default)
  {
    var grpcResult = await CaramelGrpcService.GetTwitchSetupAsync();

    return grpcResult switch
    {
      { IsSuccess: false } => Result.Fail<TwitchSetup?>(string.Join("; ", grpcResult.Errors.Select(e => e.Message))),
      { Data: null } => Result.Ok<TwitchSetup?>(null),
      { Data: { } data } => Result.Ok<TwitchSetup?>(MapTwitchSetupToDomain(data)),
      _ => Result.Fail<TwitchSetup?>("Unknown gRPC response state.")
    };
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

    return grpcResult switch
    {
      { IsSuccess: true, Data: { } data } => Result.Ok(MapTwitchSetupToDomain(data)),
      _ => Result.Fail<TwitchSetup>(string.Join("; ", grpcResult.Errors.Select(e => e.Message)))
    };
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
