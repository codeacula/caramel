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

  private GetTwitchSetupQueryHandler CreateHandler()
  {
    return new(_store.Object);
  }

  private static TwitchSetup MakeSetup()
  {
    return new()
    {
      BotUserId = "111",
      BotLogin = "caramel_bot",
      Channels = [new TwitchChannel { UserId = "999", Login = "streamer" }],
      ConfiguredOn = DateTimeOffset.UtcNow,
      UpdatedOn = DateTimeOffset.UtcNow,
    };
  }

  [Fact]
  public async Task HandleReturnsOkNullWhenStoreReturnsNullAsync()
  {
    _ = _store
      .Setup(s => s.GetAsync(It.IsAny<CancellationToken>()))
      .ReturnsAsync(Result.Ok<TwitchSetup?>(null));

    var handler = CreateHandler();
    var result = await handler.Handle(new GetTwitchSetupQuery(), CancellationToken.None);

    _ = result.IsSuccess.Should().BeTrue();
    _ = result.Value.Should().BeNull();
  }

  [Fact]
  public async Task HandleReturnsOkSetupWhenStoreReturnsSetupAsync()
  {
    var setup = MakeSetup();
    _ = _store
      .Setup(s => s.GetAsync(It.IsAny<CancellationToken>()))
      .ReturnsAsync(Result.Ok<TwitchSetup?>(setup));

    var handler = CreateHandler();
    var result = await handler.Handle(new GetTwitchSetupQuery(), CancellationToken.None);

    _ = result.IsSuccess.Should().BeTrue();
    _ = result.Value.Should().BeSameAs(setup);
  }

  [Fact]
  public async Task HandleReturnsFailWhenStoreThrowsAsync()
  {
    _ = _store
      .Setup(s => s.GetAsync(It.IsAny<CancellationToken>()))
      .ThrowsAsync(new InvalidOperationException("connection lost"));

    var handler = CreateHandler();
    var result = await handler.Handle(new GetTwitchSetupQuery(), CancellationToken.None);

    _ = result.IsFailed.Should().BeTrue();
    _ = result.Errors.Should().ContainSingle(e => e.Message == "connection lost");
  }
}
