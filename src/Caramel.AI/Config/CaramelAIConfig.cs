namespace Caramel.AI.Config;

public record CaramelAiConfig
{
  public string ModelId { get; init; } = "";
  public string Endpoint { get; init; } = "";
  public string ApiKey { get; init; } = "";
}
