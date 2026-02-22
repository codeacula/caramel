using Caramel.Cache;
using Caramel.Discord;
using Caramel.GRPC;

using NetCord.Hosting.AspNetCore;
using NetCord.Hosting.Services;

var builder = WebApplication.CreateBuilder(args);

_ = builder.Configuration.AddEnvironmentVariables()
      .AddUserSecrets<ICaramelDiscord>();

var redisConnection = builder.Configuration.GetConnectionString("Redis")
  ?? throw new InvalidOperationException("Redis connection string not found");

// Add services to the container.
_ = builder.Services
  .AddCacheServices(redisConnection)
      .AddGrpcClientServices()
      .AddDiscordServices();

var app = builder.Build();

_ = app.AddModules(typeof(ICaramelDiscord).Assembly);
_ = app.UseHttpInteractions("/interactions");
_ = app.UseRequestLocalization();

await app.RunAsync();
