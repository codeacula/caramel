using Caramel.Application.Twitch;
using Caramel.Core.Twitch;
using Caramel.Domain.Twitch;

using FluentAssertions;

using FluentResults;

using Moq;

namespace Caramel.Application.Tests.Twitch;

public sealed class SaveTwitchSetupCommandHandlerTests
{
  private readonly Mock<ITwitchSetupStore> _store = new();

  private SaveTwitchSetupCommandHandler CreateHandler()
  {
    return new(_store.Object);
  }

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

  private static SaveTwitchSetupCommand MakeCommand(string botLogin = "caramel_bot")
  {
    return new()
    {
      BotUserId = "111",
      BotLogin = botLogin,
      Channels = [("999", "streamer")],
    };
  }

  [Fact]
  public async Task HandleReturnsOkSetupWhenStoreSavesAsync()
  {
    var saved = MakeSetup();
    _ = _store
      .Setup(s => s.SaveAsync(It.IsAny<TwitchSetup>(), It.IsAny<CancellationToken>()))
      .ReturnsAsync(Result.Ok(saved));

    var handler = CreateHandler();
    var result = await handler.Handle(MakeCommand(), CancellationToken.None);

    _ = result.IsSuccess.Should().BeTrue();
    _ = result.Value.Should().BeSameAs(saved);
  }

  [Fact]
  public async Task HandleReturnsFailWhenStoreFailsAsync()
  {
    _ = _store
      .Setup(s => s.SaveAsync(It.IsAny<TwitchSetup>(), It.IsAny<CancellationToken>()))
      .ReturnsAsync(Result.Fail<TwitchSetup>("DB write failed"));

    var handler = CreateHandler();
    var result = await handler.Handle(MakeCommand(), CancellationToken.None);

    _ = result.IsFailed.Should().BeTrue();
    _ = result.Errors.Should().ContainSingle(e => e.Message == "DB write failed");
  }

  [Fact]
  public async Task HandleMapsCommandFieldsToTwitchSetupAsync()
  {
    TwitchSetup? capturedSetup = null;
    _ = _store
      .Setup(s => s.SaveAsync(It.IsAny<TwitchSetup>(), It.IsAny<CancellationToken>()))
      .Callback<TwitchSetup, CancellationToken>((setup, _) => capturedSetup = setup)
      .ReturnsAsync(Result.Ok(MakeSetup()));

    var handler = CreateHandler();
    _ = await handler.Handle(
      new SaveTwitchSetupCommand
      {
        BotUserId = "111",
        BotLogin = "caramel_bot",
        Channels = [("999", "streamer"), ("888", "other_channel")],
      },
      CancellationToken.None);

    _ = capturedSetup.Should().NotBeNull();
    _ = capturedSetup!.BotUserId.Should().Be("111");
    _ = capturedSetup.BotLogin.Should().Be("caramel_bot");
    _ = capturedSetup.Channels.Should().HaveCount(2);
    _ = capturedSetup.Channels[0].UserId.Should().Be("999");
    _ = capturedSetup.Channels[0].Login.Should().Be("streamer");
    _ = capturedSetup.Channels[1].UserId.Should().Be("888");
    _ = capturedSetup.Channels[1].Login.Should().Be("other_channel");
  }

  [Fact]
  public async Task HandleReturnsFailWhenStoreThrowsAsync()
  {
    _ = _store
      .Setup(s => s.SaveAsync(It.IsAny<TwitchSetup>(), It.IsAny<CancellationToken>()))
      .ThrowsAsync(new InvalidOperationException("connection lost"));

    var handler = CreateHandler();
    var result = await handler.Handle(MakeCommand(), CancellationToken.None);

    _ = result.IsFailed.Should().BeTrue();
    _ = result.Errors.Should().ContainSingle(e => e.Message.Contains("Invalid operation state"));
  }
}
