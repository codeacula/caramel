using Caramel.Domain.People.Models;

namespace Caramel.GRPC.Context;

public class UserContext : IUserContext
{
  public Person? Person { get; set; }
}
