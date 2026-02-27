namespace Caramel.Twitch.Tests.Auth;

public sealed class DualOAuthStateManagerTests
{
  [Fact]
  public void GenerateStateForBotReturnsNonEmptyString()
  {
    var manager = new DualOAuthStateManager();
    var state = manager.GenerateState(TwitchAccountType.Bot);
    _ = state.Should().NotBeNullOrEmpty();
  }

  [Fact]
  public void GenerateStateForBroadcasterReturnsNonEmptyString()
  {
    var manager = new DualOAuthStateManager();
    var state = manager.GenerateState(TwitchAccountType.Broadcaster);
    _ = state.Should().NotBeNullOrEmpty();
  }

  [Fact]
  public void GenerateStateReturnsDifferentValuesEachTime()
  {
    var manager = new DualOAuthStateManager();
    var state1 = manager.GenerateState(TwitchAccountType.Bot);
    var state2 = manager.GenerateState(TwitchAccountType.Bot);
    _ = state1.Should().NotBe(state2);
  }

  [Fact]
  public void ValidateAndConsumeStateReturnsCorrectAccountTypeForBotState()
  {
    var manager = new DualOAuthStateManager();
    var state = manager.GenerateState(TwitchAccountType.Bot);
    var result = manager.ValidateAndConsumeState(state);
    _ = result.Should().Be(TwitchAccountType.Bot);
  }

  [Fact]
  public void ValidateAndConsumeStateReturnsCorrectAccountTypeForBroadcasterState()
  {
    var manager = new DualOAuthStateManager();
    var state = manager.GenerateState(TwitchAccountType.Broadcaster);
    var result = manager.ValidateAndConsumeState(state);
    _ = result.Should().Be(TwitchAccountType.Broadcaster);
  }

  [Fact]
  public void ValidateAndConsumeStateReturnsNullForInvalidState()
  {
    var manager = new DualOAuthStateManager();
    var result = manager.ValidateAndConsumeState("invalid-state-123");
    _ = result.Should().BeNull();
  }

  [Fact]
  public void ValidateAndConsumeStateReturnsNullWhenStateAlreadyConsumed()
  {
    var manager = new DualOAuthStateManager();
    var state = manager.GenerateState(TwitchAccountType.Bot);

    _ = manager.ValidateAndConsumeState(state).Should().Be(TwitchAccountType.Bot);
    _ = manager.ValidateAndConsumeState(state).Should().BeNull();
  }

  [Fact]
  public void ValidateAndConsumeStateReturnsNullForEmptyState()
  {
    var manager = new DualOAuthStateManager();
    var result = manager.ValidateAndConsumeState(string.Empty);
    _ = result.Should().BeNull();
  }

  [Fact]
  public void MultipleStatesCanCoexist()
  {
    var manager = new DualOAuthStateManager();
    var botState = manager.GenerateState(TwitchAccountType.Bot);
    var broadcasterState = manager.GenerateState(TwitchAccountType.Broadcaster);
    var anotherBotState = manager.GenerateState(TwitchAccountType.Bot);

    _ = manager.ValidateAndConsumeState(botState).Should().Be(TwitchAccountType.Bot);
    _ = manager.ValidateAndConsumeState(broadcasterState).Should().Be(TwitchAccountType.Broadcaster);
    _ = manager.ValidateAndConsumeState(anotherBotState).Should().Be(TwitchAccountType.Bot);
  }

  [Fact]
  public void BotAndBroadcasterStatesAreIndependent()
  {
    var manager = new DualOAuthStateManager();
    var botState = manager.GenerateState(TwitchAccountType.Bot);
    var broadcasterState = manager.GenerateState(TwitchAccountType.Broadcaster);

    // Both should be valid initially
    _ = manager.ValidateAndConsumeState(botState).Should().Be(TwitchAccountType.Bot);
    _ = manager.ValidateAndConsumeState(broadcasterState).Should().Be(TwitchAccountType.Broadcaster);

    // Both should be consumed now
    _ = manager.ValidateAndConsumeState(botState).Should().BeNull();
    _ = manager.ValidateAndConsumeState(broadcasterState).Should().BeNull();
  }
}
