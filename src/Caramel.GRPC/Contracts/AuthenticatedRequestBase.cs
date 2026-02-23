using System.Runtime.Serialization;

using Caramel.Domain.Common.Enums;
using Caramel.Domain.People.ValueObjects;

namespace Caramel.GRPC.Contracts;

[DataContract]
public abstract record AuthenticatedRequestBase : IAuthenticatedRequest
{
  [DataMember(Order = 101)]
  public required Platform Platform { get; init; }

  [DataMember(Order = 102)]
  public required string PlatformUserId { get; init; }

  [DataMember(Order = 103)]
  public required string Username { get; init; }

  public PlatformId ToPlatformId()
  {
    return new(Username, PlatformUserId, Platform);
  }
}
