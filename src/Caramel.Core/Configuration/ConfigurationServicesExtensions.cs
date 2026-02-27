using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Caramel.Core.Configuration;

/// <summary>
/// Extension methods for registering and validating configuration options in dependency injection.
/// Provides centralized configuration management with compile-time type safety.
/// </summary>
public static class ConfigurationServicesExtensions
{
  /// <summary>
  /// Registers all Caramel configuration options in the dependency injection container.
  /// Validates all options on startup and throws ConfigurationValidationException if any fail.
  /// </summary>
  /// <param name="services">The service collection to register options with.</param>
  /// <param name="configuration">The application configuration.</param>
  /// <returns>The service collection for method chaining.</returns>
  /// <exception cref="ConfigurationValidationException">Thrown if any required configuration is invalid or missing.</exception>
  public static IServiceCollection AddCaramelOptions(
    this IServiceCollection services,
    IConfiguration configuration)
  {
    if (services == null)
      throw new ArgumentNullException(nameof(services));

    if (configuration == null)
      throw new ArgumentNullException(nameof(configuration));

    // Register all configuration option classes
    RegisterAndValidateOption<CaramelAiConfigOptions>(services, configuration, CaramelAiConfigOptions.SectionName);
    RegisterAndValidateOption<DiscordConfigOptions>(services, configuration, DiscordConfigOptions.SectionName);
    RegisterAndValidateOption<TwitchConfigOptions>(services, configuration, TwitchConfigOptions.SectionName);
    RegisterAndValidateOption<OBSConfigOptions>(services, configuration, OBSConfigOptions.SectionName);
    RegisterAndValidateOption<GrpcConfigOptions>(services, configuration, GrpcConfigOptions.SectionName);
    RegisterAndValidateOption<DatabaseConfigOptions>(services, configuration, DatabaseConfigOptions.SectionName);

    return services;
  }

  /// <summary>
  /// Registers a single configuration option type with validation.
  /// </summary>
  private static void RegisterAndValidateOption<TOptions>(
    IServiceCollection services,
    IConfiguration configuration,
    string sectionName)
    where TOptions : ConfigurationOptions, new()
  {
    // Configure the options with the IOptions<T> pattern
    services.Configure<TOptions>(configuration.GetSection(sectionName));

    // Add post-configuration validation
    services.AddOptions<TOptions>()
      .Validate(options =>
      {
        var errors = options.Validate().ToList();
        return errors.Count == 0;
      }, $"Configuration validation failed for '{sectionName}'")
      .ValidateOnStart();
  }

  /// <summary>
  /// Manually validates a configuration options instance.
  /// Useful for testing or for validating options outside of DI context.
  /// </summary>
  /// <typeparam name="TOptions">The configuration options type to validate.</typeparam>
  /// <param name="options">The options instance to validate.</param>
  /// <param name="sectionName">The name of the configuration section (for error messages).</param>
  /// <returns>True if validation passes, false otherwise.</returns>
  public static bool ValidateOptions<TOptions>(TOptions options, string sectionName)
    where TOptions : ConfigurationOptions
  {
    if (options == null)
      throw new ArgumentNullException(nameof(options));

    var errors = options.Validate().ToList();
    if (errors.Count > 0)
    {
      throw ConfigurationValidationException.CreateFromErrors(sectionName, errors);
    }

    return true;
  }
}
