using Microsoft.Extensions.Options;

namespace Caramel.Core.Configuration;

/// <summary>
/// Example service demonstrating how to inject and use IOptions&lt;T&gt; configuration.
/// This shows the recommended pattern for accessing configuration in services.
/// </summary>
/// <remarks>
/// Services should always receive configuration through constructor injection of IOptions&lt;T&gt;
/// rather than directly accessing IConfiguration. This provides several benefits:
/// - Compile-time type safety
/// - IntelliSense support
/// - Easier testing (mock IOptions instead of IConfiguration)
/// - Configuration is validated at startup
/// </remarks>
public sealed class ConfigurationExampleService
{
  private readonly CaramelAiConfigOptions _aiConfig;
  private readonly DiscordConfigOptions _discordConfig;
  private readonly TwitchConfigOptions _twitchConfig;
  private readonly GrpcConfigOptions _grpcConfig;

  /// <summary>
  /// Constructor demonstrating standard DI pattern for configuration options.
  /// </summary>
  public ConfigurationExampleService(
    IOptions<CaramelAiConfigOptions> aiConfig,
    IOptions<DiscordConfigOptions> discordConfig,
    IOptions<TwitchConfigOptions> twitchConfig,
    IOptions<GrpcConfigOptions> grpcConfig)
  {
    _aiConfig = aiConfig?.Value ?? throw new ArgumentNullException(nameof(aiConfig));
    _discordConfig = discordConfig?.Value ?? throw new ArgumentNullException(nameof(discordConfig));
    _twitchConfig = twitchConfig?.Value ?? throw new ArgumentNullException(nameof(twitchConfig));
    _grpcConfig = grpcConfig?.Value ?? throw new ArgumentNullException(nameof(grpcConfig));
  }

  /// <summary>
  /// Example method showing how to use configuration in business logic.
  /// </summary>
  public void LogConfigurationSummary()
  {
    Console.WriteLine("=== Configuration Summary (secrets masked) ===");
    Console.WriteLine(_aiConfig.ToString());
    Console.WriteLine(_discordConfig.ToString());
    Console.WriteLine(_twitchConfig.ToString());
    Console.WriteLine(_grpcConfig.ToString());
  }

  /// <summary>
  /// Example method showing how to access specific configuration values.
  /// </summary>
  public string GetGrpcServerUrl()
  {
    var protocol = _grpcConfig.UseHttps ? "https" : "http";
    return $"{protocol}://{_grpcConfig.Host}:{_grpcConfig.Port}";
  }

  /// <summary>
  /// Example method showing how to access API configuration.
  /// </summary>
  public string GetAiModelInfo()
  {
    return $"Using model '{_aiConfig.ModelId}' at '{_aiConfig.Endpoint}'";
  }

  /// <summary>
  /// Example method showing how to access optional configuration properties.
  /// </summary>
  public bool IsMessageTheAiEnabled()
  {
    return !string.IsNullOrEmpty(_twitchConfig.MessageTheAiRewardId);
  }
}
