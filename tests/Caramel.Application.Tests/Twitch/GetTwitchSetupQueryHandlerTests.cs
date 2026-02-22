using Caramel.Application.Twitch;
using Caramel.Core.Twitch;
using Caramel.Domain.Twitch;

using FluentAssertions;

using FluentResults;

using Moq;

namespace Caramel.Application.Tests.Twitch;

public sealed class GetTwitchSetupQueryHandlerTests
{
  private readonly Mock<ITwitchSetupStore> _store = new();

  private GetTwitchSetupQueryHandler CreateHandler() =>
    new(_store.Object);

  private static TwitchSetup MakeSetup() =>
    new()
    {
      BotUserId = "111",
      BotLogin = "caramel_bot",
      Channels = [new TwitchChannel { UserId = "999", Login = "streamer" }],
      ConfiguredOn = DateTimeOffset.UtcNow,
      UpdatedOn = DateTimeOffset.UtcNow,
    };

  [Fact]
  public async Task Handle_ReturnsOkNull_WhenStoreReturnsNull()
  {
    _store
      .Setup(s => s.GetAsync(It.IsAny<CancellationToken>()))
      .ReturnsAsync(Result.Ok<TwitchSetup?>(null));

    var handler = CreateHandler();
    var result = await handler.Handle(new GetTwitchSetupQuery(), CancellationToken.None);

    result.IsSuccess.Should().BeTrue();
    result.Value.Should().BeNull();
  }

  [Fact]
  public async Task Handle_ReturnsOkSetup_WhenStoreReturnsSetup()
  {
    var setup = MakeSetup();
    _store
      .Setup(s => s.GetAsync(It.IsAny<CancellationToken>()))
      .ReturnsAsync(Result.Ok<TwitchSetup?>(setup));

    var handler = CreateHandler();
    var result = await handler.Handle(new GetTwitchSetupQuery(), CancellationToken.None);

    result.IsSuccess.Should().BeTrue();
    result.Value.Should().BeSameAs(setup);
  }

  [Fact]
  public async Task Handle_ReturnsFail_WhenStoreThrows()
  {
    _store
      .Setup(s => s.GetAsync(It.IsAny<CancellationToken>()))
      .ThrowsAsync(new InvalidOperationException("connection lost"));

    var handler = CreateHandler();
    var result = await handler.Handle(new GetTwitchSetupQuery(), CancellationToken.None);

    result.IsFailed.Should().BeTrue();
    result.Errors.Should().ContainSingle(e => e.Message == "connection lost");
  }
}
