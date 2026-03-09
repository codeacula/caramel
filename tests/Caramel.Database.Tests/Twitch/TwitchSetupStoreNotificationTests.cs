using Marten;

namespace Caramel.Database.Tests.Twitch;

/// <summary>
/// Simplified tests for notification behavior around Twitch setup persistence.
/// These tests focus on the store contract shape and notification semantics
/// without attempting to fake Marten query pipelines.
/// </summary>
public sealed class TwitchSetupStoreNotificationTests
{
  private readonly Mock<IDocumentSession> _session = new();
  private readonly Mock<ITokenEncryptionService> _encryptionService = new();
  private readonly Mock<ITwitchSetupChangedNotifier> _notifier = new();

  [Fact]
  public void ConstructorCanBeCreatedWithNotifierDependency()
  {
    var store = CreateStore();

    _ = store.Should().NotBeNull();
  }

  [Fact]
  public async Task SaveAsyncWhenSaveChangesThrowsDoesNotPublishNotificationAsync()
  {
    // Arrange
    var setup = CreateSetup();

    _ = _session
      .Setup(s => s.Query<DbTwitchSetup>())
      .Throws(new InvalidOperationException("db failure"));

    var store = CreateStore();

    // Act
    var result = await store.SaveAsync(setup, CancellationToken.None);

    // Assert
    _ = result.IsFailed.Should().BeTrue();
    _ = result.Errors.Should().ContainSingle(e => e.Message.Contains("db failure"));
    _notifier.Verify(
      n => n.PublishAsync(It.IsAny<TwitchSetup>(), It.IsAny<CancellationToken>()),
      Times.Never);
  }

  [Fact]
  public async Task SaveBotTokensAsyncWhenEncryptionThrowsDoesNotPublishNotificationAsync()
  {
    // Arrange
    var tokens = CreateBotTokens();

    _ = _encryptionService
      .Setup(e => e.Encrypt(tokens.AccessToken))
      .Throws(new InvalidOperationException("encryption failed"));

    var store = CreateStore();

    // Act
    var result = await store.SaveBotTokensAsync(tokens, CancellationToken.None);

    // Assert
    _ = result.IsFailed.Should().BeTrue();
    _ = result.Errors.Should().ContainSingle(e => e.Message.Contains("encryption failed"));
    _notifier.Verify(
      n => n.PublishAsync(It.IsAny<TwitchSetup>(), It.IsAny<CancellationToken>()),
      Times.Never);
  }

  [Fact]
  public async Task SaveBroadcasterTokensAsyncWhenEncryptionThrowsDoesNotPublishNotificationAsync()
  {
    // Arrange
    var tokens = CreateBroadcasterTokens();

    _ = _encryptionService
      .Setup(e => e.Encrypt(tokens.AccessToken))
      .Throws(new InvalidOperationException("encryption failed"));

    var store = CreateStore();

    // Act
    var result = await store.SaveBroadcasterTokensAsync(tokens, CancellationToken.None);

    // Assert
    _ = result.IsFailed.Should().BeTrue();
    _ = result.Errors.Should().ContainSingle(e => e.Message.Contains("encryption failed"));
    _notifier.Verify(
      n => n.PublishAsync(It.IsAny<TwitchSetup>(), It.IsAny<CancellationToken>()),
      Times.Never);
  }

  [Fact]
  public async Task SaveBotTokensAsyncWhenSaveChangesThrowsDoesNotPublishNotificationAsync()
  {
    // Arrange
    var tokens = CreateBotTokens();

    _ = _encryptionService
      .Setup(e => e.Encrypt(It.IsAny<string>()))
      .Returns<string>(value => $"encrypted-{value}");

    _ = _session
      .Setup(s => s.SaveChangesAsync(It.IsAny<CancellationToken>()))
      .ThrowsAsync(new InvalidOperationException("db write failed"));

    var store = CreateStore();

    // Act
    var result = await store.SaveBotTokensAsync(tokens, CancellationToken.None);

    // Assert
    _ = result.IsFailed.Should().BeTrue();
    _ = result.Errors.Should().NotBeEmpty();
    _notifier.Verify(
      n => n.PublishAsync(It.IsAny<TwitchSetup>(), It.IsAny<CancellationToken>()),
      Times.Never);
  }

  [Fact]
  public async Task SaveBroadcasterTokensAsyncWhenSaveChangesThrowsDoesNotPublishNotificationAsync()
  {
    // Arrange
    var tokens = CreateBroadcasterTokens();

    _ = _encryptionService
      .Setup(e => e.Encrypt(It.IsAny<string>()))
      .Returns<string>(value => $"encrypted-{value}");

    _ = _session
      .Setup(s => s.SaveChangesAsync(It.IsAny<CancellationToken>()))
      .ThrowsAsync(new InvalidOperationException("db write failed"));

    var store = CreateStore();

    // Act
    var result = await store.SaveBroadcasterTokensAsync(tokens, CancellationToken.None);

    // Assert
    _ = result.IsFailed.Should().BeTrue();
    _ = result.Errors.Should().NotBeEmpty();
    _notifier.Verify(
      n => n.PublishAsync(It.IsAny<TwitchSetup>(), It.IsAny<CancellationToken>()),
      Times.Never);
  }

  private TwitchSetupStore CreateStore()
  {
    return new TwitchSetupStore(_session.Object, _encryptionService.Object, _notifier.Object);
  }

  private static TwitchSetup CreateSetup()
  {
    return new()
    {
      BotUserId = "111",
      BotLogin = "caramel_bot",
      Channels =
      [
        new TwitchChannel
        {
          UserId = "999",
          Login = "streamer",
        },
      ],
      ConfiguredOn = DateTimeOffset.UtcNow.AddMinutes(-5),
      UpdatedOn = DateTimeOffset.UtcNow,
    };
  }

  private static TwitchAccountTokens CreateBotTokens()
  {
    return new()
    {
      UserId = "111",
      Login = "caramel_bot",
      AccessToken = "bot-access-token",
      RefreshToken = "bot-refresh-token",
      ExpiresAt = DateTime.UtcNow.AddHours(1),
      LastRefreshedOn = DateTimeOffset.UtcNow,
    };
  }

  private static TwitchAccountTokens CreateBroadcasterTokens()
  {
    return new()
    {
      UserId = "999",
      Login = "streamer",
      AccessToken = "broadcaster-access-token",
      RefreshToken = "broadcaster-refresh-token",
      ExpiresAt = DateTime.UtcNow.AddHours(1),
      LastRefreshedOn = DateTimeOffset.UtcNow,
    };
  }
}
