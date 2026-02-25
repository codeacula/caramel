using System.Collections.Concurrent;
using System.Net.WebSockets;

using Caramel.API.Services;

using Caramel.Cache;
using Caramel.GRPC;

WebApplicationBuilder webAppBuilder = WebApplication.CreateBuilder(args);
var configuration = webAppBuilder.Configuration;

string redisConnectionString = configuration.GetConnectionString("Redis")
  ?? throw new InvalidOperationException("Redis connection string is missing.");

_ = webAppBuilder.Services.AddControllers();
_ = webAppBuilder.Services
  .AddCacheServices(redisConnectionString);
_ = webAppBuilder.Services.AddGrpcClientServices();

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
