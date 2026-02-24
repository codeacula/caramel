using System.Runtime.Serialization;

using Caramel.Domain.Common.Enums;
using Caramel.Domain.People.ValueObjects;

namespace Caramel.GRPC.Contracts;

[DataContract]
public sealed record NewMessageRequest : IAuthenticatedRequest
{
  [DataMember(Order = 1)]
  public required string Username { get; init; }

  [DataMember(Order = 2)]
  public required string PlatformUserId { get; init; }

  [DataMember(Order = 3)]
  public required Platform Platform { get; init; }

  [DataMember(Order = 4)]
  public required string Content { get; init; }

  public PlatformId ToPlatformId()
  {
    return new(Username, PlatformUserId, Platform);
  }
}
