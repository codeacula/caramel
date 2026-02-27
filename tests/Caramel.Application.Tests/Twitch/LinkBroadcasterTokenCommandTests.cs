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
  public async Task Handle_WithValidRequest_UpdatesSetupWithBroadcasterTokensAsync()
  {
    // Arrange
    var getResult = Result.Ok<TwitchSetup?>(_existingSetup);
    _mockStore.Setup(s => s.GetAsync(It.IsAny<CancellationToken>())).ReturnsAsync(getResult);

    TwitchSetup? capturedSetup = null;
    _mockStore
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
    result.IsSuccess.Should().BeTrue();
    result.Value.Should().NotBeNull();
    result.Value.BroadcasterTokens.Should().NotBeNull();
    result.Value.BroadcasterTokens!.UserId.Should().Be("broadcaster-999");
    result.Value.BroadcasterTokens!.Login.Should().Be("streamer");
    result.Value.BroadcasterTokens!.AccessToken.Should().Be("broadcaster-access-token");
    result.Value.BroadcasterTokens!.RefreshToken.Should().Be("broadcaster-refresh-token");

    capturedSetup.Should().NotBeNull();
    capturedSetup!.BroadcasterTokens.Should().NotBeNull();
  }

  [Fact]
  public async Task Handle_WhenSetupNotConfigured_ReturnsFailureAsync()
  {
    // Arrange
    var getResult = Result.Ok<TwitchSetup?>(null);
    _mockStore.Setup(s => s.GetAsync(It.IsAny<CancellationToken>())).ReturnsAsync(getResult);

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
    result.IsFailed.Should().BeTrue();
    result.Errors.Should().HaveCount(1);
    result.Errors[0].Message.Should().Contain("not configured");
  }

  [Fact]
  public async Task Handle_WhenGetSetupFails_ReturnsFailureAsync()
  {
    // Arrange
    var getResult = Result.Fail<TwitchSetup?>("Database error");
    _mockStore.Setup(s => s.GetAsync(It.IsAny<CancellationToken>())).ReturnsAsync(getResult);

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
    result.IsFailed.Should().BeTrue();
    result.Errors.Should().HaveCount(1);
  }

  [Fact]
  public async Task Handle_PreservesExistingBotTokensAsync()
  {
    // Arrange
    var getResult = Result.Ok<TwitchSetup?>(_existingSetup);
    _mockStore.Setup(s => s.GetAsync(It.IsAny<CancellationToken>())).ReturnsAsync(getResult);

    TwitchSetup? capturedSetup = null;
    _mockStore
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
    result.IsSuccess.Should().BeTrue();
    result.Value.BotTokens.Should().NotBeNull();
    result.Value.BotTokens!.UserId.Should().Be("bot-111");
    result.Value.BotTokens!.Login.Should().Be("caramel_bot");
  }

  [Fact]
  public async Task Handle_WithoutRefreshToken_StoresNullRefreshTokenAsync()
  {
    // Arrange
    var getResult = Result.Ok<TwitchSetup?>(_existingSetup);
    _mockStore.Setup(s => s.GetAsync(It.IsAny<CancellationToken>())).ReturnsAsync(getResult);

    TwitchSetup? capturedSetup = null;
    _mockStore
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
    result.IsSuccess.Should().BeTrue();
    result.Value.BroadcasterTokens.Should().NotBeNull();
    result.Value.BroadcasterTokens!.RefreshToken.Should().BeNull();
  }

  [Fact]
  public async Task Handle_UpdatesTimestampsAsync()
  {
    // Arrange
    var getResult = Result.Ok<TwitchSetup?>(_existingSetup);
    _mockStore.Setup(s => s.GetAsync(It.IsAny<CancellationToken>())).ReturnsAsync(getResult);

    TwitchSetup? capturedSetup = null;
    _mockStore
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
    result.IsSuccess.Should().BeTrue();
    capturedSetup.Should().NotBeNull();
    capturedSetup!.UpdatedOn.Should().BeOnOrAfter(beforeHandle.AddSeconds(-1));
    capturedSetup!.UpdatedOn.Should().BeOnOrBefore(afterHandle.AddSeconds(1));
    capturedSetup!.ConfiguredOn.Should().Be(_existingSetup.ConfiguredOn); // ConfiguredOn should not change
  }
}
