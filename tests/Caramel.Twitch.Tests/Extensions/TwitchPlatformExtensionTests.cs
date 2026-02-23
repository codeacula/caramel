namespace Caramel.Twitch.Tests.Extensions;

public sealed class TwitchPlatformExtensionTests
{
  [Fact]
  public void GetTwitchPlatformIdWithValidDataReturnsCorrectPlatformId()
  {
    // Arrange
    const string username = "streamer123";
    const string userId = "user_id_456";

    // Act
    var result = TwitchPlatformExtension.GetTwitchPlatformId(username, userId);

    // Assert
    _ = result.Username.Should().Be(username);
    _ = result.PlatformUserId.Should().Be(userId);
    _ = result.Platform.Should().Be(Platform.Twitch);
  }

  [Fact]
  public void GetTwitchPlatformIdSetsPlatformToTwitch()
  {
    // Arrange & Act
    var result = TwitchPlatformExtension.GetTwitchPlatformId("user", "123");

    // Assert
    _ = result.Platform.Should().Be(Platform.Twitch);
    _ = result.Platform.Should().NotBe(Platform.Discord);
    _ = result.Platform.Should().NotBe(Platform.Web);
  }
}
