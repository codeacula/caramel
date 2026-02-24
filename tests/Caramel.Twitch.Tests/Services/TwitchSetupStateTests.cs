using Caramel.Domain.Twitch;
using Caramel.Twitch.Services;

namespace Caramel.Twitch.Tests.Services;

public sealed class TwitchSetupStateTests
{
  private static TwitchSetup MakeSetup(string botLogin = "caramel_bot")
  {
    return new()
    {
      BotUserId = "111",
      BotLogin = botLogin,
      Channels = [new TwitchChannel { UserId = "999", Login = "streamer" }],
      ConfiguredOn = DateTimeOffset.UtcNow,
      UpdatedOn = DateTimeOffset.UtcNow,
    };
  }

  [Fact]
  public void IsConfiguredReturnsFalseInitially()
  {
    var state = new TwitchSetupState();
    _ = state.IsConfigured.Should().BeFalse();
  }

  [Fact]
  public void CurrentReturnsNullInitially()
  {
    var state = new TwitchSetupState();
    _ = state.Current.Should().BeNull();
  }

  [Fact]
  public void IsConfiguredReturnsTrueAfterUpdate()
  {
    var state = new TwitchSetupState();
    state.Update(MakeSetup());
    _ = state.IsConfigured.Should().BeTrue();
  }

  [Fact]
  public void CurrentReturnsSetupAfterUpdate()
  {
    var setup = MakeSetup();
    var state = new TwitchSetupState();
    state.Update(setup);
    _ = state.Current.Should().BeSameAs(setup);
  }

  [Fact]
  public void UpdateReplacesExistingSetupWhenCalledTwice()
  {
    var first = MakeSetup("bot_v1");
    var second = MakeSetup("bot_v2");
    var state = new TwitchSetupState();

    state.Update(first);
    state.Update(second);

    _ = state.Current.Should().BeSameAs(second);
    _ = state.Current!.BotLogin.Should().Be("bot_v2");
  }
}
