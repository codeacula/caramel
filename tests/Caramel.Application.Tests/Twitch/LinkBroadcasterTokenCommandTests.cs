using Caramel.Application.Twitch;
using Caramel.Core.Twitch;
using Caramel.Domain.Twitch;

using FluentAssertions;

using FluentResults;

using Moq;

namespace Caramel.Application.Tests.Twitch;

/// <summary>
/// Tests for LinkBroadcasterTokenCommand.
/// </summary>
public sealed class LinkBroadcasterTokenCommandTests
{
  private readonly Mock<ITwitchSetupStore> _mockStore = new();

  private readonly TwitchSetup _existingSetup = new()
  {
    BotUserId = "bot-111",
    BotLogin = "caramel_bot",
    Channels = [new TwitchChannel { UserId = "streamer-999", Login = "streamer" }],
    ConfiguredOn = DateTimeOffset.UtcNow.AddDays(-1),
    UpdatedOn = DateTimeOffset.UtcNow.AddDays(-1),
    BotTokens = new TwitchAccountTokens
    {
      UserId = "bot-111",
      Login = "caramel_bot",
      AccessToken = "bot-access-token",
      RefreshToken = "bot-refresh-token",
      ExpiresAt = DateTime.UtcNow.AddHours(1),
      LastRefreshedOn = DateTimeOffset.UtcNow,
    },
  };

  [Fact]
  public async Task HandleWithValidRequestUpdatesSetupWithBroadcasterTokensAsync()
  {
    // Arrange
    var getResult = Result.Ok<TwitchSetup?>(_existingSetup);
    _ = _mockStore.Setup(s => s.GetAsync(It.IsAny<CancellationToken>())).ReturnsAsync(getResult);

    TwitchSetup? capturedSetup = null;
    _ = _mockStore
      .Setup(s => s.SaveAsync(It.IsAny<TwitchSetup>(), It.IsAny<CancellationToken>()))
      .Callback<TwitchSetup, CancellationToken>((setup, _) => capturedSetup = setup)
      .ReturnsAsync((TwitchSetup setup, CancellationToken _) => Result.Ok(setup));

    var command = new LinkBroadcasterTokenCommand
    {
      BroadcasterUserId = "broadcaster-999",
      BroadcasterLogin = "streamer",
      AccessToken = "broadcaster-access-token",
      RefreshToken = "broadcaster-refresh-token",
      ExpiresAt = DateTime.UtcNow.AddHours(1),
    };

    var handler = new LinkBroadcasterTokenCommandHandler(_mockStore.Object);

    // Act
    var result = await handler.Handle(command, CancellationToken.None);

    // Assert
    _ = result.IsSuccess.Should().BeTrue();
    _ = result.Value.Should().NotBeNull();
    _ = result.Value.BroadcasterTokens.Should().NotBeNull();
    _ = result.Value.BroadcasterTokens!.UserId.Should().Be("broadcaster-999");
    _ = result.Value.BroadcasterTokens!.Login.Should().Be("streamer");
    _ = result.Value.BroadcasterTokens!.AccessToken.Should().Be("broadcaster-access-token");
    _ = result.Value.BroadcasterTokens!.RefreshToken.Should().Be("broadcaster-refresh-token");

    _ = capturedSetup.Should().NotBeNull();
    _ = capturedSetup!.BroadcasterTokens.Should().NotBeNull();
  }

  [Fact]
  public async Task HandleWhenSetupNotConfiguredReturnsFailureAsync()
  {
    // Arrange
    var getResult = Result.Ok<TwitchSetup?>(null);
    _ = _mockStore.Setup(s => s.GetAsync(It.IsAny<CancellationToken>())).ReturnsAsync(getResult);

    var command = new LinkBroadcasterTokenCommand
    {
      BroadcasterUserId = "broadcaster-999",
      BroadcasterLogin = "streamer",
      AccessToken = "broadcaster-access-token",
      RefreshToken = "broadcaster-refresh-token",
      ExpiresAt = DateTime.UtcNow.AddHours(1),
    };

    var handler = new LinkBroadcasterTokenCommandHandler(_mockStore.Object);

    // Act
    var result = await handler.Handle(command, CancellationToken.None);

    // Assert
    _ = result.IsFailed.Should().BeTrue();
    _ = result.Errors.Should().HaveCount(1);
    _ = result.Errors[0].Message.Should().Contain("not configured");
  }

  [Fact]
  public async Task HandleWhenGetSetupFailsReturnsFailureAsync()
  {
    // Arrange
    var getResult = Result.Fail<TwitchSetup?>("Database error");
    _ = _mockStore.Setup(s => s.GetAsync(It.IsAny<CancellationToken>())).ReturnsAsync(getResult);

    var command = new LinkBroadcasterTokenCommand
    {
      BroadcasterUserId = "broadcaster-999",
      BroadcasterLogin = "streamer",
      AccessToken = "broadcaster-access-token",
      RefreshToken = "broadcaster-refresh-token",
      ExpiresAt = DateTime.UtcNow.AddHours(1),
    };

    var handler = new LinkBroadcasterTokenCommandHandler(_mockStore.Object);

    // Act
    var result = await handler.Handle(command, CancellationToken.None);

    // Assert
    _ = result.IsFailed.Should().BeTrue();
    _ = result.Errors.Should().HaveCount(1);
  }

  [Fact]
  public async Task HandlePreservesExistingBotTokensAsync()
  {
    // Arrange
    var getResult = Result.Ok<TwitchSetup?>(_existingSetup);
    _ = _mockStore.Setup(s => s.GetAsync(It.IsAny<CancellationToken>())).ReturnsAsync(getResult);

    TwitchSetup? capturedSetup = null;
    _ = _mockStore
      .Setup(s => s.SaveAsync(It.IsAny<TwitchSetup>(), It.IsAny<CancellationToken>()))
      .Callback<TwitchSetup, CancellationToken>((setup, _) => capturedSetup = setup)
      .ReturnsAsync((TwitchSetup setup, CancellationToken _) => Result.Ok(setup));

    var command = new LinkBroadcasterTokenCommand
    {
      BroadcasterUserId = "broadcaster-999",
      BroadcasterLogin = "streamer",
      AccessToken = "broadcaster-access-token",
      RefreshToken = "broadcaster-refresh-token",
      ExpiresAt = DateTime.UtcNow.AddHours(1),
    };

    var handler = new LinkBroadcasterTokenCommandHandler(_mockStore.Object);

    // Act
    var result = await handler.Handle(command, CancellationToken.None);

    // Assert
    _ = result.IsSuccess.Should().BeTrue();
    _ = result.Value.BotTokens.Should().NotBeNull();
    _ = result.Value.BotTokens!.UserId.Should().Be("bot-111");
    _ = result.Value.BotTokens!.Login.Should().Be("caramel_bot");
  }

  [Fact]
  public async Task HandleWithoutRefreshTokenStoresNullRefreshTokenAsync()
  {
    // Arrange
    var getResult = Result.Ok<TwitchSetup?>(_existingSetup);
    _ = _mockStore.Setup(s => s.GetAsync(It.IsAny<CancellationToken>())).ReturnsAsync(getResult);

    TwitchSetup? capturedSetup = null;
    _ = _mockStore
      .Setup(s => s.SaveAsync(It.IsAny<TwitchSetup>(), It.IsAny<CancellationToken>()))
      .Callback<TwitchSetup, CancellationToken>((setup, _) => capturedSetup = setup)
      .ReturnsAsync((TwitchSetup setup, CancellationToken _) => Result.Ok(setup));

    var command = new LinkBroadcasterTokenCommand
    {
      BroadcasterUserId = "broadcaster-999",
      BroadcasterLogin = "streamer",
      AccessToken = "broadcaster-access-token",
      RefreshToken = null,
      ExpiresAt = DateTime.UtcNow.AddHours(1),
    };

    var handler = new LinkBroadcasterTokenCommandHandler(_mockStore.Object);

    // Act
    var result = await handler.Handle(command, CancellationToken.None);

    // Assert
    _ = result.IsSuccess.Should().BeTrue();
    _ = result.Value.BroadcasterTokens.Should().NotBeNull();
    _ = result.Value.BroadcasterTokens!.RefreshToken.Should().BeNull();
  }

  [Fact]
  public async Task HandleUpdatesTimestampsAsync()
  {
    // Arrange
    var getResult = Result.Ok<TwitchSetup?>(_existingSetup);
    _ = _mockStore.Setup(s => s.GetAsync(It.IsAny<CancellationToken>())).ReturnsAsync(getResult);

    TwitchSetup? capturedSetup = null;
    _ = _mockStore
      .Setup(s => s.SaveAsync(It.IsAny<TwitchSetup>(), It.IsAny<CancellationToken>()))
      .Callback<TwitchSetup, CancellationToken>((setup, _) => capturedSetup = setup)
      .ReturnsAsync((TwitchSetup setup, CancellationToken _) => Result.Ok(setup));

    var command = new LinkBroadcasterTokenCommand
    {
      BroadcasterUserId = "broadcaster-999",
      BroadcasterLogin = "streamer",
      AccessToken = "broadcaster-access-token",
      RefreshToken = "broadcaster-refresh-token",
      ExpiresAt = DateTime.UtcNow.AddHours(1),
    };

    var handler = new LinkBroadcasterTokenCommandHandler(_mockStore.Object);
    var beforeHandle = DateTimeOffset.UtcNow;

    // Act
    var result = await handler.Handle(command, CancellationToken.None);

    var afterHandle = DateTimeOffset.UtcNow;

    // Assert
    _ = result.IsSuccess.Should().BeTrue();
    _ = capturedSetup.Should().NotBeNull();
    _ = capturedSetup!.UpdatedOn.Should().BeOnOrAfter(beforeHandle.AddSeconds(-1));
    _ = capturedSetup!.UpdatedOn.Should().BeOnOrBefore(afterHandle.AddSeconds(1));
    _ = capturedSetup!.ConfiguredOn.Should().Be(_existingSetup.ConfiguredOn); // ConfiguredOn should not change
  }
}
