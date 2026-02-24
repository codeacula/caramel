using FluentResults;

using Microsoft.Extensions.Options;

using OBSWebsocketDotNet;

namespace Caramel.Service.OBS;

public sealed class OBSService : IHostedService, IOBSService, IDisposable
{
  private readonly OBSWebsocket _obs;
  private readonly OBSConfig _config;
  private readonly ILogger<OBSService> _logger;

  public bool IsConnected => _obs.IsConnected;

  public OBSService(IOptions<OBSConfig> config, ILogger<OBSService> logger)
  {
    _config = config.Value;
    _logger = logger;
    _obs = new OBSWebsocket();
    _obs.Connected += OnConnected;
    _obs.Disconnected += OnDisconnected;
  }

  public Task StartAsync(CancellationToken cancellationToken)
  {
    OBSLogs.Connecting(_logger, _config.Url);
    // ConnectAsync is fire-and-forget (returns void); run on background thread so
    // it doesn't block the hosted service startup.
    _ = Task.Run(() =>
    {
      try
      {
        _obs.ConnectAsync(_config.Url, _config.Password);
      }
      catch (Exception ex)
      {
        OBSLogs.ConnectFailed(_logger, _config.Url, ex.Message);
      }
    }, cancellationToken);

    return Task.CompletedTask;
  }

  public Task StopAsync(CancellationToken cancellationToken)
  {
    if (_obs.IsConnected)
    {
      _obs.Disconnect();
    }

    return Task.CompletedTask;
  }

  public Task<Result> SetCurrentProgramSceneAsync(string sceneName, CancellationToken cancellationToken = default)
  {
    if (!_obs.IsConnected)
    {
      return Task.FromResult(Result.Fail("OBS is not connected."));
    }

    try
    {
      _obs.SetCurrentProgramScene(sceneName);
      OBSLogs.SceneSwitched(_logger, sceneName);
      return Task.FromResult(Result.Ok());
    }
    catch (Exception ex)
    {
      OBSLogs.SceneSwitchFailed(_logger, sceneName, ex.Message);
      return Task.FromResult(Result.Fail(ex.Message));
    }
  }

  public Task<Result<string>> GetCurrentProgramSceneAsync(CancellationToken cancellationToken = default)
  {
    if (!_obs.IsConnected)
    {
      return Task.FromResult(Result.Fail<string>("OBS is not connected."));
    }

    try
    {
      var scene = _obs.GetCurrentProgramScene();
      return Task.FromResult(Result.Ok(scene));
    }
    catch (Exception ex)
    {
      return Task.FromResult(Result.Fail<string>(ex.Message));
    }
  }

  private void OnConnected(object? sender, EventArgs e)
    => OBSLogs.Connected(_logger, _config.Url);

  private void OnDisconnected(object? sender, OBSWebsocketDotNet.Communication.ObsDisconnectionInfo e)
    => OBSLogs.Disconnected(_logger, e.DisconnectReason ?? "unknown");

  public void Dispose()
  {
    _obs.Connected -= OnConnected;
    _obs.Disconnected -= OnDisconnected;
  }
}

internal static partial class OBSLogs
{
  [LoggerMessage(EventId = 4000, Level = LogLevel.Information, Message = "Connecting to OBS at {Url}")]
  public static partial void Connecting(ILogger logger, string url);

  [LoggerMessage(EventId = 4001, Level = LogLevel.Information, Message = "Connected to OBS at {Url}")]
  public static partial void Connected(ILogger logger, string url);

  [LoggerMessage(EventId = 4002, Level = LogLevel.Warning, Message = "Failed to connect to OBS at {Url}: {Error}")]
  public static partial void ConnectFailed(ILogger logger, string url, string error);

  [LoggerMessage(EventId = 4003, Level = LogLevel.Warning, Message = "Disconnected from OBS: {Reason}")]
  public static partial void Disconnected(ILogger logger, string reason);

  [LoggerMessage(EventId = 4004, Level = LogLevel.Information, Message = "Switched OBS scene to '{SceneName}'")]
  public static partial void SceneSwitched(ILogger logger, string sceneName);

  [LoggerMessage(EventId = 4005, Level = LogLevel.Warning, Message = "Failed to switch OBS scene to '{SceneName}': {Error}")]
  public static partial void SceneSwitchFailed(ILogger logger, string sceneName, string error);
}
