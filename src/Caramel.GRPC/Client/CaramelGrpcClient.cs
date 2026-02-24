using Caramel.Core.API;
using Caramel.Core.Conversations;
using Caramel.Core.OBS;
using Caramel.Core.ToDos.Responses;
using Caramel.Domain.People.ValueObjects;
using Caramel.Domain.ToDos.Models;
using Caramel.Domain.Twitch;
using Caramel.GRPC.Interceptors;
using Caramel.GRPC.Service;

using FluentResults;

using Grpc.Core.Interceptors;
using Grpc.Net.Client;

using ProtoBuf.Grpc.Client;

using CoreCreateReminderRequest = Caramel.Core.Reminders.Requests.CreateReminderRequest;
using CoreCreateToDoRequest = Caramel.Core.ToDos.Requests.CreateToDoRequest;
using GrpcCreateReminderRequest = Caramel.GRPC.Contracts.CreateReminderRequest;
using GrpcCreateToDoRequest = Caramel.GRPC.Contracts.CreateToDoRequest;
using GrpcGetDailyPlanRequest = Caramel.GRPC.Contracts.GetDailyPlanRequest;
using GrpcGetPersonToDosRequest = Caramel.GRPC.Contracts.GetPersonToDosRequest;
using GrpcManageAccessRequest = Caramel.GRPC.Contracts.ManageAccessRequest;
using GrpcReminderDTO = Caramel.GRPC.Contracts.ReminderDTO;
using GrpcToDoDTO = Caramel.GRPC.Contracts.ToDoDTO;

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

  public async Task<Result<ToDo>> CreateToDoAsync(CoreCreateToDoRequest request, CancellationToken cancellationToken = default)
  {
    var grpcRequest = new GrpcCreateToDoRequest
    {
      Platform = request.PlatformId.Platform,
      PlatformUserId = request.PlatformId.PlatformUserId,
      Username = request.PlatformId.Username,
      Title = request.Title,
      Description = request.Description,
      ReminderDate = request.ReminderDate,
    };

    Result<GrpcToDoDTO> grpcResponse = await CaramelGrpcService.CreateToDoAsync(grpcRequest);

    return grpcResponse.IsFailed ? Result.Fail<ToDo>(grpcResponse.Errors) : Result.Ok(MapToDomain(grpcResponse.Value));
  }

  public async Task<Result<Reminder>> CreateReminderAsync(CoreCreateReminderRequest request, CancellationToken cancellationToken = default)
  {
    var grpcRequest = new GrpcCreateReminderRequest
    {
      Platform = request.PlatformId.Platform,
      PlatformUserId = request.PlatformId.PlatformUserId,
      Username = request.PlatformId.Username,
      Message = request.Message,
      ReminderTime = request.ReminderTime,
    };

    Result<GrpcReminderDTO> grpcResponse = await CaramelGrpcService.CreateReminderAsync(grpcRequest);

    return grpcResponse.IsFailed
      ? Result.Fail<Reminder>(grpcResponse.Errors)
      : Result.Ok(MapReminderToDomain(grpcResponse.Value));
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

  public async Task<Result<IEnumerable<ToDoSummary>>> GetToDosAsync(PlatformId platformId, bool includeCompleted = false, CancellationToken cancellationToken = default)
  {
    var grpcRequest = new GrpcGetPersonToDosRequest
    {
      Platform = platformId.Platform,
      IncludeCompleted = includeCompleted,
      PlatformUserId = platformId.PlatformUserId,
      Username = platformId.Username
    };

    Result<GrpcToDoDTO[]> grpcResponse = await CaramelGrpcService.GetPersonToDosAsync(grpcRequest);

    if (grpcResponse.IsFailed)
    {
      return Result.Fail<IEnumerable<ToDoSummary>>(grpcResponse.Errors);
    }

    var summaries = grpcResponse.Value.Select(dto => new ToDoSummary
    {
      Id = dto.Id,
      Description = dto.Description,
      ReminderDate = dto.ReminderDate,
      CreatedOn = dto.CreatedOn,
      UpdatedOn = dto.UpdatedOn
    }).ToArray();

    return Result.Ok<IEnumerable<ToDoSummary>>(summaries);
  }

  public async Task<Result<DailyPlanResponse>> GetDailyPlanAsync(PlatformId platformId, CancellationToken cancellationToken = default)
  {
    var grpcRequest = new GrpcGetDailyPlanRequest
    {
      Platform = platformId.Platform,
      PlatformUserId = platformId.PlatformUserId,
      Username = platformId.Username
    };

    var grpcResponse = await CaramelGrpcService.GetDailyPlanAsync(grpcRequest);

    if (!grpcResponse.IsSuccess || grpcResponse.Data == null)
    {
      return Result.Fail<DailyPlanResponse>(
        string.Join("; ", grpcResponse.Errors.Select(e => e.Message))
      );
    }

    var dto = grpcResponse.Data;
    // Defensive: SuggestedTasks may be null due to serialization edge-cases across the gRPC boundary.
    // Treat null as empty to provide failsafe behavior for callers.
    var suggestedTasks = (Contracts.DailyPlanTaskDTO[]?)dto.SuggestedTasks ?? [];

    var tasks = suggestedTasks.Select(t => new DailyPlanTaskResponse
    {
      Id = t.Id,
      Description = t.Description,
      Priority = t.Priority,
      Energy = t.Energy,
      Interest = t.Interest,
      DueDate = t.DueDate
    }).ToList();

    var response = new DailyPlanResponse
    {
      SuggestedTasks = tasks,
      SelectionRationale = dto.SelectionRationale,
      TotalActiveTodos = dto.TotalActiveTodos
    };

    return Result.Ok(response);
  }

  public async Task<Result<string>> GrantAccessAsync(PlatformId adminPlatformId, PlatformId targetPlatformId, CancellationToken cancellationToken = default)
  {
    var grpcRequest = new GrpcManageAccessRequest
    {
      AdminPlatform = adminPlatformId.Platform,
      AdminPlatformUserId = adminPlatformId.PlatformUserId,
      AdminUsername = adminPlatformId.Username,
      TargetPlatform = targetPlatformId.Platform,
      TargetPlatformUserId = targetPlatformId.PlatformUserId,
      TargetUsername = targetPlatformId.Username
    };

    var grpcResult = await CaramelGrpcService.GrantAccessAsync(grpcRequest);

    return grpcResult.IsSuccess
      ? Result.Ok(grpcResult.Data ?? string.Empty)
      : Result.Fail(string.Join("; ", grpcResult.Errors.Select(e => e.Message)));
  }

  public async Task<Result<string>> RevokeAccessAsync(PlatformId adminPlatformId, PlatformId targetPlatformId, CancellationToken cancellationToken = default)
  {
    var grpcRequest = new GrpcManageAccessRequest
    {
      AdminPlatform = adminPlatformId.Platform,
      AdminPlatformUserId = adminPlatformId.PlatformUserId,
      AdminUsername = adminPlatformId.Username,
      TargetPlatform = targetPlatformId.Platform,
      TargetPlatformUserId = targetPlatformId.PlatformUserId,
      TargetUsername = targetPlatformId.Username
    };

    var grpcResult = await CaramelGrpcService.RevokeAccessAsync(grpcRequest);

    return grpcResult.IsSuccess
      ? Result.Ok(grpcResult.Data ?? string.Empty)
      : Result.Fail(string.Join("; ", grpcResult.Errors.Select(e => e.Message)));
  }

  public async Task<Result<TwitchSetup?>> GetTwitchSetupAsync(CancellationToken cancellationToken = default)
  {
    var grpcResult = await CaramelGrpcService.GetTwitchSetupAsync();

    if (!grpcResult.IsSuccess)
    {
      return Result.Fail<TwitchSetup?>(string.Join("; ", grpcResult.Errors.Select(e => e.Message)));
    }

    return grpcResult.Data is null ? Result.Ok<TwitchSetup?>(null) : Result.Ok<TwitchSetup?>(MapTwitchSetupToDomain(grpcResult.Data));
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
    if (!grpcResult.IsSuccess || grpcResult.Data is null)
    {
      return Result.Fail<OBSStatus>(string.Join("; ", grpcResult.Errors.Select(e => e.Message)));
    }

    return Result.Ok(new OBSStatus
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

  private static ToDo MapToDomain(GrpcToDoDTO dto)
  {
    return new ToDo
    {
      Id = new(dto.Id),
      PersonId = new(dto.PersonId),
      Description = new(dto.Description),
      Priority = new(dto.Priority),
      Energy = new(dto.Energy),
      Interest = new(dto.Interest),
      Reminders = [],
      DueDate = null,
      CreatedOn = new(dto.CreatedOn),
      UpdatedOn = new(dto.UpdatedOn)
    };
  }

  private static Reminder MapReminderToDomain(GrpcReminderDTO dto)
  {
    return new Reminder
    {
      Id = new(dto.Id),
      PersonId = new(dto.PersonId),
      Details = new(dto.Details),
      ReminderTime = new(dto.ReminderTime),
      CreatedOn = new(dto.CreatedOn),
      UpdatedOn = new(dto.UpdatedOn)
    };
  }
}
