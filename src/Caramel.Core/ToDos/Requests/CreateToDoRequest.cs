using System.Runtime.Serialization;

using Caramel.Domain.People.ValueObjects;

namespace Caramel.Core.ToDos.Requests;

[DataContract]
public sealed record CreateToDoRequest
{
  [DataMember(Order = 1)]
  public required PlatformId PlatformId { get; init; }

  [DataMember(Order = 2)]
  public required string Title { get; init; }

  [DataMember(Order = 3)]
  public required string Description { get; init; }

  [DataMember(Order = 4)]
  public DateTime? ReminderDate { get; init; }
}
