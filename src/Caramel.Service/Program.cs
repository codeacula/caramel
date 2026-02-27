using Caramel.AI;
using Caramel.Application;
using Caramel.Cache;
using Caramel.Core.Configuration;
using Caramel.Database;
using Caramel.GRPC;
using Caramel.Service;

WebApplicationBuilder webAppBuilder = WebApplication.CreateBuilder(args);
var configuration = webAppBuilder.Configuration;

_ = webAppBuilder.Services.AddControllers();
_ = webAppBuilder.Services.AddCaramelOptions(configuration);
_ = webAppBuilder.Services
  .AddDatabaseServices(configuration)
  .AddCacheServices(configuration.GetConnectionString("Redis")!)
  .AddRequiredServices(configuration)
  .AddAiServices(configuration)
  .AddApplicationServices()
  .AddGrpcServerServices();

WebApplication app = webAppBuilder.Build();

// Apply database migrations
await app.Services.MigrateDatabaseAsync();

_ = app.UseRequestLocalization();

_ = app.MapControllers();
_ = app.AddGrpcServerServices();

await app.RunAsync();
