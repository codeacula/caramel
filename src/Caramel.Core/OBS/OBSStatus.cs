namespace Caramel.Core.OBS;

public sealed record OBSStatus
{
  public required bool IsConnected { get; init; }
  public string? CurrentScene { get; init; }
}
