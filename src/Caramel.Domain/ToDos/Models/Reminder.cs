using Caramel.Domain.Common.ValueObjects;
using Caramel.Domain.People.ValueObjects;
using Caramel.Domain.ToDos.ValueObjects;

namespace Caramel.Domain.ToDos.Models;

public record Reminder
{
  public ReminderId Id { get; init; }
  public PersonId PersonId { get; init; }
  public Details Details { get; init; }
  public QuartzJobId? QuartzJobId { get; init; }
  public ReminderTime ReminderTime { get; init; }
  public AcknowledgedOn? AcknowledgedOn { get; init; }
  public CreatedOn CreatedOn { get; init; }
  public UpdatedOn UpdatedOn { get; init; }
}
