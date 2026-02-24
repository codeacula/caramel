using Caramel.Core.Conversations;
using Caramel.Core.OBS;
using Caramel.Core.Reminders.Requests;
using Caramel.Core.ToDos.Requests;
using Caramel.Core.ToDos.Responses;
using Caramel.Domain.People.ValueObjects;
using Caramel.Domain.ToDos.Models;
using Caramel.Domain.Twitch;

using FluentResults;

namespace Caramel.Core.API;

public interface ICaramelServiceClient
{
  Task<Result<ToDo>> CreateToDoAsync(CreateToDoRequest request, CancellationToken cancellationToken = default);
  Task<Result<Reminder>> CreateReminderAsync(CreateReminderRequest request, CancellationToken cancellationToken = default);
  Task<Result<IEnumerable<ToDoSummary>>> GetToDosAsync(PlatformId platformId, bool includeCompleted = false, CancellationToken cancellationToken = default);
  Task<Result<DailyPlanResponse>> GetDailyPlanAsync(PlatformId platformId, CancellationToken cancellationToken = default);
  Task<Result<string>> GrantAccessAsync(PlatformId adminPlatformId, PlatformId targetPlatformId, CancellationToken cancellationToken = default);
  Task<Result<string>> RevokeAccessAsync(PlatformId adminPlatformId, PlatformId targetPlatformId, CancellationToken cancellationToken = default);
  Task<Result<string>> SendMessageAsync(ProcessMessageRequest request, CancellationToken cancellationToken = default);
  Task<Result<TwitchSetup?>> GetTwitchSetupAsync(CancellationToken cancellationToken = default);
  Task<Result<TwitchSetup>> SaveTwitchSetupAsync(TwitchSetup setup, CancellationToken cancellationToken = default);
  Task<Result<OBSStatus>> GetOBSStatusAsync(CancellationToken cancellationToken = default);
  Task<Result<string>> SetOBSSceneAsync(string sceneName, CancellationToken cancellationToken = default);
}
