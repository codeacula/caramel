using Caramel.Core.ToDos;
using Caramel.Domain.ToDos.ValueObjects;

using FluentResults;

namespace Caramel.Application.ToDos;

public sealed class NoOpToDoReminderScheduler : IToDoReminderScheduler
{
  public Task<Result> DeleteJobAsync(QuartzJobId quartzJobId, CancellationToken cancellationToken = default)
  {
    return Task.FromResult(Result.Ok());
  }

  public Task<Result<QuartzJobId>> GetOrCreateJobAsync(DateTime reminderDate, CancellationToken cancellationToken = default)
  {
    return Task.FromResult(Result.Fail<QuartzJobId>("To-Do reminder scheduler is not configured."));
  }
}
