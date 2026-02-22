using Caramel.Domain.People.ValueObjects;
using Caramel.Domain.ToDos.Models;
using Caramel.Domain.ToDos.ValueObjects;

using FluentResults;

namespace Caramel.Core.ToDos;

public interface IReminderStore
{
  Task<Result<Reminder>> CreateAsync(
    ReminderId id,
    PersonId personId,
    Details details,
    ReminderTime reminderTime,
    QuartzJobId quartzJobId,
    CancellationToken cancellationToken = default);

  Task<Result<Reminder>> GetAsync(ReminderId id, CancellationToken cancellationToken = default);

  Task<Result<IEnumerable<Reminder>>> GetByToDoIdAsync(ToDoId toDoId, CancellationToken cancellationToken = default);

  Task<Result<IEnumerable<Reminder>>> GetByQuartzJobIdAsync(QuartzJobId quartzJobId, CancellationToken cancellationToken = default);

  Task<Result> LinkToToDoAsync(ReminderId reminderId, ToDoId toDoId, CancellationToken cancellationToken = default);

  Task<Result> UnlinkFromToDoAsync(ReminderId reminderId, ToDoId toDoId, CancellationToken cancellationToken = default);

  Task<Result> MarkAsSentAsync(ReminderId id, CancellationToken cancellationToken = default);

  Task<Result> AcknowledgeAsync(ReminderId id, CancellationToken cancellationToken = default);

  Task<Result> DeleteAsync(ReminderId id, CancellationToken cancellationToken = default);

  Task<Result<IEnumerable<ToDoId>>> GetLinkedToDoIdsAsync(ReminderId reminderId, CancellationToken cancellationToken = default);

  Task<Result<IEnumerable<ReminderId>>> GetLinkedReminderIdsAsync(ToDoId toDoId, CancellationToken cancellationToken = default);
}
