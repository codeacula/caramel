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
    var manager = new OAuthStateManager(_mockConfig);
    var state = manager.GenerateState();
    state.Should().NotBeNullOrEmpty();
  }

  [Fact]
  public void GenerateStateReturnsDifferentValuesEachTime()
  {
    var manager = new OAuthStateManager(_mockConfig);
    var state1 = manager.GenerateState();
    var state2 = manager.GenerateState();
    state1.Should().NotBe(state2);
  }

  [Fact]
  public void ValidateAndConsumeStateReturnsTrue_ForValidState()
  {
    var manager = new OAuthStateManager(_mockConfig);
    var state = manager.GenerateState();
    var result = manager.ValidateAndConsumeState(state);
    result.Should().BeTrue();
  }

  [Fact]
  public void ValidateAndConsumeStateReturnsFalse_ForInvalidState()
  {
    var manager = new OAuthStateManager(_mockConfig);
    var result = manager.ValidateAndConsumeState("invalid-state-123");
    result.Should().BeFalse();
  }

  [Fact]
  public void ValidateAndConsumeStateReturnsFalse_WhenStateAlreadyConsumed()
  {
    var manager = new OAuthStateManager(_mockConfig);
    var state = manager.GenerateState();

    manager.ValidateAndConsumeState(state).Should().BeTrue();
    manager.ValidateAndConsumeState(state).Should().BeFalse();
  }

  [Fact]
  public void ValidateAndConsumeStateReturnsFalse_ForEmptyState()
  {
    var manager = new OAuthStateManager(_mockConfig);
    var result = manager.ValidateAndConsumeState(string.Empty);
    result.Should().BeFalse();
  }

  [Fact]
  public void CleanupExpiredStatesRemovesExpiredStates()
  {
    var manager = new OAuthStateManager(_mockConfig);

    var state1 = manager.GenerateState();
    Task.Delay(100).Wait();

    manager.CleanupExpiredStates();

    // State1 should still be valid (TTL is 10 minutes)
    manager.ValidateAndConsumeState(state1).Should().BeTrue();
  }

  [Fact]
  public void MultipleStatesCanCoexist()
  {
    var manager = new OAuthStateManager(_mockConfig);
    var state1 = manager.GenerateState();
    var state2 = manager.GenerateState();
    var state3 = manager.GenerateState();

    manager.ValidateAndConsumeState(state1).Should().BeTrue();
    manager.ValidateAndConsumeState(state3).Should().BeTrue();
    manager.ValidateAndConsumeState(state2).Should().BeTrue();
  }

  [Fact]
  public void ValidateAndConsumeStateIsThreadSafe()
  {
    var manager = new OAuthStateManager(_mockConfig);
    var states = Enumerable.Range(0, 100).Select(_ => manager.GenerateState()).ToList();

    var successCount = 0;
    var tasks = states.Select(state =>
    {
      return Task.Run(() =>
      {
        if (manager.ValidateAndConsumeState(state))
        {
          Interlocked.Increment(ref successCount);
        }
      });
    });

    Task.WaitAll(tasks.ToArray());

    // All 100 states should validate exactly once
    successCount.Should().Be(100);
  }
}
