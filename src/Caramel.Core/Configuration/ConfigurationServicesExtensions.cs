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
    ArgumentNullException.ThrowIfNull(services);

    ArgumentNullException.ThrowIfNull(configuration);

    // Register configuration option classes only when their sections are present.
    RegisterAndValidateOptionIfPresent<CaramelAiConfigOptions>(services, configuration, CaramelAiConfigOptions.SectionName);
    RegisterAndValidateOptionIfPresent<DiscordConfigOptions>(services, configuration, DiscordConfigOptions.SectionName);
    RegisterAndValidateOptionIfPresent<TwitchConfigOptions>(services, configuration, TwitchConfigOptions.SectionName);
    RegisterAndValidateOptionIfPresent<OBSConfigOptions>(services, configuration, OBSConfigOptions.SectionName);
    RegisterAndValidateOptionIfPresent<GrpcConfigOptions>(services, configuration, GrpcConfigOptions.SectionName);
    RegisterAndValidateOption<DatabaseConfigOptions>(services, configuration, DatabaseConfigOptions.SectionName);

    return services;
  }

  /// <summary>
  /// Registers a single configuration option type with validation.
  /// </summary>
  /// <param name="services"></param>
  /// <param name="configuration"></param>
  /// <param name="sectionName"></param>
  private static void RegisterAndValidateOptionIfPresent<TOptions>(
    IServiceCollection services,
    IConfiguration configuration,
    string sectionName)
    where TOptions : ConfigurationOptions, new()
  {
    var section = configuration.GetSection(sectionName);
    if (!section.Exists())
    {
      return;
    }

    RegisterAndValidateOption<TOptions>(services, configuration, sectionName);
  }

  /// <summary>
  /// Registers a single configuration option type with validation.
  /// </summary>
  /// <param name="services"></param>
  /// <param name="configuration"></param>
  /// <param name="sectionName"></param>
  private static void RegisterAndValidateOption<TOptions>(
    IServiceCollection services,
    IConfiguration configuration,
    string sectionName)
    where TOptions : ConfigurationOptions, new()
  {
    // Configure the options with the IOptions<T> pattern
    _ = services.Configure<TOptions>(configuration.GetSection(sectionName));

    // Add post-configuration validation with detailed error messages
    _ = services.AddOptions<TOptions>()
      .Validate(options =>
      {
        var errors = options.Validate().ToList();
        if (errors.Count > 0)
        {
          // Include detailed validation errors in the exception
          var errorDetails = string.Join("; ", errors);
          throw ConfigurationValidationException.CreateFromErrors(sectionName, errors);
        }
        return true;
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
  /// <exception cref="ConfigurationValidationException"></exception>
  public static bool ValidateOptions<TOptions>(TOptions options, string sectionName)
    where TOptions : ConfigurationOptions
  {
    ArgumentNullException.ThrowIfNull(options);

    var errors = options.Validate().ToList();
    return errors.Count > 0 ? throw ConfigurationValidationException.CreateFromErrors(sectionName, errors) : true;
  }
}
