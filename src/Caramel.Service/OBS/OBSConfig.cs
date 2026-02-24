namespace Caramel.Service.OBS;

public sealed record OBSConfig
{
  public string Url { get; init; } = "ws://localhost:4455";
  public string Password { get; init; } = string.Empty;
}
