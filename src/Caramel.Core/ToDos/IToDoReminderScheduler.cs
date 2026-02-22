using Caramel.Domain.ToDos.ValueObjects;

using FluentResults;

namespace Caramel.Core.ToDos;

public interface IToDoReminderScheduler
{
  Task<Result> DeleteJobAsync(QuartzJobId quartzJobId, CancellationToken cancellationToken = default);
  Task<Result<QuartzJobId>> GetOrCreateJobAsync(DateTime reminderDate, CancellationToken cancellationToken = default);
}
