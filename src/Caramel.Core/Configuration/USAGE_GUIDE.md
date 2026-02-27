/// <summary>
/// CONFIGURATION VALIDATION INFRASTRUCTURE - COMPREHENSIVE GUIDE
/// 
/// This guide demonstrates how to use the IOptions&lt;T&gt; pattern for standardized
/// configuration validation in Caramel applications.
/// 
/// BENEFITS:
/// ✓ Compile-time type safety - Properties are strongly typed
/// ✓ Runtime validation - Clear error messages for missing/invalid values
/// ✓ IntelliSense support - Full IDE autocomplete for configuration
/// ✓ Self-documenting - XML comments on every property
/// ✓ Startup validation - Issues caught immediately on application start
/// ✓ Secrets protection - Automatic masking in ToString() for safe logging
/// ✓ Flexible validation - Both attribute-based and custom validation rules
/// </summary>

// SECTION 1: HOW TO REGISTER IN DEPENDENCY INJECTION
// ===================================================
//
// In your Program.cs:
//
//   var builder = WebApplication.CreateBuilder(args);
//
//   // Register all configuration options with validation
//   builder.Services.AddCaramelOptions(builder.Configuration);
//
//   var app = builder.Build();
//   // ... rest of setup
//   await app.RunAsync();
//
// That's it! Configuration is now validated on startup.
// If any required configuration is missing, a ConfigurationValidationException
// will be thrown with clear error messages before the app starts.


// SECTION 2: HOW TO INJECT CONFIGURATION IN SERVICES
// ===================================================
//
// Inject IOptions<T> in your service constructor:
//
//   public class MyService
//   {
//     private readonly CaramelAiConfigOptions _aiConfig;
//
//     // Constructor injection
//     public MyService(IOptions<CaramelAiConfigOptions> aiConfig)
//     {
//       _aiConfig = aiConfig?.Value ?? throw new ArgumentNullException(nameof(aiConfig));
//     }
//
//     public void DoSomething()
//     {
//       // Use the configuration
//       var endpoint = _aiConfig.Endpoint;
//       var modelId = _aiConfig.ModelId;
//       // Configuration is strongly typed and validated at startup
//     }
//   }
//
// Alternative: If you need to listen for configuration changes:
//
//   public class MyService
//   {
//     public MyService(IOptionsMonitor<CaramelAiConfigOptions> aiConfigMonitor)
//     {
//       var currentConfig = aiConfigMonitor.CurrentValue;
//       // Subscribe to changes
//       aiConfigMonitor.OnChange(newConfig => { /* ... */ });
//     }
//   }


// SECTION 3: APPSETTINGS.JSON STRUCTURE
// ======================================
//
// {
//   "CaramelAiConfig": {
//     "ModelId": "gpt-4",
//     "Endpoint": "https://api.openai.com/v1",
//     "ApiKey": "sk-...",
//     "SystemPrompt": "You are a helpful AI assistant.",
//     "MaxTokens": 2000,
//     "Temperature": 0.7
//   },
//   "DiscordConfig": {
//     "Token": "MzAwMjU5...",
//     "PublicKey": "abcd1234...",
//     "BotName": "Caramel",
//     "CommandPrefix": "!"
//   },
//   "TwitchConfig": {
//     "ClientId": "...",
//     "ClientSecret": "...",
//     "AccessToken": "...",
//     "RefreshToken": "...",
//     "EncryptionKey": "...",
//     "OAuthCallbackUrl": "http://localhost:5146/auth/twitch/callback",
//     "BotUserId": "123456",
//     "ChannelIds": "987654,555555",
//     "MessageTheAiRewardId": "guid-here"
//   },
//   "ObsConfig": {
//     "Url": "ws://localhost:4455",
//     "Password": "websocket-password",
//     "ConnectionTimeoutSeconds": 10,
//     "AutoReconnect": true,
//     "MaxReconnectAttempts": 5
//   },
//   "GrpcHostConfig": {
//     "Host": "localhost",
//     "Port": 5270,
//     "ApiToken": "dev-api-token",
//     "UseHttps": true,
//     "ValidateSslCertificate": true,
//     "ConnectionTimeoutSeconds": 30
//   },
//   "ConnectionStrings": {
//     "Caramel": "Host=localhost;Database=caramel_db;Username=user;Password=pass",
//     "Quartz": "Host=localhost;Database=caramel_db;Username=user;Password=pass",
//     "Redis": "localhost:6379,password=secret"
//   }
// }


// SECTION 4: ENVIRONMENT-SPECIFIC CONFIGURATION
// ==============================================
//
// appsettings.Development.json
//   - Can have empty/dummy values for development
//   - Override any settings needed for development
//
// appsettings.Production.json
//   - Must have all required values
//   - More restrictive validation (SSL, HTTPS)
//
// appsettings.json
//   - Base configuration template
//   - Used as default if environment-specific file not found
//
// At runtime, ASP.NET Core merges these files:
// base + environment-specific = final configuration


// SECTION 5: CONFIGURATION CLASSES AND VALIDATION
// ================================================
//
// Each configuration class:
//
// 1. Inherits from ConfigurationOptions
// 2. Has a const SectionName for use in appsettings
// 3. Defines properties with:
//    - [Required] - Must have a value
//    - [StringLength] - Min/max length
//    - [Range] - Numeric bounds
//    - [Url] - Must be valid URL
// 4. Implements Validate() for complex rules
// 5. Implements ToString() that masks secrets
// 6. Has IsRequired property (true = must validate, false = optional)
//
// Available configuration classes:
// - CaramelAiConfigOptions - AI service settings
// - DiscordConfigOptions - Discord bot settings
// - TwitchConfigOptions - Twitch integration
// - OBSConfigOptions - OBS WebSocket (optional)
// - GrpcConfigOptions - gRPC service settings
// - DatabaseConfigOptions - Database connections


// SECTION 6: ERROR MESSAGES AND TROUBLESHOOTING
// ==============================================
//
// When configuration is invalid, you'll see errors like:
//
// Example 1: Missing required field
//   Configuration validation failed for 'CaramelAiConfig':
//   - ModelId is required
//   - ApiKey is required
//
// Example 2: Invalid format
//   Configuration validation failed for 'DiscordConfig':
//   - PublicKey must be a valid hexadecimal string
//   - Token appears to be invalid
//
// Example 3: Custom validation
//   Configuration validation failed for 'TwitchConfig':
//   - ChannelIds must contain at least one channel ID
//   - Invalid channel ID format: 'invalid@channel'
//
// Fix:
// 1. Check the error message for which field is wrong
// 2. See the property documentation for requirements
// 3. Update appsettings.json with correct values
// 4. Restart the application


// SECTION 7: LOGGING CONFIGURATION SAFELY
// ========================================
//
// All configuration classes implement ToString() to mask secrets:
//
//   public class MyService
//   {
//     private readonly ILogger<MyService> _logger;
//     private readonly CaramelAiConfigOptions _config;
//
//     public MyService(ILogger<MyService> logger, IOptions<CaramelAiConfigOptions> config)
//     {
//       _logger = logger;
//       _config = config.Value;
//     }
//
//     public void Initialize()
//     {
//       // Safe to log - ApiKey will show as *** instead of actual value
//       _logger.LogInformation("Initializing with {Config}", _config);
//     }
//   }
//
// Output: "Initializing with CaramelAiConfig { ModelId = gpt-4, Endpoint = ..., ... }"
// The ApiKey will never be logged, even in ToString()


// SECTION 8: VALIDATION IN TESTS
// ==============================
//
// For unit tests, you can manually create and validate options:
//
//   [TestClass]
//   public class ConfigurationTests
//   {
//     [TestMethod]
//     public void ValidCaramelAiConfig_ShouldPass()
//     {
//       var config = new CaramelAiConfigOptions
//       {
//         ModelId = "gpt-4",
//         Endpoint = "https://api.openai.com/v1",
//         ApiKey = "sk-test-key-1234567890",
//         SystemPrompt = "Test"
//       };
//
//       // Validate manually
//       var errors = config.Validate().ToList();
//       Assert.IsEmpty(errors);
//     }
//
//     [TestMethod]
//     public void MissingApiKey_ShouldFail()
//     {
//       var config = new CaramelAiConfigOptions
//       {
//         ModelId = "gpt-4",
//         Endpoint = "https://api.openai.com/v1"
//         // ApiKey is missing
//       };
//
//       var errors = config.Validate().ToList();
//       Assert.IsTrue(errors.Any(e => e.Contains("ApiKey")));
//     }
//   }


// SECTION 9: OPTIONAL CONFIGURATION
// ==================================
//
// Some configuration sections are optional:
//
// OBSConfigOptions.IsRequired = false
//   - If not configured, OBS features are disabled
//   - No error if not in appsettings.json
//
// To check if optional config is available:
//
//   public class StreamingService
//   {
//     private readonly OBSConfigOptions? _obsConfig;
//
//     public StreamingService(IOptions<OBSConfigOptions>? obsConfig)
//     {
//       _obsConfig = obsConfig?.Value;
//     }
//
//     public async Task StartStreamingAsync()
//     {
//       if (_obsConfig != null)
//       {
//         // OBS is configured, connect to it
//         await ConnectToObsAsync(_obsConfig);
//       }
//       else
//       {
//         // OBS is not configured, skip OBS integration
//         _logger.LogInformation("OBS not configured, skipping OBS integration");
//       }
//     }
//   }


// SECTION 10: COMMON PATTERNS
// ===========================
//
// Pattern 1: Required configuration for core features
//   public class CoreService
//   {
//     public CoreService(IOptions<CaramelAiConfigOptions> aiConfig)
//     {
//       ArgumentNullException.ThrowIfNull(aiConfig);
//       _config = aiConfig.Value;
//     }
//   }
//
// Pattern 2: Optional configuration for features
//   public class OptionalFeatureService
//   {
//     public OptionalFeatureService(IOptions<OBSConfigOptions>? obsConfig = null)
//     {
//       _obsConfig = obsConfig?.Value;
//     }
//   }
//
// Pattern 3: Configuration validation in factory
//   public class ServiceFactory
//   {
//     public IMyService CreateService(IServiceProvider provider)
//     {
//       var config = provider.GetRequiredService<IOptions<MyConfigOptions>>().Value;
//       var errors = config.Validate().ToList();
//       if (errors.Any())
//         throw ConfigurationValidationException.CreateFromErrors("MyConfig", errors);
//
//       return new MyService(config);
//     }
//   }

// NOTES:
// ======
// - Always use IOptions<T> for dependency injection, not raw properties
// - Configuration is immutable after application startup
// - Validation happens once on startup, not on every access
// - Use IOptionsMonitor<T> only if you need hot-reload capability
// - Secrets should be managed via Secret Manager or environment variables
// - Never commit appsettings with real secrets to version control
