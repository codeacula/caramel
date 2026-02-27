# Configuration Guide

This document covers all configuration options for the Caramel system, including environment variables, secrets management, and deployment-specific settings.

## Table of Contents

1. [Configuration Overview](#configuration-overview)
2. [Environment Variables Reference](#environment-variables-reference)
3. [Development Setup](#development-setup)
4. [Staging/Production Deployment](#stagingproduction-deployment)
5. [Secrets Management Best Practices](#secrets-management-best-practices)
6. [Environment Variable Precedence](#environment-variable-precedence)
7. [Troubleshooting](#troubleshooting)

## Configuration Overview

Caramel uses a layered configuration system that allows different settings for different environments:

```
┌─────────────────────────────────────────────────────┐
│ Environment Variables (highest priority)             │
│ - Loaded from host environment                       │
│ - Used in Docker/Kubernetes deployments              │
└──────────────────────┬──────────────────────────────┘
                       ↓
┌─────────────────────────────────────────────────────┐
│ User Secrets (development only)                      │
│ - Located in ~/.microsoft/usersecrets/<project-id>   │
│ - Only available during development                  │
└──────────────────────┬──────────────────────────────┘
                       ↓
┌─────────────────────────────────────────────────────┐
│ Environment-Specific Config Files                    │
│ - appsettings.<Environment>.json                     │
│ - Staging, Production, Development                   │
└──────────────────────┬──────────────────────────────┘
                       ↓
┌─────────────────────────────────────────────────────┐
│ Base Configuration (lowest priority)                 │
│ - appsettings.json                                   │
│ - Shared defaults and structure                      │
└─────────────────────────────────────────────────────┘
```

### Configuration Files

- **appsettings.json** - Base configuration for all environments (shared structure)
- **appsettings.Development.json** - Development-specific overrides
- **appsettings.Staging.json** - Staging environment overrides (committed, NO SECRETS)
- **appsettings.Production.json** - Production overrides (committed, NO SECRETS)
- **.env.example** - Template for environment variables (reference only, no secrets)
- **User Secrets** - Local development secrets (never committed)

### Environment Detection

The application environment is determined by the `ASPNETCORE_ENVIRONMENT` variable:

```bash
# Development (default for local development)
ASPNETCORE_ENVIRONMENT=Development

# Staging
ASPNETCORE_ENVIRONMENT=Staging

# Production
ASPNETCORE_ENVIRONMENT=Production
```

## Environment Variables Reference

### Database Configuration

| Variable | Description | Example | Required |
|----------|-------------|---------|----------|
| `CARAMEL_DATABASE_CONNECTIONSTRING` | PostgreSQL connection string for Caramel data | `Host=localhost;Database=caramel_db;Username=caramel;Password=****` | ✅ |
| `CARAMEL_QUARTZ_CONNECTIONSTRING` | PostgreSQL connection string for Quartz scheduler | `Host=localhost;Database=caramel_db;Username=caramel;Password=****` | ✅ |
| `CARAMEL_REDIS_CONNECTIONSTRING` | Redis connection string for caching | `localhost:6379,password=****` | ✅ |
| `POSTGRES_USER` | PostgreSQL admin username | `caramel` | ✅ (Docker only) |
| `POSTGRES_PASSWORD` | PostgreSQL admin password | `secure-password` | ✅ (Docker only) |
| `POSTGRES_DB` | PostgreSQL database name | `caramel_db` | ✅ (Docker only) |
| `REDIS_PASSWORD` | Redis password | `secure-password` | ✅ (Docker only) |

### Discord Configuration

| Variable | Description | Example | Required | Notes |
|----------|-------------|---------|----------|-------|
| `CARAMEL_DISCORD_TOKEN` | Discord bot token | `MTk4NjIyNDgzNzE3Mzc0OTc2.C-LXXQ.x...` | ✅ | [Get from Developer Portal](https://discord.com/developers/applications) |
| `CARAMEL_DISCORD_PUBLICKEY` | Discord application public key | `740191cff2...` | ✅ | [Get from Developer Portal](https://discord.com/developers/applications) |

### Twitch Integration Configuration

| Variable | Description | Example | Required | Notes |
|----------|-------------|---------|----------|-------|
| `CARAMEL_TWITCH_CLIENTID` | Twitch application client ID | `abc123def456...` | ✅ | [Get from Dev Console](https://dev.twitch.tv/console/apps) |
| `CARAMEL_TWITCH_CLIENTSECRET` | Twitch application client secret | `xyz789abc123...` | ✅ | [Get from Dev Console](https://dev.twitch.tv/console/apps) |
| `CARAMEL_TWITCH_ACCESSTOKEN` | Twitch OAuth access token | `access-token-here` | ❌ | Generated via OAuth flow, can be empty on first run |
| `CARAMEL_TWITCH_REFRESHTOKEN` | Twitch OAuth refresh token | `refresh-token-here` | ❌ | Generated via OAuth flow, can be empty on first run |
| `CARAMEL_TWITCH_OAUTHCALLBACKURL` | OAuth callback URL | `http://localhost:8080/auth/twitch/callback` | ✅ | Must match registered callback in Twitch |
| `CARAMEL_TWITCH_ENCRYPTIONKEY` | Session encryption key | `use-secure-random-string` | ✅ | Generate with: `openssl rand -base64 32` |
| `CARAMEL_TWITCH_MESSAGETHEOAIREWARDID` | Channel point redeem GUID (optional) | `01abc02b-9234-56cd-ef01-23456789abc0` | ❌ | GUID of "Message The AI" reward |

### Twitch Notifications Configuration

| Variable | Description | Example | Required | Notes |
|----------|-------------|---------|----------|-------|
| `CARAMEL_TWITCH_NOTIF_ACCESSTOKEN` | Service access token for sending notifications | `access-token-here` | ❌ | Different from user token |
| `CARAMEL_TWITCH_NOTIF_BOTUSERID` | Bot's Twitch user ID | `123456789` | ❌ | Numeric user ID of bot account |

### gRPC Configuration

| Variable | Description | Example | Required | Notes |
|----------|-------------|---------|----------|-------|
| `CARAMEL_GRPC_HOST` | gRPC service host | `localhost` (dev) or `caramel-service` (Docker) | ✅ | Use service name in Docker |
| `CARAMEL_GRPC_PORT` | gRPC service port | `5270` | ✅ | Default is 5270 |
| `CARAMEL_GRPC_USEHTTPS` | Use HTTPS for gRPC | `false` (dev) or `true` (prod) | ✅ | Boolean value |
| `CARAMEL_GRPC_APITOKEN` | API token for gRPC authentication | `dev-api-token` or `prod-secure-token` | ✅ | Must match server token |
| `CARAMEL_GRPC_VALIDATESSLCERTIFICATE` | Validate SSL certificates | `false` (dev) or `true` (prod) | ✅ | Boolean value |

### AI/LLM Configuration

| Variable | Description | Example | Required | Notes |
|----------|-------------|---------|----------|-------|
| `CARAMEL_AI_MODELID` | AI model identifier | `gpt-4` or `claude-3-sonnet` | ✅ | Depends on provider |
| `CARAMEL_AI_ENDPOINT` | AI service endpoint | `https://api.openai.com/v1` | ✅ | Provider-specific |
| `CARAMEL_AI_APIKEY` | AI service API key | `sk-proj-...` | ✅ | Keep this secret |

### OBS Integration

| Variable | Description | Example | Required | Notes |
|----------|-------------|---------|----------|-------|
| `CARAMEL_OBS_URL` | OBS WebSocket URL | `ws://localhost:4455` (dev) or `ws://host.docker.internal:4455` (Docker) | ✅ | WebSocket protocol |
| `CARAMEL_OBS_PASSWORD` | OBS WebSocket password | `obs-websocket-password` | ❌ | Leave empty if OBS has no password |

### Application Configuration

| Variable | Description | Example | Required | Notes |
|----------|-------------|---------|----------|-------|
| `ASPNETCORE_ENVIRONMENT` | Application environment | `Development`, `Staging`, or `Production` | ✅ | Controls which config files are loaded |

## Development Setup

### Using User Secrets Locally

User Secrets is the recommended way to manage secrets during development. It stores secrets in a user profile directory instead of committing them to the repository.

#### Step 1: Initialize User Secrets (if not already done)

```bash
# Navigate to a project directory (e.g., Caramel.API)
cd src/Caramel.API

# Initialize user secrets for this project
dotnet user-secrets init
```

This creates a `.csproj` entry with a secret ID and stores secrets in:
- **Linux/Mac**: `~/.microsoft/usersecrets/<project-id>/secrets.json`
- **Windows**: `%APPDATA%\microsoft\UserSecrets\<project-id>\secrets.json`

#### Step 2: Set Secrets

```bash
# Set individual secrets
dotnet user-secrets set "CARAMEL_DISCORD_TOKEN" "your-discord-token"
dotnet user-secrets set "CARAMEL_TWITCH_CLIENTID" "your-twitch-client-id"
dotnet user-secrets set "CARAMEL_TWITCH_CLIENTSECRET" "your-twitch-client-secret"

# Set database connection strings
dotnet user-secrets set "CARAMEL_DATABASE_CONNECTIONSTRING" "Host=localhost;Database=caramel_db;Username=caramel;Password=caramel"
dotnet user-secrets set "CARAMEL_REDIS_CONNECTIONSTRING" "localhost:6379,password=caramel_redis"

# Set AI configuration
dotnet user-secrets set "CARAMEL_AI_MODELID" "gpt-4"
dotnet user-secrets set "CARAMEL_AI_ENDPOINT" "https://api.openai.com/v1"
dotnet user-secrets set "CARAMEL_AI_APIKEY" "your-api-key"
```

#### Step 3: Verify Secrets

```bash
# List all secrets (values are hidden)
dotnet user-secrets list

# View a specific secret
dotnet user-secrets list --show-values
```

#### Step 4: Clear Secrets (when needed)

```bash
# Remove a single secret
dotnet user-secrets remove "CARAMEL_DISCORD_TOKEN"

# Clear all secrets for the project
dotnet user-secrets clear
```

### Local Development Workflow

```bash
# 1. Clone repository
git clone https://github.com/codeacula/caramel.git
cd caramel

# 2. Start infrastructure
docker-compose -f compose.dev.yaml up -d

# 3. For each project that needs secrets (API, Service, Discord, Twitch):
cd src/Caramel.API
dotnet user-secrets init
dotnet user-secrets set "CARAMEL_DISCORD_TOKEN" "..."
# ... set other secrets ...

# 4. Set ASPNETCORE_ENVIRONMENT (Development is default)
export ASPNETCORE_ENVIRONMENT=Development

# 5. Run the project
dotnet run
```

## Staging/Production Deployment

### Environment-Specific Configuration Files

For Staging and Production, configuration is managed through:

1. **appsettings.Staging.json** / **appsettings.Production.json** - Non-secret settings (committed)
2. **Environment Variables** - Actual secrets (injected by deployment system)

### Docker Deployment

```bash
# Build image
docker build -t caramel:latest .

# Run with environment variables
docker run -d \
  --name caramel \
  -e ASPNETCORE_ENVIRONMENT=Staging \
  -e CARAMEL_DISCORD_TOKEN="your-token" \
  -e CARAMEL_DATABASE_CONNECTIONSTRING="Host=db;..." \
  -e CARAMEL_REDIS_CONNECTIONSTRING="redis:6379,password=..." \
  caramel:latest
```

### Kubernetes Deployment

Create a ConfigMap for non-secret settings:

```yaml
apiVersion: v1
kind: ConfigMap
metadata:
  name: caramel-config
data:
  ASPNETCORE_ENVIRONMENT: "Production"
  CARAMEL_GRPC_HOST: "caramel-service"
  CARAMEL_GRPC_PORT: "5270"
  CARAMEL_GRPC_USEHTTPS: "true"
  CARAMEL_OBS_URL: "ws://obs-service:4455"
```

Create a Secret for sensitive data:

```yaml
apiVersion: v1
kind: Secret
metadata:
  name: caramel-secrets
type: Opaque
stringData:
  CARAMEL_DISCORD_TOKEN: "your-token"
  CARAMEL_TWITCH_CLIENTID: "your-id"
  CARAMEL_TWITCH_CLIENTSECRET: "your-secret"
  CARAMEL_DATABASE_CONNECTIONSTRING: "Host=db-service;..."
  CARAMEL_REDIS_CONNECTIONSTRING: "redis-service:6379,password=..."
  CARAMEL_AI_APIKEY: "your-api-key"
```

Reference in Deployment:

```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: caramel-api
spec:
  template:
    spec:
      containers:
      - name: caramel-api
        envFrom:
        - configMapRef:
            name: caramel-config
        - secretRef:
            name: caramel-secrets
```

## Secrets Management Best Practices

### ✅ Do's

1. **Use environment variables in production** - Never embed secrets in config files
2. **Use user-secrets in development** - Store locally, never in repository
3. **Rotate secrets regularly** - Especially API keys and tokens
4. **Use strong, random values** - For encryption keys and API tokens:
   ```bash
   # Generate secure random string
   openssl rand -base64 32
   ```
5. **Restrict access** - Limit who can view/modify secrets
6. **Use vault solutions** - Consider HashiCorp Vault, AWS Secrets Manager, Azure Key Vault
7. **Audit secret access** - Log when and who accesses secrets
8. **Keep .env.example up-to-date** - Document required variables
9. **Use different secrets per environment** - Never reuse production secrets for staging

### ❌ Don'ts

1. **Never commit secrets to Git** - Even if in a private repo
2. **Never hardcode secrets** - In config files or source code
3. **Never share secrets via email or chat** - Use secure channels only
4. **Never log secrets** - Configure logging to exclude sensitive fields
5. **Never use default secrets** - Change all defaults immediately
6. **Never commit .env files** - Only .env.example should be committed
7. **Never expose secrets in error messages** - Sanitize before displaying
8. **Never use the same secret everywhere** - Each environment should be unique

### Handling Accidentally Committed Secrets

If a secret is accidentally committed:

```bash
# 1. Immediately revoke the secret (in the service/platform)

# 2. Remove from Git history using git-filter-repo
git clone --mirror https://github.com/yourrepo/caramel.git
cd caramel.git
git filter-repo --invert-regex --path-regex '^(?:.*/)?(secrets|keys|passwords)\..*'

# 3. Force push the cleaned history
git push --force
```

**Better:** Use a secrets scanning tool like:
- GitHub Secret Scanning (automatic)
- TruffleHog (local scanning)
- git-secrets (pre-commit hook)

## Environment Variable Precedence

Configuration is loaded in this order (later entries override earlier ones):

1. **appsettings.json** (base)
2. **appsettings.{ASPNETCORE_ENVIRONMENT}.json** (staging/production/development)
3. **appsettings.local.json** (development only, if exists)
4. **User Secrets** (development only if secrets are initialized)
5. **Environment Variables** (highest priority, overrides everything)

Example - setting the Discord token:

```bash
# If any of these exist, they override in this order:
# 1. In appsettings.json: "DiscordConfig": { "Token": "base-value" }
# 2. In appsettings.Development.json: "DiscordConfig": { "Token": "dev-value" }
# 3. Via user-secrets: dotnet user-secrets set "DiscordConfig:Token" "secret-value"
# 4. Via environment variable: CARAMEL_DISCORD_TOKEN=env-value

# The environment variable always wins
echo $CARAMEL_DISCORD_TOKEN  # This value is used
```

## Troubleshooting

### "Configuration key not found"

**Problem:** Application can't find a required configuration key.

**Solution:**
1. Check that the environment variable is set: `echo $VARIABLE_NAME`
2. Verify the variable name matches (case-sensitive on Linux/Mac)
3. Check appsettings files for the key
4. For user-secrets: `dotnet user-secrets list`

### "Invalid connection string"

**Problem:** Database connection fails with "Invalid Connection String".

**Solution:**
```bash
# Check the connection string format
echo $CARAMEL_DATABASE_CONNECTIONSTRING

# Test connection with psql
psql -h localhost -U caramel -d caramel_db

# If using Docker, check service name
docker-compose ps  # Should show postgres running
```

### "Secret not accessible in production"

**Problem:** Works locally but fails in deployed environment.

**Solution:**
1. Verify environment variable is set in deployment system
2. Check logs for missing configuration
3. Verify secret name matches (use underscores or colons correctly)
4. Check that application has permissions to read secrets

### "User-secrets not working"

**Problem:** Secrets set with `dotnet user-secrets` aren't being used.

**Solution:**
```bash
# Verify user-secrets are initialized
grep -A2 "UserSecretsId" src/Caramel.API/Caramel.API.csproj

# If not found, initialize
dotnet user-secrets init

# Verify secrets exist
dotnet user-secrets list

# Check file permissions on Linux/Mac
ls -la ~/.microsoft/usersecrets/*/secrets.json
```

### "Environment variables not overriding config files"

**Problem:** Environment variables set but not being used.

**Solution:**
1. Check variable name format - use `_` for nested keys:
   ```
   # For "ConnectionStrings:Caramel", use:
   CARAMEL_DATABASE_CONNECTIONSTRING
   # OR
   ConnectionStrings__Caramel
   ```
2. Restart application after setting environment variables
3. Verify `ASPNETCORE_ENVIRONMENT` is set correctly
4. Check application logs for configuration being loaded

---

For more information, see [SECRETS.md](./SECRETS.md) for detailed secrets management strategies.
