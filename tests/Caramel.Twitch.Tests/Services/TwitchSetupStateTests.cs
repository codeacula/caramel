using Caramel.Domain.Twitch;
using Caramel.Twitch.Services;

namespace Caramel.Twitch.Tests.Services;

public sealed class TwitchSetupStateTests
{
  private static TwitchSetup MakeSetup(string botLogin = "caramel_bot") =>
    new()
    {
      BotUserId = "111",
      BotLogin = botLogin,
      Channels = [new TwitchChannel { UserId = "999", Login = "streamer" }],
      ConfiguredOn = DateTimeOffset.UtcNow,
      UpdatedOn = DateTimeOffset.UtcNow,
    };

  [Fact]
  public void IsConfigured_ReturnsFalse_Initially()
  {
    var state = new TwitchSetupState();
    state.IsConfigured.Should().BeFalse();
  }

  [Fact]
  public void Current_ReturnsNull_Initially()
  {
    var state = new TwitchSetupState();
    state.Current.Should().BeNull();
  }

  [Fact]
  public void IsConfigured_ReturnsTrue_AfterUpdate()
  {
    var state = new TwitchSetupState();
    state.Update(MakeSetup());
    state.IsConfigured.Should().BeTrue();
  }

  [Fact]
  public void Current_ReturnsSetup_AfterUpdate()
  {
    var setup = MakeSetup();
    var state = new TwitchSetupState();
    state.Update(setup);
    state.Current.Should().BeSameAs(setup);
  }

  [Fact]
  public void Update_ReplacesExistingSetup_WhenCalledTwice()
  {
    var first = MakeSetup("bot_v1");
    var second = MakeSetup("bot_v2");
    var state = new TwitchSetupState();

    state.Update(first);
    state.Update(second);

    state.Current.Should().BeSameAs(second);
    state.Current!.BotLogin.Should().Be("bot_v2");
  }
}
