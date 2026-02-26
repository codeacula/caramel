namespace Caramel.Twitch.Tests.Controllers;

public sealed class AuthControllerScopeTests
{
  [Fact]
  public void ScopesContainsChannelEditCommercial()
  {
    // Arrange
    var scopesField = typeof(AuthController)
        .GetField("_scopes", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

    // Act
    var scopes = scopesField?.GetValue(null) as string;

    // Assert
    _ = scopes.Should().NotBeNull();
    _ = scopes.Should().Contain("channel:edit:commercial");
  }
}
