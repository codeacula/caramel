# Secrets Management Guide

This guide covers how to securely manage secrets across different environments and deployment scenarios for the Caramel system.

## Table of Contents

1. [Local Development Secrets](#local-development-secrets)
2. [Environment-Specific Secrets](#environment-specific-secrets)
3. [Docker Secrets](#docker-secrets)
4. [Kubernetes Secrets](#kubernetes-secrets)
5. [CI/CD Secrets Injection](#cicd-secrets-injection)
6. [Secrets Rotation](#secrets-rotation)
7. [Security Tools](#security-tools)
8. [Common Scenarios](#common-scenarios)

## Local Development Secrets

### User Secrets Overview

User Secrets is the recommended way to manage secrets during local development. It stores secrets outside the repository in your user profile directory.

**Advantages:**
- ✅ Secrets never committed to Git
- ✅ Simple CLI interface
- ✅ Built-in to .NET
- ✅ Per-project isolation
- ✅ Easy to switch between projects

**Limitations:**
- Only for local development (not for staging/production)
- Stored in plaintext in user home directory (rely on OS permissions)

### Setup User Secrets

#### For Each Project

```bash
# Navigate to project with secrets
cd src/Caramel.API

# Initialize user secrets (creates UserSecretsId in .csproj)
dotnet user-secrets init

# Verify it was added to .csproj
grep -A1 "UserSecretsId" Caramel.API.csproj
# Output should be something like:
# <PropertyGroup>
#   <UserSecretsId>abc123def456-ghi789</UserSecretsId>
```

#### Setting Secrets

```bash
# Database
dotnet user-secrets set "CARAMEL_DATABASE_CONNECTIONSTRING" "Host=localhost;Database=caramel_db;Username=caramel;Password=caramel"
dotnet user-secrets set "CARAMEL_QUARTZ_CONNECTIONSTRING" "Host=localhost;Database=caramel_db;Username=caramel;Password=caramel"
dotnet user-secrets set "CARAMEL_REDIS_CONNECTIONSTRING" "localhost:6379,password=caramel_redis"

# Discord
dotnet user-secrets set "CARAMEL_DISCORD_TOKEN" "your-discord-bot-token"
dotnet user-secrets set "CARAMEL_DISCORD_PUBLICKEY" "your-discord-public-key"

# Twitch
dotnet user-secrets set "CARAMEL_TWITCH_CLIENTID" "your-twitch-client-id"
dotnet user-secrets set "CARAMEL_TWITCH_CLIENTSECRET" "your-twitch-client-secret"
dotnet user-secrets set "CARAMEL_TWITCH_OAUTHCALLBACKURL" "http://localhost:8080/auth/twitch/callback"
dotnet user-secrets set "CARAMEL_TWITCH_ENCRYPTIONKEY" "$(openssl rand -base64 32)"

# gRPC
dotnet user-secrets set "CARAMEL_GRPC_HOST" "localhost"
dotnet user-secrets set "CARAMEL_GRPC_PORT" "5270"
dotnet user-secrets set "CARAMEL_GRPC_USEHTTPS" "false"
dotnet user-secrets set "CARAMEL_GRPC_APITOKEN" "dev-api-token"
dotnet user-secrets set "CARAMEL_GRPC_VALIDATESSLCERTIFICATE" "false"

# AI
dotnet user-secrets set "CARAMEL_AI_MODELID" "gpt-4"
dotnet user-secrets set "CARAMEL_AI_ENDPOINT" "https://api.openai.com/v1"
dotnet user-secrets set "CARAMEL_AI_APIKEY" "your-api-key"

# OBS
dotnet user-secrets set "CARAMEL_OBS_URL" "ws://localhost:4455"
dotnet user-secrets set "CARAMEL_OBS_PASSWORD" "your-obs-password"
```

#### Viewing and Managing Secrets

```bash
# List all secrets (values hidden)
dotnet user-secrets list

# List all secrets with values
dotnet user-secrets list --show-values

# Remove a secret
dotnet user-secrets remove "CARAMEL_DISCORD_TOKEN"

# Clear all secrets for this project
dotnet user-secrets clear
```

### Local Environment Variables

For quick testing or CI/CD pipelines, you can also use environment variables:

```bash
# Linux/Mac
export CARAMEL_DISCORD_TOKEN="your-token"
dotnet run --project src/Caramel.API

# Windows
set CARAMEL_DISCORD_TOKEN=your-token
dotnet run --project src/Caramel.API

# Or inline
CARAMEL_DISCORD_TOKEN="token" dotnet run
```

### .env Files (Development Only)

**WARNING:** Only for local development, use with caution.

If you must use a `.env` file locally:

```bash
# Create a .env file (never commit this)
cp .env.example .env

# Edit .env with your actual secrets
nano .env

# Ensure it's in .gitignore (it should be)
grep "^.env$" .gitignore

# Load environment variables (various tools):
# dotnet user-secrets is preferred instead
```

## Environment-Specific Secrets

### Development

**Location:** User Secrets or environment variables  
**Security:** Relies on OS user isolation  
**Management:** Manual via `dotnet user-secrets`

```bash
# Set up once per machine
cd src/Caramel.API
dotnet user-secrets init
dotnet user-secrets set "CARAMEL_DISCORD_TOKEN" "dev-token"
```

### Staging

**Location:** Environment variables in staging deployment system  
**Security:** Stored securely in deployment platform  
**Management:** Platform-specific (see sections below)

**appsettings.Staging.json** (committed, no secrets):
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information"
    }
  },
  "GrpcHostConfig": {
    "Host": "caramel-service-staging",
    "Port": 5270,
    "ValidateSslCertificate": true,
    "UseHttps": true
  }
}
```

### Production

**Location:** Enterprise secrets management system  
**Security:** Encrypted at rest, access controlled  
**Management:** Restricted access, audit logging

**appsettings.Production.json** (committed, no secrets):
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Warning",
      "Microsoft.AspNetCore": "Error"
    }
  },
  "GrpcHostConfig": {
    "Host": "caramel-service",
    "Port": 5270,
    "ValidateSslCertificate": true,
    "UseHttps": true
  }
}
```

## Docker Secrets

### Using Environment Variables

```bash
# Run with environment variables
docker run -d \
  --name caramel-api \
  -e ASPNETCORE_ENVIRONMENT=Staging \
  -e CARAMEL_DISCORD_TOKEN="your-token" \
  -e CARAMEL_DATABASE_CONNECTIONSTRING="Host=postgres;Password=secret" \
  -e CARAMEL_REDIS_CONNECTIONSTRING="redis:6379,password=secret" \
  caramel:latest
```

### Using Docker Compose with Environment Variables

**docker-compose.prod.yaml:**
```yaml
version: '3.8'

services:
  caramel-api:
    image: caramel:latest
    environment:
      ASPNETCORE_ENVIRONMENT: Production
      CARAMEL_DISCORD_TOKEN: ${CARAMEL_DISCORD_TOKEN}
      CARAMEL_DATABASE_CONNECTIONSTRING: ${CARAMEL_DATABASE_CONNECTIONSTRING}
      CARAMEL_REDIS_CONNECTIONSTRING: ${CARAMEL_REDIS_CONNECTIONSTRING}
      CARAMEL_TWITCH_CLIENTID: ${CARAMEL_TWITCH_CLIENTID}
      CARAMEL_TWITCH_CLIENTSECRET: ${CARAMEL_TWITCH_CLIENTSECRET}
    ports:
      - "5144:5144"
    depends_on:
      - postgres
      - redis

  postgres:
    image: postgres:16
    environment:
      POSTGRES_PASSWORD: ${POSTGRES_PASSWORD}
      POSTGRES_DB: caramel_db

  redis:
    image: redis:7-alpine
    command: redis-server --requirepass ${REDIS_PASSWORD}
```

**Create .env file with secrets:**
```bash
# Never commit this file
CARAMEL_DISCORD_TOKEN=your-token
CARAMEL_DATABASE_CONNECTIONSTRING=Host=postgres;Password=secret
# ... etc
```

**Run with environment file:**
```bash
docker-compose -f docker-compose.prod.yaml --env-file .env up -d
```

### Using Docker Secrets (Swarm Mode)

For Docker Swarm, use native secret management:

```bash
# Create secrets
echo "your-discord-token" | docker secret create caramel_discord_token -
echo "your-twitch-secret" | docker secret create caramel_twitch_secret -

# Reference in docker-compose
version: '3.8'

services:
  caramel-api:
    image: caramel:latest
    environment:
      CARAMEL_DISCORD_TOKEN_FILE: /run/secrets/caramel_discord_token
      CARAMEL_TWITCH_CLIENTSECRET_FILE: /run/secrets/caramel_twitch_secret
    secrets:
      - caramel_discord_token
      - caramel_twitch_secret

secrets:
  caramel_discord_token:
    external: true
  caramel_twitch_secret:
    external: true
```

**Note:** Application needs to read `*_FILE` variables to load from secret files.

## Kubernetes Secrets

### Create Secrets

**Method 1: Using kubectl imperatively**

```bash
kubectl create secret generic caramel-secrets \
  --from-literal=CARAMEL_DISCORD_TOKEN='your-token' \
  --from-literal=CARAMEL_TWITCH_CLIENTID='your-id' \
  --from-literal=CARAMEL_TWITCH_CLIENTSECRET='your-secret' \
  --from-literal=CARAMEL_DATABASE_CONNECTIONSTRING='Host=postgres;Password=...' \
  --from-literal=CARAMEL_REDIS_CONNECTIONSTRING='redis:6379,password=...' \
  --from-literal=CARAMEL_AI_APIKEY='your-key'
```

**Method 2: Using manifest (for GitOps)**

**caramel-secrets.yaml:**
```yaml
apiVersion: v1
kind: Secret
metadata:
  name: caramel-secrets
  namespace: default
type: Opaque
stringData:
  CARAMEL_DISCORD_TOKEN: your-token
  CARAMEL_TWITCH_CLIENTID: your-id
  CARAMEL_TWITCH_CLIENTSECRET: your-secret
  CARAMEL_DATABASE_CONNECTIONSTRING: Host=postgres;Password=...
  CARAMEL_REDIS_CONNECTIONSTRING: redis:6379,password=...
  CARAMEL_AI_APIKEY: your-key
```

**Apply with encryption:**
```bash
# Ensure etcd encryption is configured in your cluster
kubectl apply -f caramel-secrets.yaml

# Verify (values are base64 encoded in storage)
kubectl get secret caramel-secrets -o yaml
```

### Create ConfigMap for Non-Secret Settings

**caramel-config.yaml:**
```yaml
apiVersion: v1
kind: ConfigMap
metadata:
  name: caramel-config
  namespace: default
data:
  ASPNETCORE_ENVIRONMENT: "Production"
  CARAMEL_GRPC_HOST: "caramel-service"
  CARAMEL_GRPC_PORT: "5270"
  CARAMEL_GRPC_USEHTTPS: "true"
  CARAMEL_OBS_URL: "ws://obs-service:4455"
  LOG_LEVEL: "Information"
```

### Reference in Deployment

```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: caramel-api
  namespace: default
spec:
  replicas: 3
  selector:
    matchLabels:
      app: caramel-api
  template:
    metadata:
      labels:
        app: caramel-api
    spec:
      containers:
      - name: caramel-api
        image: caramel:latest
        ports:
        - containerPort: 5144
        
        # Load ConfigMap as environment variables
        envFrom:
        - configMapRef:
            name: caramel-config
        
        # Load Secret as environment variables
        - secretRef:
            name: caramel-secrets
        
        # Or mount as files (more secure for password hashing)
        volumeMounts:
        - name: secrets
          mountPath: /etc/secrets
          readOnly: true
      
      volumes:
      - name: secrets
        secret:
          secretName: caramel-secrets
```

### Using Sealed Secrets (GitOps)

For better security in GitOps workflows, use sealed-secrets:

```bash
# Install sealed-secrets controller
kubectl apply -f https://github.com/bitnami-labs/sealed-secrets/releases/download/v0.24.0/controller.yaml

# Seal a secret
echo -n 'your-token' | kubectl create secret generic my-secret \
  --dry-run=client \
  --from-file=password=/dev/stdin \
  -o yaml | kubeseal -f - > my-sealed-secret.yaml

# Push sealed secret to Git (safe)
git add my-sealed-secret.yaml
git commit -m "Add sealed secret"

# In cluster, sealed-secrets controller decrypts automatically
kubectl apply -f my-sealed-secret.yaml
```

## CI/CD Secrets Injection

### GitHub Actions

**Store secrets in GitHub repository settings (Settings → Secrets and Variables → Actions)**

**.github/workflows/deploy.yaml:**
```yaml
name: Deploy to Production

on:
  push:
    branches:
      - main

jobs:
  deploy:
    runs-on: ubuntu-latest
    steps:
    - uses: actions/checkout@v4
    
    - name: Build Docker image
      run: docker build -t caramel:${{ github.sha }} .
    
    - name: Push to registry
      run: docker push caramel:${{ github.sha }}
    
    - name: Deploy to Kubernetes
      run: |
        kubectl set image deployment/caramel-api \
          caramel-api=caramel:${{ github.sha }}
      env:
        # Secrets are injected as environment variables
        DISCORD_TOKEN: ${{ secrets.DISCORD_TOKEN }}
        TWITCH_CLIENT_ID: ${{ secrets.TWITCH_CLIENT_ID }}
        TWITCH_CLIENT_SECRET: ${{ secrets.TWITCH_CLIENT_SECRET }}
    
    - name: Update Kubernetes secret
      run: |
        kubectl create secret generic caramel-secrets \
          --from-literal=CARAMEL_DISCORD_TOKEN='${{ secrets.DISCORD_TOKEN }}' \
          --from-literal=CARAMEL_TWITCH_CLIENTID='${{ secrets.TWITCH_CLIENT_ID }}' \
          --from-literal=CARAMEL_TWITCH_CLIENTSECRET='${{ secrets.TWITCH_CLIENT_SECRET }}' \
          --dry-run=client -o yaml | kubectl apply -f -
```

### GitLab CI/CD

**Store secrets in CI/CD Settings (Settings → CI/CD → Variables)**

**.gitlab-ci.yml:**
```yaml
stages:
  - build
  - deploy

build:
  stage: build
  script:
    - docker build -t caramel:$CI_COMMIT_SHA .
    - docker push caramel:$CI_COMMIT_SHA

deploy:
  stage: deploy
  script:
    - kubectl create secret generic caramel-secrets --dry-run=client -o yaml
      --from-literal=CARAMEL_DISCORD_TOKEN="$DISCORD_TOKEN"
      --from-literal=CARAMEL_TWITCH_CLIENTID="$TWITCH_CLIENT_ID"
      --from-literal=CARAMEL_TWITCH_CLIENTSECRET="$TWITCH_CLIENT_SECRET" |
      kubectl apply -f -
    - kubectl set image deployment/caramel-api caramel-api=caramel:$CI_COMMIT_SHA
  only:
    - main
```

### Azure DevOps

**Store secrets in Variable Groups**

**azure-pipelines.yaml:**
```yaml
trigger:
  - main

stages:
- stage: Build
  jobs:
  - job: BuildDockerImage
    pool:
      vmImage: 'ubuntu-latest'
    steps:
    - task: Docker@2
      inputs:
        command: build
        Dockerfile: Dockerfile
        tags: caramel:$(Build.BuildId)

- stage: Deploy
  dependsOn: Build
  jobs:
  - job: DeployToKubernetes
    pool:
      vmImage: 'ubuntu-latest'
    variables:
    - group: caramel-production-secrets  # Reference variable group
    steps:
    - task: KubernetesManifest@0
      inputs:
        action: 'createSecret'
        kubernetesServiceConnection: 'kubernetes-prod'
        secretType: 'generic'
        secretName: 'caramel-secrets'
        secretArguments: |
          --from-literal=CARAMEL_DISCORD_TOKEN='$(DiscordToken)'
          --from-literal=CARAMEL_TWITCH_CLIENTID='$(TwitchClientId)'
          --from-literal=CARAMEL_TWITCH_CLIENTSECRET='$(TwitchClientSecret)'
```

## Secrets Rotation

### Rotating Discord Token

```bash
# 1. Generate new token in Discord Developer Portal
# 2. Update in production secrets manager
kubectl patch secret caramel-secrets -p '{"data":{"CARAMEL_DISCORD_TOKEN":"'$(echo -n 'new-token' | base64 -w0)'"}}'

# 3. Restart pods to pick up new secret
kubectl rollout restart deployment/caramel-api

# 4. Verify in logs
kubectl logs -f deployment/caramel-api
```

### Rotating Database Password

```bash
# 1. Change password in database
# ALTER USER caramel WITH PASSWORD 'new-password';

# 2. Update connection string secret
kubectl create secret generic caramel-secrets \
  --from-literal=CARAMEL_DATABASE_CONNECTIONSTRING='Host=postgres;Password=new-password' \
  --dry-run=client -o yaml | kubectl apply -f -

# 3. Restart applications to reconnect
kubectl rollout restart deployment/caramel-api
kubectl rollout restart deployment/caramel-service
```

### Rotation Schedule

- **API Keys (external services):** Every 90 days
- **Database passwords:** Every 180 days
- **Encryption keys:** As needed, with rolling strategy
- **Bot tokens:** Immediately if compromised, quarterly otherwise
- **SSL/TLS certificates:** Automatically (Let's Encrypt every 60 days)

## Security Tools

### Pre-Commit Hooks

Prevent secrets from being committed:

**Install git-secrets:**
```bash
# macOS
brew install git-secrets

# Linux
git clone https://github.com/awslabs/git-secrets.git
cd git-secrets && make install

# Windows (via Git Bash)
git clone https://github.com/awslabs/git-secrets.git
cd git-secrets && ./install.ps1
```

**Configure for repository:**
```bash
cd /path/to/caramel
git secrets --install
git secrets --register-aws  # Scan for AWS keys
git secrets --add '.env'    # Scan for .env files
git secrets --add '.*\.local\.json'
```

**Test it works:**
```bash
# This should fail (blocked by pre-commit hook)
echo "CARAMEL_DISCORD_TOKEN=abc123" >> .env
git add .env
git commit -m "test"  # Should fail with secret detection
```

### GitHub Secret Scanning

GitHub automatically scans repositories for known secret patterns.

**Enable:**
1. Go to repository Settings
2. Code security → Secret scanning
3. Enable "Push protection"

**Review found secrets:**
- Go to Security tab → Secret scanning
- Review and revoke any exposed secrets

### TruffleHog Scanning

Comprehensive secret scanning tool:

```bash
# Scan current directory
trufflehog filesystem .

# Scan Git history
trufflehog git https://github.com/codeacula/caramel.git

# Output in JSON format
trufflehog filesystem . --json
```

## Common Scenarios

### Scenario 1: New Developer Onboarding

```bash
# 1. Clone repository
git clone https://github.com/codeacula/caramel.git
cd caramel

# 2. Start Docker services
docker-compose -f compose.dev.yaml up -d

# 3. For each project needing secrets
for project in API Service Discord Twitch; do
  cd src/Caramel.${project}
  dotnet user-secrets init
  
  # Ask team lead for secrets or retrieve from shared vault
  # Then set them
  dotnet user-secrets set "CARAMEL_DISCORD_TOKEN" "..."
  # ... set other secrets ...
  cd ../..
done

# 4. Run project
dotnet run --project src/Caramel.API
```

### Scenario 2: Promoting Secrets from Staging to Production

```bash
# 1. Export staging secrets
kubectl get secret caramel-secrets-staging -o jsonpath='{.data}' > staging-secrets.json

# 2. Review and update as needed
# - Some values might be different (endpoints, etc.)
# - Some values should be rotated

# 3. Create production secret
kubectl create secret generic caramel-secrets \
  --from-literal=CARAMEL_DISCORD_TOKEN='prod-token' \
  --from-literal=CARAMEL_DATABASE_CONNECTIONSTRING='prod-connection-string' \
  # ... other secrets ...

# 4. Update deployment
kubectl set selector service/caramel-api environment=production
```

### Scenario 3: Emergency Credential Rotation

```bash
# 1. Create new secret with updated values
NEW_SECRET=$(cat << EOF
CARAMEL_DISCORD_TOKEN=new-token
CARAMEL_TWITCH_CLIENTSECRET=new-secret
EOF
)

# 2. Apply immediately
kubectl create secret generic caramel-secrets \
  --from-literal="$NEW_SECRET" \
  --dry-run=client -o yaml | kubectl apply -f -

# 3. Force restart all pods
kubectl rollout restart deployment/caramel-api
kubectl rollout restart deployment/caramel-service

# 4. Verify new pods are running
kubectl get pods -w
```

---

For configuration details, see [CONFIGURATION.md](./CONFIGURATION.md).
