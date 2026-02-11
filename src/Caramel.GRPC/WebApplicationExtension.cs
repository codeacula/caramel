using Caramel.GRPC.Service;

using Microsoft.AspNetCore.Builder;

namespace Caramel.GRPC;

public static class WebApplicationExtension
{
  public static WebApplication AddGrpcServerServices(this WebApplication app)
  {
    _ = app.MapGrpcService<CaramelGrpcService>();

    return app;
  }
}
