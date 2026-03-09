using Caramel.Core.Configuration;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Caramel.Core.Tests.Configuration;

public sealed class ConfigurationServicesExtensionsTests
{
  [Fact]
  public void AddCaramelOptionsAllowsTwitchConfigWithoutRuntimeTokens()
  {
    // Arrange
    var services = new ServiceCollection();

    var configuration = new ConfigurationBuilder()
      .AddInMemoryCollection(new Dictionary<string, string?>
      {
        ["CaramelAiConfig:ModelId"] = "gpt-4o-mini",
        ["CaramelAiConfig:Endpoint"] = "https://api.example.com/v1",
        ["CaramelAiConfig:ApiKey"] = "test-api-key-12345",

        ["DiscordConfig:Token"] = "abc.def.ghi",
        ["DiscordConfig:PublicKey"] = "0123456789abcdef0123456789abcdef",

        ["TwitchConfig:ClientId"] = "test-client-id-12345",
        ["TwitchConfig:ClientSecret"] = "test-client-secret-12345",
        ["TwitchConfig:EncryptionKey"] = new string('a', 32),
        ["TwitchConfig:OAuthCallbackUrl"] = "https://localhost:8083/auth/twitch/callback",

        ["ObsConfig:Url"] = "ws://localhost:4455",
        ["ObsConfig:Password"] = "super-secret-password",

        ["GrpcHostConfig:Host"] = "localhost",
        ["GrpcHostConfig:Port"] = "5270",
        ["GrpcHostConfig:ApiToken"] = "test-grpc-api-token-12345",
        ["GrpcHostConfig:UseHttps"] = "false",

        ["ConnectionStrings:Caramel"] = "Host=localhost;Database=caramel_db;Username=caramel;Password=caramel",
        ["ConnectionStrings:Quartz"] = "Host=localhost;Database=caramel_db;Username=caramel;Password=caramel",
        ["ConnectionStrings:Redis"] = "localhost:6379,password=caramel_redis",
      })
      .Build();

    // Act
    var act = () =>
    {
      _ = services.AddCaramelOptions(configuration);
      using var provider = services.BuildServiceProvider(validateScopes: true);

      // Force options validation on resolution
      _ = provider.GetRequiredService<Microsoft.Extensions.Options.IOptions<CaramelAiConfigOptions>>().Value;
      _ = provider.GetRequiredService<Microsoft.Extensions.Options.IOptions<DiscordConfigOptions>>().Value;
      _ = provider.GetRequiredService<Microsoft.Extensions.Options.IOptions<TwitchConfigOptions>>().Value;
      _ = provider.GetRequiredService<Microsoft.Extensions.Options.IOptions<OBSConfigOptions>>().Value;
      _ = provider.GetRequiredService<Microsoft.Extensions.Options.IOptions<GrpcConfigOptions>>().Value;
      _ = provider.GetRequiredService<Microsoft.Extensions.Options.IOptions<DatabaseConfigOptions>>().Value;
    };

    // Assert
    _ = act.Should().NotThrow();
  }

  [Fact]
  public void ValidateOptionsAllowsTwitchConfigWithoutRuntimeTokens()
  {
    // Arrange
    var options = new TwitchConfigOptions
    {
      ClientId = "test-client-id-12345",
      ClientSecret = "test-client-secret-12345",
      EncryptionKey = new string('a', 32),
      OAuthCallbackUrl = "https://localhost:8083/auth/twitch/callback",
    };

    // Act
    var act = () => ConfigurationServicesExtensions.ValidateOptions(options, TwitchConfigOptions.SectionName);

    // Assert
    _ = act.Should().NotThrow();
  }

  [Fact]
  public void ValidateOptionsStillRejectsMissingRequiredAppOAuthSettings()
  {
    // Arrange
    var options = new TwitchConfigOptions
    {
      ClientId = "test-client-id-12345",
      EncryptionKey = new string('a', 32),
      OAuthCallbackUrl = "https://localhost:8083/auth/twitch/callback",
    };

    // Act
    var act = () => ConfigurationServicesExtensions.ValidateOptions(options, TwitchConfigOptions.SectionName);

    // Assert
    _ = act.Should()
      .Throw<ConfigurationValidationException>()
      .WithMessage("*ClientSecret is required*");
  }
}
