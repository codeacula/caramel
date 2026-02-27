# Development Setup Guide

Get up and running with Caramel in minutes.

## Prerequisites

- **.NET 10 SDK** - [Download](https://dotnet.microsoft.com/download/dotnet/10.0)
- **Docker & Docker Compose** - [Install](https://docs.docker.com/get-docker/)
- **Git** - [Install](https://git-scm.com/)
- *(Optional)* **VS Code** with C# Dev Kit extension for IDE development

## Quick Start

### 1. Clone and Navigate
```bash
git clone https://github.com/codeacula/caramel.git
cd caramel
```

### 2. Start Infrastructure
```bash
docker-compose -f compose.dev.yaml up -d
```

This starts:
- PostgreSQL (port 5432)
- Redis (port 6379)
- Any other required services

### 3. Configure Environment
```bash
cp .env.example .env
```

Edit `.env` with your Discord/Twitch tokens and local configuration:
```
DISCORD_TOKEN=your_token_here
TWITCH_CLIENT_ID=your_id_here
TWITCH_ACCESS_TOKEN=your_token_here
```

### 4. Build and Test
```bash
# Restore packages
dotnet restore

# Build solution
dotnet build

# Run all tests
dotnet test

# Run specific project tests
dotnet test --project src/Caramel.Application.Tests
```

### 5. Run Locally

**Option A: Run the service**
```bash
dotnet run --project src/Caramel.Service
```

**Option B: Run the API only**
```bash
dotnet run --project src/Caramel.API
```

**Option C: Run with debugging** (VS Code)
- Open the project root in VS Code
- Press `F5` or go to Run → Start Debugging
- Select ".NET" when prompted

## Development Workflow

### Adding a New Command Handler

1. Create a new file in `src/Caramel.Application/Conversations/Commands/`
2. Implement `ICommand<TResult>` interface
3. Create corresponding handler implementing `ICommandHandler<TCommand, TResult>`
4. Register in dependency injection (usually auto-discovered via MediatR)
5. Add tests in `src/Caramel.Application.Tests/`

Example:
```csharp
public record MyNewCommand(string Input) : ICommand<Result<string>>;

public class MyNewCommandHandler(ILogger<MyNewCommandHandler> logger) 
  : ICommandHandler<MyNewCommand, Result<string>>
{
  public async Task<Result<string>> Handle(MyNewCommand request, CancellationToken ct)
  {
    // Implementation
    return Result.Ok("Success");
  }
}
```

### Running Specific Tests

```bash
# Run tests in a specific file
dotnet test --filter "ClassName=EventSubLifecycleServiceTests"

# Run a specific test method
dotnet test --filter "Name~ExecuteAsyncWhenTokenAvailable"

# Run with verbose output
dotnet test -v detailed
```

### Database Migrations

```bash
# Create migration
dotnet ef migrations add MigrationName --project src/Caramel.Database

# Apply migrations
dotnet ef database update --project src/Caramel.Database
```

## Logging

All modules use `ILogger<T>` for logging. To adjust log levels:

**In appsettings.json:**
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Caramel.Service": "Debug",
      "Caramel.Application": "Debug"
    }
  }
}
```

**At runtime:** Set `ASPNETCORE_ENVIRONMENT=Development` for verbose logging.

## Debugging

### Setting Breakpoints
1. Place cursor on line in VS Code
2. Press `F9` to toggle breakpoint (red dot appears)
3. Press `F5` to run with debugger
4. Execution pauses at breakpoint

### Inspecting Variables
- Hover over variable to see value
- Right-click → "Add to Watch" to monitor across steps
- Use "Debug Console" to evaluate expressions

### Common Debug Scenarios

**Debugging a command handler:**
```csharp
// Add breakpoint in Handle() method
// Trigger via API: POST /api/conversations
// Step through with F10
```

**Debugging a failing test:**
```bash
# Run test in debug mode
dotnet test --filter "Name~YourTestName" -- RunConfiguration.TargetPlatform=x64
```

## Troubleshooting

### Port Already in Use
```bash
# Find process using port
lsof -i :5432  # PostgreSQL
lsof -i :6379  # Redis

# Kill process (Linux/Mac)
kill -9 <PID>

# Or restart Docker
docker-compose -f compose.dev.yaml restart
```

### Database Connection Issues
```bash
# Check database is running
docker-compose -f compose.dev.yaml ps

# View logs
docker-compose -f compose.dev.yaml logs postgres

# Reset database
docker-compose -f compose.dev.yaml down -v
docker-compose -f compose.dev.yaml up -d
```

### Build Failures
```bash
# Clean everything
dotnet clean
rm -rf bin obj

# Restore fresh
dotnet restore

# Rebuild
dotnet build
```

## Project Structure at a Glance

```
src/
├── Caramel.API/              # REST endpoints
├── Caramel.Service/          # Background service & gRPC host
├── Caramel.Application/      # Command/query handlers (CQRS)
├── Caramel.Domain/           # Business entities and rules
├── Caramel.Database/         # Data access layer
├── Caramel.Cache/            # Redis caching
├── Caramel.AI/               # LLM integration
├── Caramel.Notifications/    # Discord/Twitch messaging
├── Caramel.Discord/          # Discord bot
├── Caramel.Twitch/           # Twitch bot
└── ...

tests/
├── Caramel.Application.Tests/
├── Caramel.Database.Tests/
└── ...
```

## Further Help

- **See Architecture:** Read [ARCHITECTURE.md](../ARCHITECTURE.md)
- **Contributing:** See [CONTRIBUTING.md](../CONTRIBUTING.md)
- **Issues:** Check GitHub Issues or create a new one
- **.NET Docs:** https://docs.microsoft.com/dotnet/
