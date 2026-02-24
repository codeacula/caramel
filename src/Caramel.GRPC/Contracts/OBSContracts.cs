using System.Runtime.Serialization;

namespace Caramel.GRPC.Contracts;

[DataContract]
public sealed record OBSStatusDTO
{
  [DataMember(Order = 1)]
  public required bool IsConnected { get; init; }

  [DataMember(Order = 2)]
  public string? CurrentScene { get; init; }
}

[DataContract]
public sealed record SetOBSSceneRequest
{
  [DataMember(Order = 1)]
  public required string SceneName { get; init; }
}
