using Caramel.AI;
using Caramel.AI.DTOs;
using Caramel.AI.Requests;
using Caramel.Application.Conversations;
using Caramel.Core.People;
using Caramel.Domain.Common.Enums;
using Caramel.Domain.Common.ValueObjects;
using Caramel.Domain.People.Models;
using Caramel.Domain.People.ValueObjects;

using FluentResults;

using Moq;

namespace Caramel.Application.Tests.Conversations;

public sealed class AskTheOrbCommandHandlerTests
{
  private readonly Mock<ICaramelAIAgent> _aiAgentMock;
  private readonly Mock<IAIRequestBuilder> _requestBuilderMock;
  private readonly Mock<IPersonStore> _personStoreMock;
  private readonly AskTheOrbCommandHandler _handler;

  public AskTheOrbCommandHandlerTests()
  {
    _aiAgentMock = new Mock<ICaramelAIAgent>();
    _requestBuilderMock = new Mock<IAIRequestBuilder>();
    _personStoreMock = new Mock<IPersonStore>();

    _handler = new AskTheOrbCommandHandler(
      _aiAgentMock.Object,
      _personStoreMock.Object,
      TimeProvider.System);
  }

  [Fact]
  public async Task HandleWithValidInputReturnsAiResponseAsync()
  {
    // Arrange
    var personId = new PersonId(Guid.NewGuid());
    var command = new AskTheOrbCommand(personId, new Content("What is the airspeed velocity of an unladen swallow?"));

    _ = _personStoreMock
      .Setup(x => x.GetAsync(personId, It.IsAny<CancellationToken>()))
      .ReturnsAsync(Result.Ok(CreatePerson(personId)));

    _ = _aiAgentMock
      .Setup(x => x.CreateResponseRequest(It.IsAny<IEnumerable<ChatMessageDTO>>(), It.IsAny<string>(), It.IsAny<string>()))
      .Returns(_requestBuilderMock.Object);

    _ = _requestBuilderMock
      .Setup(x => x.ExecuteAsync(It.IsAny<CancellationToken>()))
      .ReturnsAsync(new AIRequestResult { Success = true, Content = "An African or European swallow?" });

    // Act
    var result = await _handler.Handle(command, CancellationToken.None);

    // Assert
    Assert.True(result.IsSuccess);
    Assert.Equal("An African or European swallow?", result.Value);
  }

  [Fact]
  public async Task HandleWithMissingPersonReturnsFailureAsync()
  {
    // Arrange
    var personId = new PersonId(Guid.NewGuid());
    var command = new AskTheOrbCommand(personId, new Content("hello"));

    _ = _personStoreMock
      .Setup(x => x.GetAsync(personId, It.IsAny<CancellationToken>()))
      .ReturnsAsync(Result.Fail<Person>("not found"));

    // Act
    var result = await _handler.Handle(command, CancellationToken.None);

    // Assert
    Assert.True(result.IsFailed);
    Assert.Contains($"Failed to load person {personId.Value}", result.Errors.Select(e => e.Message));
  }

  [Fact]
  public async Task HandleWithFailedAiRequestReturnsFailureAsync()
  {
    // Arrange
    var personId = new PersonId(Guid.NewGuid());
    var command = new AskTheOrbCommand(personId, new Content("hello"));

    _ = _personStoreMock
      .Setup(x => x.GetAsync(personId, It.IsAny<CancellationToken>()))
      .ReturnsAsync(Result.Ok(CreatePerson(personId)));

    _ = _aiAgentMock
      .Setup(x => x.CreateResponseRequest(It.IsAny<IEnumerable<ChatMessageDTO>>(), It.IsAny<string>(), It.IsAny<string>()))
      .Returns(_requestBuilderMock.Object);

    _ = _requestBuilderMock
      .Setup(x => x.ExecuteAsync(It.IsAny<CancellationToken>()))
      .ReturnsAsync(new AIRequestResult { Success = false, ErrorMessage = "Model unavailable" });

    // Act
    var result = await _handler.Handle(command, CancellationToken.None);

    // Assert
    Assert.True(result.IsFailed);
    Assert.Contains("Model unavailable", result.Errors.Select(e => e.Message));
  }

  [Fact]
  public async Task HandleWithEmptyInputReturnsFailureAsync()
  {
    // Arrange
    var personId = new PersonId(Guid.NewGuid());
    var command = new AskTheOrbCommand(personId, new Content("   "));

    // Act
    var result = await _handler.Handle(command, CancellationToken.None);

    // Assert
    Assert.True(result.IsFailed);
    Assert.Contains("AskTheOrb request content cannot be empty.", result.Errors.Select(e => e.Message));
  }

  private static Person CreatePerson(PersonId personId)
  {
    var utcNow = DateTime.UtcNow;
    return new Person
    {
      Id = personId,
      PlatformId = new PlatformId("viewer", "viewer-123", Platform.Twitch),
      Username = new Username("viewer"),
      HasAccess = new HasAccess(true),
      CreatedOn = new CreatedOn(utcNow),
      UpdatedOn = new UpdatedOn(utcNow)
    };
  }
}
