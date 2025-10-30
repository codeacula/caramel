using Caramel.Twitch;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// Configure Twitch client credentials
builder.Services.Configure<CaramelTwitchClientCredentials>(
    builder.Configuration.GetSection("TwitchClient"));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

try
{
    app.Logger.LogInformation("Starting Caramel API...");
    var twitchCredentials = app.Services.GetRequiredService<Microsoft.Extensions.Options.IOptions<CaramelTwitchClientCredentials>>().Value;
    var twitchClient = new CaramelTwitchClient(twitchCredentials);
    await twitchClient.ConnectAsync();
}
catch (Exception ex)
{
    app.Logger.LogCritical(ex, "Application start-up failed");
    throw;
}

app.Run();
