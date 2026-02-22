using Caramel.Domain.Common.ValueObjects;

namespace Caramel.Application.Conversations;

public sealed record Reply
{
  public required Content Content { get; init; }
  public required CreatedOn CreatedOn { get; init; }
  public required UpdatedOn UpdatedOn { get; init; }
}
