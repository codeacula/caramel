using System.Runtime.Serialization;

namespace Caramel.GRPC.Contracts;

[DataContract]
public sealed record TwitchSetupDTO
{
  [DataMember(Order = 1)]
  public required string BotUserId { get; init; }

  [DataMember(Order = 2)]
  public required string BotLogin { get; init; }

  [DataMember(Order = 3)]
  public required List<TwitchChannelDTO> Channels { get; init; }
}

[DataContract]
public sealed record TwitchChannelDTO
{
  [DataMember(Order = 1)]
  public required string UserId { get; init; }

  [DataMember(Order = 2)]
  public required string Login { get; init; }
}

[DataContract]
public sealed record SaveTwitchSetupRequest
{
  [DataMember(Order = 1)]
  public required string BotUserId { get; init; }

  [DataMember(Order = 2)]
  public required string BotLogin { get; init; }

  [DataMember(Order = 3)]
  public required List<TwitchChannelDTO> Channels { get; init; }
}
