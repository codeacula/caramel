using Caramel.AI.Models;
using Caramel.AI.Requests;

namespace Caramel.AI.Tooling;

public sealed record ToolPlanValidationResult
{
  public List<PlannedToolCall> ApprovedCalls { get; init; } = [];
  public List<ToolCallResult> BlockedCalls { get; init; } = [];
}
