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

  /// <summary>UTC ticks of when the configuration was first created.</summary>
  [DataMember(Order = 4)]
  public required long ConfiguredOnTicks { get; init; }

  /// <summary>UTC ticks of when the configuration was last updated.</summary>
  [DataMember(Order = 5)]
  public required long UpdatedOnTicks { get; init; }

  /// <summary>Bot account tokens (null if not authenticated).</summary>
  [DataMember(Order = 6, IsRequired = false)]
  public TwitchAccountTokensDTO? BotTokens { get; init; }

  /// <summary>Broadcaster account tokens (null if not authenticated).</summary>
  [DataMember(Order = 7, IsRequired = false)]
  public TwitchAccountTokensDTO? BroadcasterTokens { get; init; }
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
public sealed record TwitchAccountTokensDTO
{
  [DataMember(Order = 1)]
  public required string UserId { get; init; }

  [DataMember(Order = 2)]
  public required string Login { get; init; }

  [DataMember(Order = 3)]
  public required bool HasRefreshToken { get; init; }

  [DataMember(Order = 4)]
  public required long ExpiresAtTicks { get; init; }
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

/// <summary>Request to add or update the broadcaster account OAuth token.</summary>
[DataContract]
public sealed record LinkBroadcasterTokenRequest
{
  [DataMember(Order = 1)]
  public required string BroadcasterUserId { get; init; }

  [DataMember(Order = 2)]
  public required string BroadcasterLogin { get; init; }

  [DataMember(Order = 3)]
  public required string AccessToken { get; init; }

  [DataMember(Order = 4)]
  public string? RefreshToken { get; init; }

  [DataMember(Order = 5)]
  public required long ExpiresAtTicks { get; init; }
}
