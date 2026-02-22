namespace Caramel.AI.Config;

public record CaramelAIConfig
{
  public string ModelId { get; init; } = "";
  public string Endpoint { get; init; } = "";
  public string ApiKey { get; init; } = "";
}
