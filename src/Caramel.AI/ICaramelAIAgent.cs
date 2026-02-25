using Caramel.AI.DTOs;
using Caramel.AI.Requests;

namespace Caramel.AI;

public interface ICaramelAIAgent
{
  /// <summary>
  /// Creates a new request builder for custom configuration.
  /// </summary>
  IAIRequestBuilder CreateRequest();

  IAIRequestBuilder CreateToolPlanningRequest(
    IEnumerable<ChatMessageDTO> messages,
    string userTimezone);

  IAIRequestBuilder CreateResponseRequest(
    IEnumerable<ChatMessageDTO> messages,
    string actionsSummary,
    string userTimezone);
}
