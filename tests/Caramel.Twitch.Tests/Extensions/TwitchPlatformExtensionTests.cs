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
    result.Username.Should().Be(username);
    result.PlatformUserId.Should().Be(userId);
    result.Platform.Should().Be(Platform.Twitch);
  }

  [Fact]
  public void GetTwitchPlatformIdSetsPlatformToTwitch()
  {
    // Arrange & Act
    var result = TwitchPlatformExtension.GetTwitchPlatformId("user", "123");

    // Assert
    result.Platform.Should().Be(Platform.Twitch);
    result.Platform.Should().NotBe(Platform.Discord);
    result.Platform.Should().NotBe(Platform.Web);
  }
}
