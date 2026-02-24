namespace Caramel.Twitch.Tests.Auth;

public sealed class OAuthStateManagerTests
{
  private readonly TwitchConfig _mockConfig = new()
  {
    ClientId = "test-client-id",
    ClientSecret = "test-client-secret",
    AccessToken = "test-token",
    OAuthCallbackUrl = "http://localhost:5146/auth/callback",
    EncryptionKey = Convert.ToBase64String(new byte[32]),
  };

  [Fact]
  public void GenerateStateReturnsNonEmptyString()
  {
    var manager = new OAuthStateManager();
    var state = manager.GenerateState();
    _ = state.Should().NotBeNullOrEmpty();
  }

  [Fact]
  public void GenerateStateReturnsDifferentValuesEachTime()
  {
    var manager = new OAuthStateManager();
    var state1 = manager.GenerateState();
    var state2 = manager.GenerateState();
    _ = state1.Should().NotBe(state2);
  }

  [Fact]
  public void ValidateAndConsumeStateReturnsTrueForValidState()
  {
    var manager = new OAuthStateManager();
    var state = manager.GenerateState();
    var result = manager.ValidateAndConsumeState(state);
    _ = result.Should().BeTrue();
  }

  [Fact]
  public void ValidateAndConsumeStateReturnsFalseForInvalidState()
  {
    var manager = new OAuthStateManager();
    var result = manager.ValidateAndConsumeState("invalid-state-123");
    _ = result.Should().BeFalse();
  }

  [Fact]
  public void ValidateAndConsumeStateReturnsFalseWhenStateAlreadyConsumed()
  {
    var manager = new OAuthStateManager();
    var state = manager.GenerateState();

    _ = manager.ValidateAndConsumeState(state).Should().BeTrue();
    _ = manager.ValidateAndConsumeState(state).Should().BeFalse();
  }

  [Fact]
  public void ValidateAndConsumeStateReturnsFalseForEmptyState()
  {
    var manager = new OAuthStateManager();
    var result = manager.ValidateAndConsumeState(string.Empty);
    _ = result.Should().BeFalse();
  }

  [Fact]
  public void MultipleStatesCanCoexist()
  {
    var manager = new OAuthStateManager();
    var state1 = manager.GenerateState();
    var state2 = manager.GenerateState();
    var state3 = manager.GenerateState();

    _ = manager.ValidateAndConsumeState(state1).Should().BeTrue();
    _ = manager.ValidateAndConsumeState(state3).Should().BeTrue();
    _ = manager.ValidateAndConsumeState(state2).Should().BeTrue();
  }
}
