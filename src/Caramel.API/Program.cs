using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

using Caramel.Cache;
using Caramel.Core.Twitch;
using Caramel.GRPC;

using StackExchange.Redis;

WebApplicationBuilder webAppBuilder = WebApplication.CreateBuilder(args);
var configuration = webAppBuilder.Configuration;

string redisConnectionString = configuration.GetConnectionString("Redis")
  ?? throw new InvalidOperationException("Redis connection string is missing.");

_ = webAppBuilder.Services.AddControllers();
_ = webAppBuilder.Services
  .AddCacheServices(redisConnectionString)
  .AddGrpcClientServices();

// WebSocket connection registry -- shared between the endpoint and the background broadcaster
var socketRegistry = new ConcurrentDictionary<string, WebSocket>();
_ = webAppBuilder.Services.AddSingleton(socketRegistry);

// Background service: subscribes to Redis pub/sub and fans out to all WebSocket clients
_ = webAppBuilder.Services.AddHostedService<TwitchChatRelayService>();

// CORS -- allow the Vite dev server and Caddy proxy to connect
_ = webAppBuilder.Services.AddCors(options =>
{
  options.AddPolicy("ViteDev", policy =>
    policy
      .WithOrigins("http://localhost:5173", "http://localhost:8080")
      .AllowAnyHeader()
      .AllowAnyMethod());
});

WebApplication app = webAppBuilder.Build();

_ = app.UseCors("ViteDev");
_ = app.UseRequestLocalization();
_ = app.UseWebSockets(new WebSocketOptions { KeepAliveInterval = TimeSpan.FromSeconds(30) });

if (app.Environment.IsDevelopment())
{
  _ = app.MapOpenApi();
}

_ = app.MapControllers();
_ = app.UseHttpsRedirection();
_ = app.UseDefaultFiles();
_ = app.UseStaticFiles();

// WebSocket endpoint -- clients connect here to receive live Twitch chat messages
app.Map("/ws/chat", async (HttpContext context, ConcurrentDictionary<string, WebSocket> registry) =>
{
  if (!context.WebSockets.IsWebSocketRequest)
  {
    context.Response.StatusCode = StatusCodes.Status400BadRequest;
    await context.Response.WriteAsync("Expected a WebSocket request.");
    return;
  }

  using var webSocket = await context.WebSockets.AcceptWebSocketAsync();
  var id = Guid.NewGuid().ToString();
  _ = registry.TryAdd(id, webSocket);

  try
  {
    // Keep the socket alive by draining incoming frames until the client disconnects.
    // We ignore any incoming data -- this is a one-way broadcast channel.
    var buffer = new byte[256];
    while (webSocket.State == WebSocketState.Open)
    {
      var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), context.RequestAborted);
      if (result.MessageType == WebSocketMessageType.Close)
      {
        await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", context.RequestAborted);
      }
    }
  }
  finally
  {
    _ = registry.TryRemove(id, out _);
  }
});

// SPA fallback: serve index.html for any unmatched routes (client-side routing)
app.MapFallbackToFile("index.html");

await app.RunAsync();

// ---------------------------------------------------------------------------

/// <summary>
/// Hosted service that subscribes to the Redis pub/sub channel for Twitch chat messages
/// and broadcasts each payload to every connected WebSocket client.
/// </summary>
internal sealed class TwitchChatRelayService(
  IConnectionMultiplexer redis,
  ConcurrentDictionary<string, WebSocket> registry,
  ILogger<TwitchChatRelayService> logger) : BackgroundService
{
  protected override async Task ExecuteAsync(CancellationToken stoppingToken)
  {
    var subscriber = redis.GetSubscriber();

    await subscriber.SubscribeAsync(
      RedisChannel.Literal(TwitchChatMessage.RedisChannel),
      (_, value) => OnRedisMessage(value));

    TwitchChatRelayLogs.Subscribed(logger, TwitchChatMessage.RedisChannel);

    // Keep the service alive until the host shuts down
    await Task.Delay(Timeout.Infinite, stoppingToken);
  }

  private void OnRedisMessage(RedisValue value)
  {
    if (!value.HasValue)
    {
      return;
    }

    var payload = Encoding.UTF8.GetBytes(value.ToString());

    // Fan-out: send to every connected WebSocket client
    foreach (var (id, socket) in registry)
    {
      if (socket.State != WebSocketState.Open)
      {
        _ = registry.TryRemove(id, out _);
        continue;
      }

      _ = BroadcastAsync(id, socket, payload);
    }
  }

  private async Task BroadcastAsync(string id, WebSocket socket, byte[] payload)
  {
    try
    {
      await socket.SendAsync(
        new ArraySegment<byte>(payload),
        WebSocketMessageType.Text,
        endOfMessage: true,
        CancellationToken.None);
    }
    catch (Exception ex)
    {
      TwitchChatRelayLogs.BroadcastFailed(logger, id, ex.Message);
      _ = registry.TryRemove(id, out _);
    }
  }

  public override async Task StopAsync(CancellationToken cancellationToken)
  {
    TwitchChatRelayLogs.Stopping(logger);
    await base.StopAsync(cancellationToken);
  }
}

/// <summary>
/// Structured log messages for <see cref="TwitchChatRelayService"/>.
/// </summary>
internal static partial class TwitchChatRelayLogs
{
  [LoggerMessage(Level = LogLevel.Information, Message = "TwitchChatRelayService subscribed to Redis channel '{Channel}'")]
  public static partial void Subscribed(ILogger logger, string channel);

  [LoggerMessage(Level = LogLevel.Warning, Message = "Failed to broadcast to WebSocket client {ClientId}: {Error}")]
  public static partial void BroadcastFailed(ILogger logger, string clientId, string error);

  [LoggerMessage(Level = LogLevel.Information, Message = "TwitchChatRelayService stopping")]
  public static partial void Stopping(ILogger logger);
}
