# Configuration Validation Infrastructure - Quick Reference

## Quick Start (30 seconds)

### 1. Register in Program.cs
```csharp
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddCaramelOptions(builder.Configuration);  // That's it!
var app = builder.Build();
await app.RunAsync();
```

### 2. Inject in Your Service
```csharp
public class MyService
{
    private readonly CaramelAiConfigOptions _config;
    
    public MyService(IOptions<CaramelAiConfigOptions> options)
    {
        _config = options?.Value ?? throw new ArgumentNullException(nameof(options));
    }
    
    public void DoWork()
    {
        var url = _config.Endpoint;  // Strongly typed!
    }
}
```

### 3. Configure appsettings.json
```json
{
  "CaramelAiConfig": {
    "ModelId": "gpt-4",
    "Endpoint": "https://api.openai.com/v1",
    "ApiKey": "sk-..."
  }
}
```

---

## Configuration Classes at a Glance

| Class | Section | Required | Purpose |
|-------|---------|----------|---------|
| `CaramelAiConfigOptions` | `CaramelAiConfig` | ✓ | AI service settings |
| `DiscordConfigOptions` | `DiscordConfig` | ✓ | Discord bot token & key |
| `TwitchConfigOptions` | `TwitchConfig` | ✓ | Twitch OAuth credentials |
| `OBSConfigOptions` | `ObsConfig` | ✗ | OBS WebSocket (optional) |
| `GrpcConfigOptions` | `GrpcHostConfig` | ✓ | gRPC server settings |
| `DatabaseConfigOptions` | `ConnectionStrings` | ✓ | Database connections |

---

## Common Patterns

### Pattern 1: Required Configuration
```csharp
public class CoreService
{
    public CoreService(IOptions<CaramelAiConfigOptions> config)
    {
        var options = config?.Value ?? throw new ArgumentNullException(nameof(config));
        _endpoint = options.Endpoint;
    }
}
```

### Pattern 2: Optional Configuration
```csharp
public class OptionalFeatureService
{
    private readonly OBSConfigOptions? _obsConfig;
    
    public OptionalFeatureService(IOptions<OBSConfigOptions>? obsConfig = null)
    {
        _obsConfig = obsConfig?.Value;
    }
    
    public void DoWork()
    {
        if (_obsConfig != null)
        {
            // OBS is configured, use it
            ConnectToObs(_obsConfig);
        }
    }
}
```

### Pattern 3: Multiple Configurations
```csharp
public class IntegrationService
{
    public IntegrationService(
        IOptions<DiscordConfigOptions> discord,
        IOptions<TwitchConfigOptions> twitch,
        IOptions<GrpcConfigOptions> grpc)
    {
        _discord = discord.Value;
        _twitch = twitch.Value;
        _grpc = grpc.Value;
    }
}
```

### Pattern 4: Listening to Changes
```csharp
public class DynamicConfigService
{
    public DynamicConfigService(IOptionsMonitor<GrpcConfigOptions> grpcMonitor)
    {
        var current = grpcMonitor.CurrentValue;
        grpcMonitor.OnChange(newValue => 
        {
            Console.WriteLine("gRPC config changed!");
        });
    }
}
```

### Pattern 5: Testing
```csharp
[TestClass]
public class ConfigurationTests
{
    [TestMethod]
    public void ValidConfiguration_Passes()
    {
        var config = new CaramelAiConfigOptions
        {
            ModelId = "gpt-4",
            Endpoint = "https://api.openai.com/v1",
            ApiKey = "sk-1234567890123456789"
        };
        
        var errors = config.Validate().ToList();
        Assert.IsEmpty(errors);
    }
}
```

---

## Validation Examples

### Missing Required Field
```
Configuration validation failed for 'CaramelAiConfig':
  - ModelId is required
  - ApiKey is required
```

Fix: Add `"ModelId"` and `"ApiKey"` to appsettings.json under `CaramelAiConfig` section.

### Invalid URL
```
Configuration validation failed for 'CaramelAiConfig':
  - Endpoint must be a valid URL
```

Fix: Ensure Endpoint starts with `http://` or `https://`.

### Invalid Discord Token
```
Configuration validation failed for 'DiscordConfig':
  - Token appears to be invalid (should have 3 parts separated by dots)
```

Fix: Verify your Discord bot token from https://discord.com/developers/applications

### Missing Channel IDs
```
Configuration validation failed for 'TwitchConfig':
  - ChannelIds must contain at least one channel ID
```

Fix: Add comma-separated Twitch channel IDs, e.g., `"987654,555555"`.

---

## Secret Handling

### Safe Logging
```csharp
// This is safe! Secrets are masked in ToString()
_logger.LogInformation("Config: {Config}", _config);

// Output: 
// Config: CaramelAiConfig { ModelId = gpt-4, Endpoint = ..., ApiKey = *** }
```

### Never Do This
```csharp
// WRONG - don't access raw secret values for logging!
_logger.LogError("ApiKey: {Key}", _config.ApiKey);  // ❌ Bad!

// CORRECT - use ToString() instead
_logger.LogError("Config: {Config}", _config);       // ✓ Good!
```

---

## Environment-Specific Configuration

### Development (appsettings.Development.json)
```json
{
  "GrpcHostConfig": {
    "ValidateSslCertificate": false,
    "UseHttps": false
  },
  "ObsConfig": {
    "Url": "ws://localhost:4455"
  }
}
```

### Production (appsettings.Production.json)
```json
{
  "GrpcHostConfig": {
    "ValidateSslCertificate": true,
    "UseHttps": true
  }
}
```

### Staging (appsettings.Staging.json)
```json
{
  "CaramelAiConfig": {
    "Temperature": 0.5
  }
}
```

**How it works:** ASP.NET Core merges:
1. Base appsettings.json
2. + appsettings.{Environment}.json
3. + Environment variables

---

## Troubleshooting

### "Application won't start"
Check if `AddCaramelOptions()` is in Program.cs. Look for `ConfigurationValidationException` in error message.

### "Configuration validation failed"
Check the error message - it lists exactly which fields are missing or invalid.

### "Why is my secret masked?"
That's a feature! It prevents accidental exposure. Use `_config.ApiKey` to access the actual value.

### "How do I override configuration?"
1. Use environment variables: `CARAMEL_AI_MODELID=gpt-4-turbo`
2. Use appsettings.{Environment}.json for environment-specific values
3. Use User Secrets in development: `dotnet user-secrets set "CaramelAiConfig:ApiKey" "sk-..."`

### "Can I reload configuration without restarting?"
Use `IOptionsMonitor<T>` instead of `IOptions<T>`. It supports hot-reload.

---

## Property Quick Reference

### CaramelAiConfigOptions
```
ModelId              [Required] AI model name
Endpoint             [Required, Url] API endpoint
ApiKey               [Required] API authentication key (secret)
SystemPrompt         [Optional] System context for AI
MaxTokens            [Optional, Range 1-10000]
Temperature          [Optional, Range 0.0-2.0]
```

### DiscordConfigOptions
```
Token                [Required] Bot authentication token (secret)
PublicKey            [Required] Interaction verification key (secret)
BotName              [Optional] Display name
Intents              [Optional] Intent configuration
CommandPrefix        [Optional] Text command prefix
```

### TwitchConfigOptions
```
ClientId             [Required] Twitch app ID
ClientSecret         [Required] App secret (secret)
AccessToken          [Required] OAuth token (secret)
RefreshToken         [Required] Refresh token (secret)
EncryptionKey        [Required] Session encryption (secret)
OAuthCallbackUrl     [Required, Url] OAuth redirect URI
BotUserId            [Required] Bot account ID
ChannelIds           [Required] Comma-separated channel IDs
MessageTheAiRewardId [Optional] Custom reward GUID
```

### OBSConfigOptions
```
Url                  [Required, Url] WebSocket URL (ws://, wss://)
Password             [Required] WebSocket password (secret)
ConnectionTimeoutSeconds   [Range 1-120, default 10]
AutoReconnect        [bool, default true]
MaxReconnectAttempts [Range 1-100, default 5]
```

### GrpcConfigOptions
```
Host                 [Required] Server hostname/IP
Port                 [Required, Range 1-65535]
ApiToken             [Required] Authentication token (secret)
UseHttps             [bool, default true]
ValidateSslCertificate [bool, default true]
ConnectionTimeoutSeconds [Range 1-300, default 30]
```

### DatabaseConfigOptions
```
Caramel              [Required] Primary DB connection string
Quartz               [Required] Scheduler DB connection string
Redis                [Required] Cache connection string
```

---

## File Locations

```
/src/Caramel.Core/Configuration/
├── ConfigurationOptions.cs              Base class
├── ConfigurationValidationException.cs  Exception class
├── CaramelAiConfigOptions.cs           AI config
├── DiscordConfigOptions.cs             Discord config
├── TwitchConfigOptions.cs              Twitch config
├── OBSConfigOptions.cs                 OBS config
├── GrpcConfigOptions.cs                gRPC config
├── DatabaseConfigOptions.cs            Database config
├── ConfigurationServicesExtensions.cs  DI registration
├── ConfigurationExampleService.cs      Usage example
├── IMPLEMENTATION_SUMMARY.md           Full docs
└── USAGE_GUIDE.md                      Patterns & guide
```

---

## Links

- **Full Documentation**: See `IMPLEMENTATION_SUMMARY.md`
- **Usage Patterns**: See `USAGE_GUIDE.md`
- **Example Service**: See `ConfigurationExampleService.cs`
- **DI Registration**: See `ConfigurationServicesExtensions.cs`

---

## Key Takeaways

1. **One line to register**: `builder.Services.AddCaramelOptions(builder.Configuration);`
2. **Strongly typed injection**: `IOptions<CaramelAiConfigOptions>`
3. **Validation on startup**: Fails fast with clear error messages
4. **Safe for logging**: ToString() masks all secrets
5. **Optional sections**: OBS config is optional, others required
6. **Microsoft standard**: Uses IOptions<T> pattern from ASP.NET Core
