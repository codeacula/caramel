using System.Text.Json.Serialization;

namespace Caramel.AI.Models;

public sealed record ToolPlan
{
  [JsonPropertyName("tool_calls")]
  public List<PlannedToolCall> ToolCalls { get; init; } = [];
}
