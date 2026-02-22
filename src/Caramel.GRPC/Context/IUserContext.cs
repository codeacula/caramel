using Caramel.Domain.People.Models;

namespace Caramel.GRPC.Context;

public interface IUserContext
{
  Person? Person { get; set; }
}
