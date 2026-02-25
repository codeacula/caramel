using Caramel.AI.DTOs;

namespace Caramel.AI.Tooling;

public sealed record ToolPlanValidationContext(
  IDictionary<string, object> Plugins,
  IReadOnlyList<ChatMessageDTO> ConversationHistory);
