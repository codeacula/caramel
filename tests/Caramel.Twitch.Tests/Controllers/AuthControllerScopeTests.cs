namespace Caramel.Twitch.Tests.Controllers;

public sealed class AuthControllerScopeTests
{
  [Fact]
  public void BotScopesContainsCorrectScopes()
  {
    // Arrange
    var scopesField = typeof(AuthController)
        .GetField("BotScopes", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

    // Act
    var scopes = scopesField?.GetValue(null) as string;

    // Assert
    _ = scopes.Should().NotBeNull();
    _ = scopes.Should().Contain("user:bot");
    _ = scopes.Should().Contain("user:read:chat");
    _ = scopes.Should().Contain("user:write:chat");
    _ = scopes.Should().Contain("user:manage:whispers");
    _ = scopes.Should().Contain("chat:read");
    _ = scopes.Should().Contain("chat:edit");
    _ = scopes.Should().Contain("whispers:read");
    _ = scopes.Should().Contain("whispers:edit");
  }

  [Fact]
  public void BroadcasterScopesContainsCorrectScopes()
  {
    // Arrange
    var scopesField = typeof(AuthController)
        .GetField("BroadcasterScopes", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

    // Act
    var scopes = scopesField?.GetValue(null) as string;

    // Assert
    _ = scopes.Should().NotBeNull();
    _ = scopes.Should().Contain("channel:read:redemptions");
    _ = scopes.Should().Contain("channel:edit:commercial");
    _ = scopes.Should().Contain("moderator:manage:banned_users");
    _ = scopes.Should().Contain("moderator:manage:chat_messages");
    _ = scopes.Should().Contain("channel:moderate");
  }
}
