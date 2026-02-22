using Caramel.Domain.People.Models;

using FluentResults;

namespace Caramel.Core.ToDos;

public interface IReminderMessageGenerator
{
  Task<Result<string>> GenerateReminderMessageAsync(
    Person person,
    IEnumerable<string> toDoDescriptions,
    CancellationToken cancellationToken = default);
}
