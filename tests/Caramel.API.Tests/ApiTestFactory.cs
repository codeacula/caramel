using Caramel.Core.People;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

using Moq;

using StackExchange.Redis;

namespace Caramel.API.Tests;

public sealed class ApiTestFactory : WebApplicationFactory<ICaramelAPI>
{
  protected override void ConfigureWebHost(IWebHostBuilder builder)
  {
    _ = builder.UseWebRoot(Path.Combine(AppContext.BaseDirectory, "wwwroot"));

    _ = builder.ConfigureServices(services =>
    {
      var mockCache = new Mock<IPersonCache>();
      _ = services.AddSingleton(mockCache.Object);

      var mockRedis = new Mock<IConnectionMultiplexer>();
      _ = services.AddSingleton(mockRedis.Object);

      var hostedServices = services
        .Where(descriptor => descriptor.ImplementationType?.Name == "TwitchChatRelayService")
        .ToList();

      foreach (var descriptor in hostedServices)
      {
        _ = services.Remove(descriptor);
      }
    });
  }
}
