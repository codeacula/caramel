using Caramel.Application.Conversations;
using Caramel.Application.Twitch;
using Caramel.Core.OBS;
using Caramel.GRPC.Context;
using Caramel.GRPC.Contracts;

using MediatR;

namespace Caramel.GRPC.Service;

public sealed class CaramelGrpcService(
  IMediator mediator,
  IUserContext userContext,
  IOBSStatusProvider obsStatusProvider
) : ICaramelGrpcService
{
  public async Task<GrpcResult<string>> SendCaramelMessageAsync(NewMessageRequest message)
  {
    var person = userContext.Person!;
    var command = new ProcessIncomingMessageCommand(person.Id, new Domain.Common.ValueObjects.Content(message.Content));

    var requestResult = await mediator.Send(command);
    return requestResult.IsFailed
      ? (GrpcResult<string>)requestResult.Errors.Select(e => new GrpcError(e.Message)).ToArray()
      : (GrpcResult<string>)requestResult.Value.Content.Value;
  }

  public async Task<GrpcResult<string>> AskTheOrbAsync(AskTheOrbGrpcRequest request)
  {
    var person = userContext.Person;
    if (person is null)
    {
      return (GrpcResult<string>)new[] { new GrpcError("Unable to resolve person for AskTheOrb request.") };
    }

    var command = new AskTheOrbCommand(person.Id, new Domain.Common.ValueObjects.Content(request.Content));
    var result = await mediator.Send(command);

    return result.IsFailed
      ? (GrpcResult<string>)result.Errors.Select(e => new GrpcError(e.Message)).ToArray()
      : (GrpcResult<string>)result.Value;
  }

  public async Task<GrpcResult<TwitchSetupDTO>> GetTwitchSetupAsync()
  {
    var result = await mediator.Send(new GetTwitchSetupQuery());

    if (result.IsFailed)
    {
      return result.Errors.Select(e => new GrpcError(e.Message)).ToArray();
    }

    if (result.Value is null)
    {
      return new GrpcResult<TwitchSetupDTO> { IsSuccess = true, Data = null };
    }

    var setup = result.Value;
    return new TwitchSetupDTO
    {
      BotUserId = setup.BotUserId,
      BotLogin = setup.BotLogin,
      Channels = [.. setup.Channels.Select(c => new TwitchChannelDTO { UserId = c.UserId, Login = c.Login })],
      ConfiguredOnTicks = setup.ConfiguredOn.UtcTicks,
      UpdatedOnTicks = setup.UpdatedOn.UtcTicks,
      BotTokens = setup.BotTokens is not null ? new TwitchAccountTokensDTO
      {
        UserId = setup.BotTokens.UserId,
        Login = setup.BotTokens.Login,
        HasRefreshToken = setup.BotTokens.RefreshToken is not null,
        ExpiresAtTicks = setup.BotTokens.ExpiresAt.Ticks,
      } : null,
      BroadcasterTokens = setup.BroadcasterTokens is not null ? new TwitchAccountTokensDTO
      {
        UserId = setup.BroadcasterTokens.UserId,
        Login = setup.BroadcasterTokens.Login,
        HasRefreshToken = setup.BroadcasterTokens.RefreshToken is not null,
        ExpiresAtTicks = setup.BroadcasterTokens.ExpiresAt.Ticks,
      } : null,
    };
  }

  public async Task<GrpcResult<TwitchSetupDTO>> SaveTwitchSetupAsync(SaveTwitchSetupRequest request)
  {
    var command = new SaveTwitchSetupCommand
    {
      BotUserId = request.BotUserId,
      BotLogin = request.BotLogin,
      Channels = request.Channels
        .ConvertAll(c => (c.UserId, c.Login))
    };

    var result = await mediator.Send(command);

    if (result.IsFailed)
    {
      return result.Errors.Select(e => new GrpcError(e.Message)).ToArray();
    }

    var setup = result.Value;
    return new TwitchSetupDTO
    {
      BotUserId = setup.BotUserId,
      BotLogin = setup.BotLogin,
      Channels = [.. setup.Channels.Select(c => new TwitchChannelDTO { UserId = c.UserId, Login = c.Login })],
      ConfiguredOnTicks = setup.ConfiguredOn.UtcTicks,
      UpdatedOnTicks = setup.UpdatedOn.UtcTicks
    };
  }

  public async Task<GrpcResult<TwitchSetupDTO>> LinkBroadcasterTokenAsync(LinkBroadcasterTokenRequest request)
  {
    var command = new LinkBroadcasterTokenCommand
    {
      BroadcasterUserId = request.BroadcasterUserId,
      BroadcasterLogin = request.BroadcasterLogin,
      AccessToken = request.AccessToken,
      RefreshToken = request.RefreshToken,
      ExpiresAt = new DateTime(request.ExpiresAtTicks, DateTimeKind.Utc),
    };

    var result = await mediator.Send(command);

    if (result.IsFailed)
    {
      return result.Errors.Select(e => new GrpcError(e.Message)).ToArray();
    }

    var setup = result.Value;
    return new TwitchSetupDTO
    {
      BotUserId = setup.BotUserId,
      BotLogin = setup.BotLogin,
      Channels = [.. setup.Channels.Select(c => new TwitchChannelDTO { UserId = c.UserId, Login = c.Login })],
      ConfiguredOnTicks = setup.ConfiguredOn.UtcTicks,
      UpdatedOnTicks = setup.UpdatedOn.UtcTicks,
      BotTokens = setup.BotTokens is not null ? new TwitchAccountTokensDTO
      {
        UserId = setup.BotTokens.UserId,
        Login = setup.BotTokens.Login,
        HasRefreshToken = setup.BotTokens.RefreshToken is not null,
        ExpiresAtTicks = setup.BotTokens.ExpiresAt.Ticks,
      } : null,
      BroadcasterTokens = setup.BroadcasterTokens is not null ? new TwitchAccountTokensDTO
      {
        UserId = setup.BroadcasterTokens.UserId,
        Login = setup.BroadcasterTokens.Login,
        HasRefreshToken = setup.BroadcasterTokens.RefreshToken is not null,
        ExpiresAtTicks = setup.BroadcasterTokens.ExpiresAt.Ticks,
      } : null,
    };
  }

  public async Task<GrpcResult<OBSStatusDTO>> GetOBSStatusAsync()
  {
    string? currentScene = null;
    if (obsStatusProvider.IsConnected)
    {
      var sceneResult = await obsStatusProvider.GetCurrentProgramSceneAsync();
      currentScene = sceneResult.IsSuccess ? sceneResult.Value : null;
    }

    return new OBSStatusDTO
    {
      IsConnected = obsStatusProvider.IsConnected,
      CurrentScene = currentScene
    };
  }

  public async Task<GrpcResult<string>> SetOBSSceneAsync(SetOBSSceneRequest request)
  {
    var result = await obsStatusProvider.SetCurrentProgramSceneAsync(request.SceneName);
    return result.IsFailed
      ? (GrpcResult<string>)result.Errors.Select(e => new GrpcError(e.Message)).ToArray()
      : (GrpcResult<string>)"Scene switched successfully";
  }
}
