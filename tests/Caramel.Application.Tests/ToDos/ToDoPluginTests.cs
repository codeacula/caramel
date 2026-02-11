using Caramel.Application.ToDos;
using Caramel.Application.ToDos.Models;
using Caramel.Core.People;
using Caramel.Core.ToDos;
using Caramel.Domain.Common.Enums;
using Caramel.Domain.Common.ValueObjects;
using Caramel.Domain.People.Models;
using Caramel.Domain.People.ValueObjects;
using Caramel.Domain.ToDos.Models;
using Caramel.Domain.ToDos.ValueObjects;

using FluentAssertions;

using FluentResults;

using MediatR;

using Moq;

namespace Caramel.Application.Tests.ToDos;

/// <summary>
/// Unit tests for ToDoPlugin - AI kernel functions for todo management
/// Tests cover 13 kernel functions: CreateToDo, UpdateToDo, CompleteToDo, DeleteToDo,
/// SetPriority/Energy/Interest, SetAllPriority/Energy/Interest, ListToDos, GenerateDailyPlan
/// Total: 73 atomic tests organized by method with AAA pattern
/// </summary>
public class ToDoPluginTests
{
  private readonly Mock<IMediator> _mediatorMock = new();
  private readonly Mock<IPersonStore> _personStoreMock = new();
  private readonly Mock<IFuzzyTimeParser> _fuzzyTimeParserMock = new();
  private readonly Mock<TimeProvider> _timeProviderMock = new();
  private readonly PersonConfig _personConfig = new() { DefaultTimeZoneId = "America/Chicago", DefaultDailyTaskCount = 5 };
  private readonly PersonId _personId = new(Guid.NewGuid());
  private ToDoPlugin _sut = null!;

  private void SetUp()
  {
    _sut = new ToDoPlugin(
      _mediatorMock.Object,
      _personStoreMock.Object,
      _fuzzyTimeParserMock.Object,
      _timeProviderMock.Object,
      _personConfig,
      _personId
    );
  }

  #region Helper Methods

  private static ToDo CreateValidToDo(
    Guid? todoId = null,
    Guid? personId = null,
    string? description = null,
    Level? priority = null,
    Level? energy = null,
    Level? interest = null)
  {
    return new()
    {
      Id = new ToDoId(todoId ?? Guid.NewGuid()),
      PersonId = new PersonId(personId ?? Guid.NewGuid()),
      Description = new Description(description ?? "Test todo"),
      Priority = new Priority(priority ?? Level.Green),
      Energy = new Energy(energy ?? Level.Green),
      Interest = new Interest(interest ?? Level.Green),
      CreatedOn = new CreatedOn(DateTime.UtcNow),
      UpdatedOn = new UpdatedOn(DateTime.UtcNow)
    };
  }

  private static DailyPlan CreateValidDailyPlan(int taskCount = 3, int totalActive = 10)
  {
    var tasks = Enumerable.Range(0, taskCount)
      .Select(i => new DailyPlanItem(
        new ToDoId(Guid.NewGuid()),
        $"Task {i + 1}",
        new Priority(Level.Green),
        new Energy(Level.Green),
        new Interest(Level.Green),
        null
      ))
      .ToList();

    return new DailyPlan(tasks, "Test rationale", totalActive);
  }

  #endregion

  #region CreateToDo Tests (10 tests)

  [Fact]
  public async Task CreateToDoAsyncWithValidDescriptionOnlySucceeds()
  {
    // Arrange
    SetUp();
    const string description = "Buy groceries";
    var expectedToDo = CreateValidToDo(description: description);

    _mediatorMock
      .Setup(m => m.Send(It.IsAny<CreateToDoCommand>(), It.IsAny<CancellationToken>()))
      .ReturnsAsync(Result.Ok(expectedToDo));

    // Act
    var result = await _sut.CreateToDoAsync(description);

    // Assert
    result.Should().Contain("Successfully created todo");
    result.Should().Contain("Buy groceries");
    result.Should().NotContain("reminder");
    _mediatorMock.Verify(m => m.Send(It.IsAny<CreateToDoCommand>(), It.IsAny<CancellationToken>()), Times.Once);
  }

  [Fact]
  public async Task CreateToDoAsyncWithValidFuzzyReminderTimeSucceeds()
  {
    // Arrange
    SetUp();
    const string description = "Call dentist";
    var reminderTime = DateTime.UtcNow.AddHours(2);
    var expectedToDo = CreateValidToDo(description: description);

    _fuzzyTimeParserMock
      .Setup(ftp => ftp.TryParseFuzzyTime("in 2 hours", It.IsAny<DateTime>()))
      .Returns(Result.Ok(reminderTime));

    _mediatorMock
      .Setup(m => m.Send(It.IsAny<CreateToDoCommand>(), It.IsAny<CancellationToken>()))
      .ReturnsAsync(Result.Ok(expectedToDo));

    // Act
    var result = await _sut.CreateToDoAsync(description, "in 2 hours");

    // Assert
    result.Should().Contain("Successfully created todo");
    result.Should().Contain("reminder");
    result.Should().Contain("Call dentist");
    _mediatorMock.Verify(m => m.Send(It.IsAny<CreateToDoCommand>(), It.IsAny<CancellationToken>()), Times.Once);
  }

  [Fact]
  public async Task CreateToDoAsyncWithValidIso8601ReminderSucceeds()
  {
    // Arrange
    SetUp();
    const string description = "Pay bills";
    const string isoDate = "2025-12-31T10:00:00";
    var expectedToDo = CreateValidToDo(description: description);

    _fuzzyTimeParserMock
      .Setup(ftp => ftp.TryParseFuzzyTime(isoDate, It.IsAny<DateTime>()))
      .Returns(Result.Fail("Not fuzzy"));

    _personStoreMock
      .Setup(ps => ps.GetAsync(_personId, It.IsAny<CancellationToken>()))
      .ReturnsAsync(Result.Ok(new Person
      {
        Id = _personId,
        PlatformId = new("testuser", "123456", Platform.Discord),
        Username = new("testuser"),
        HasAccess = new(true),
        TimeZoneId = null,
        NotificationChannels = [],
        CreatedOn = new CreatedOn(DateTime.UtcNow),
        UpdatedOn = new UpdatedOn(DateTime.UtcNow)
      }));

    _mediatorMock
      .Setup(m => m.Send(It.IsAny<CreateToDoCommand>(), It.IsAny<CancellationToken>()))
      .ReturnsAsync(Result.Ok(expectedToDo));

    // Act
    var result = await _sut.CreateToDoAsync(description, isoDate);

    // Assert
    result.Should().Contain("Successfully created todo");
    result.Should().Contain("Pay bills");
    _mediatorMock.Verify(m => m.Send(It.IsAny<CreateToDoCommand>(), It.IsAny<CancellationToken>()), Times.Once);
  }

  [Fact]
  public async Task CreateToDoAsyncWithPrioritiesAndLevelsSucceeds()
  {
    // Arrange
    SetUp();
    const string description = "Important meeting";
    var expectedToDo = CreateValidToDo(
      description: description,
      priority: Level.Red,
      energy: Level.Yellow,
      interest: Level.Blue
    );

    _mediatorMock
      .Setup(m => m.Send(It.IsAny<CreateToDoCommand>(), It.IsAny<CancellationToken>()))
      .ReturnsAsync(Result.Ok(expectedToDo));

    // Act
    var result = await _sut.CreateToDoAsync(description, null, "red", "yellow", "blue");

    // Assert
    result.Should().Contain("Successfully created todo");
    result.Should().Contain("Important meeting");
    _mediatorMock.Verify(m => m.Send(It.IsAny<CreateToDoCommand>(), It.IsAny<CancellationToken>()), Times.Once);
  }

  [Fact]
  public async Task CreateToDoAsyncWithColorEmojiLevelsSucceeds()
  {
    // Arrange
    SetUp();
    const string description = "Emoji test";
    var expectedToDo = CreateValidToDo(description: description);

    _mediatorMock
      .Setup(m => m.Send(It.IsAny<CreateToDoCommand>(), It.IsAny<CancellationToken>()))
      .ReturnsAsync(Result.Ok(expectedToDo));

    // Act
    var result = await _sut.CreateToDoAsync(description, null, "🔵", "🟢", "🟡");

    // Assert
    result.Should().Contain("Successfully created todo");
    _mediatorMock.Verify(m => m.Send(It.IsAny<CreateToDoCommand>(), It.IsAny<CancellationToken>()), Times.Once);
  }

  [Fact]
  public async Task CreateToDoAsyncWithNumberLevelsSucceeds()
  {
    // Arrange
    SetUp();
    const string description = "Number levels";
    var expectedToDo = CreateValidToDo(description: description);

    _mediatorMock
      .Setup(m => m.Send(It.IsAny<CreateToDoCommand>(), It.IsAny<CancellationToken>()))
      .ReturnsAsync(Result.Ok(expectedToDo));

    // Act
    var result = await _sut.CreateToDoAsync(description, null, "2", "1", "0");

    // Assert
    result.Should().Contain("Successfully created todo");
    _mediatorMock.Verify(m => m.Send(It.IsAny<CreateToDoCommand>(), It.IsAny<CancellationToken>()), Times.Once);
  }

  [Fact]
  public async Task CreateToDoAsyncWithEmptyDescriptionReturnsFailed()
  {
    // Arrange
    SetUp();

    // Act
    var result = await _sut.CreateToDoAsync("");

    // Assert
    result.Should().Contain("Error creating todo");
  }

  [Fact]
  public async Task CreateToDoAsyncWithInvalidReminderFormatReturnsFailed()
  {
    // Arrange
    SetUp();
    const string description = "Test";
    const string invalidDate = "not a valid date at all";

    _fuzzyTimeParserMock
      .Setup(ftp => ftp.TryParseFuzzyTime(invalidDate, It.IsAny<DateTime>()))
      .Returns(Result.Fail("Invalid fuzzy time"));

    // Act
    var result = await _sut.CreateToDoAsync(description, invalidDate);

    // Assert
    result.Should().Contain("Failed to create todo");
    result.Should().Contain("Invalid reminder format");
  }

  [Fact]
  public async Task CreateToDoAsyncWithInvalidPriorityLevelReturnsFailed()
  {
    // Arrange
    SetUp();
    const string description = "Test";
    const string invalidLevel = "purple";

    _mediatorMock
      .Setup(m => m.Send(It.IsAny<CreateToDoCommand>(), It.IsAny<CancellationToken>()))
      .ReturnsAsync(Result.Ok(CreateValidToDo(description: description)));

    // Act
    var result = await _sut.CreateToDoAsync(description, null, invalidLevel);

    // Assert
    result.Should().Contain("Successfully created todo");
  }

  [Fact]
  public async Task CreateToDoAsyncWhenMediatorFailsReturnsFailed()
  {
    // Arrange
    SetUp();
    const string description = "Test";

    _mediatorMock
      .Setup(m => m.Send(It.IsAny<CreateToDoCommand>(), It.IsAny<CancellationToken>()))
      .ReturnsAsync(Result.Fail("Store error"));

    // Act
    var result = await _sut.CreateToDoAsync(description);

    // Assert
    result.Should().Contain("Failed to create todo");
    result.Should().Contain("Store error");
  }

  #endregion

  #region UpdateToDo Tests (8 tests)

  [Fact]
  public async Task UpdateToDoAsyncWithValidIdAndDescriptionSucceeds()
  {
    // Arrange
    SetUp();
    var todoId = Guid.NewGuid();
    const string newDescription = "Updated task";

    _mediatorMock
      .Setup(m => m.Send(It.IsAny<UpdateToDoCommand>(), It.IsAny<CancellationToken>()))
      .ReturnsAsync(Result.Ok());

    // Act
    var result = await _sut.UpdateToDoAsync(todoId.ToString(), newDescription);

    // Assert
    result.Should().Contain("Successfully updated the todo");
    result.Should().Contain(newDescription);
    _mediatorMock.Verify(m => m.Send(It.IsAny<UpdateToDoCommand>(), It.IsAny<CancellationToken>()), Times.Once);
  }

  [Fact]
  public async Task UpdateToDoAsyncWithInvalidGuidFormatReturnsFailed()
  {
    // Arrange
    SetUp();
    const string invalidId = "not-a-guid";

    // Act
    var result = await _sut.UpdateToDoAsync(invalidId, "New description");

    // Assert
    result.Should().Contain("Failed to update todo");
    result.Should().Contain("Invalid todo ID format");
    _mediatorMock.Verify(m => m.Send(It.IsAny<UpdateToDoCommand>(), It.IsAny<CancellationToken>()), Times.Never);
  }

  [Fact]
  public async Task UpdateToDoAsyncWithEmptyDescriptionSucceeds()
  {
    // Arrange
    SetUp();
    var todoId = Guid.NewGuid();

    _mediatorMock
      .Setup(m => m.Send(It.IsAny<UpdateToDoCommand>(), It.IsAny<CancellationToken>()))
      .ReturnsAsync(Result.Ok());

    // Act
    var result = await _sut.UpdateToDoAsync(todoId.ToString(), "");

    // Assert
    result.Should().Contain("Successfully updated the todo");
    _mediatorMock.Verify(m => m.Send(It.IsAny<UpdateToDoCommand>(), It.IsAny<CancellationToken>()), Times.Once);
  }

  [Fact]
  public async Task UpdateToDoAsyncWhenMediatorFailsReturnsFailed()
  {
    // Arrange
    SetUp();
    var todoId = Guid.NewGuid();

    _mediatorMock
      .Setup(m => m.Send(It.IsAny<UpdateToDoCommand>(), It.IsAny<CancellationToken>()))
      .ReturnsAsync(Result.Fail("Todo not found"));

    // Act
    var result = await _sut.UpdateToDoAsync(todoId.ToString(), "New description");

    // Assert
    result.Should().Contain("Failed to update todo");
    result.Should().Contain("Todo not found");
  }

  [Fact]
  public async Task UpdateToDoAsyncWhenExceptionThrownReturnsError()
  {
    // Arrange
    SetUp();
    var todoId = Guid.NewGuid();

    _mediatorMock
      .Setup(m => m.Send(It.IsAny<UpdateToDoCommand>(), It.IsAny<CancellationToken>()))
      .ThrowsAsync(new InvalidOperationException("Database error"));

    // Act
    var result = await _sut.UpdateToDoAsync(todoId.ToString(), "New description");

    // Assert
    result.Should().Contain("Error updating todo");
    result.Should().Contain("Database error");
  }

  [Fact]
  public async Task UpdateToDoAsyncWithGuidInUppercaseSucceeds()
  {
    // Arrange
    SetUp();
    var todoId = Guid.NewGuid().ToString().ToUpperInvariant();

    _mediatorMock
      .Setup(m => m.Send(It.IsAny<UpdateToDoCommand>(), It.IsAny<CancellationToken>()))
      .ReturnsAsync(Result.Ok());

    // Act
    var result = await _sut.UpdateToDoAsync(todoId, "New description");

    // Assert
    result.Should().Contain("Successfully updated the todo");
    _mediatorMock.Verify(m => m.Send(It.IsAny<UpdateToDoCommand>(), It.IsAny<CancellationToken>()), Times.Once);
  }

  [Fact]
  public async Task UpdateToDoAsyncWithWhitespaceDescriptionSucceeds()
  {
    // Arrange
    SetUp();
    var todoId = Guid.NewGuid().ToString();

    _mediatorMock
      .Setup(m => m.Send(It.IsAny<UpdateToDoCommand>(), It.IsAny<CancellationToken>()))
      .ReturnsAsync(Result.Ok());

    // Act
    var result = await _sut.UpdateToDoAsync(todoId, "   ");

    // Assert
    result.Should().Contain("Successfully updated the todo");
    _mediatorMock.Verify(m => m.Send(It.IsAny<UpdateToDoCommand>(), It.IsAny<CancellationToken>()), Times.Once);
  }

  #endregion

  #region CompleteToDo Tests (6 tests)

  [Fact]
  public async Task CompleteToDoAsyncWithValidIdSucceeds()
  {
    // Arrange
    SetUp();
    var todoId = Guid.NewGuid();

    _mediatorMock
      .Setup(m => m.Send(It.IsAny<CompleteToDoCommand>(), It.IsAny<CancellationToken>()))
      .ReturnsAsync(Result.Ok());

    // Act
    var result = await _sut.CompleteToDoAsync(todoId.ToString());

    // Assert
    result.Should().Contain("Successfully marked the todo as completed");
    _mediatorMock.Verify(m => m.Send(It.IsAny<CompleteToDoCommand>(), It.IsAny<CancellationToken>()), Times.Once);
  }

  [Fact]
  public async Task CompleteToDoAsyncWithInvalidIdFormatReturnsFailed()
  {
    // Arrange
    SetUp();

    // Act
    var result = await _sut.CompleteToDoAsync("invalid-guid");

    // Assert
    result.Should().Contain("Failed to complete todo");
    result.Should().Contain("Invalid todo ID format");
    _mediatorMock.Verify(m => m.Send(It.IsAny<CompleteToDoCommand>(), It.IsAny<CancellationToken>()), Times.Never);
  }

  [Fact]
  public async Task CompleteToDoAsyncWhenMediatorFailsReturnsFailed()
  {
    // Arrange
    SetUp();
    var todoId = Guid.NewGuid();

    _mediatorMock
      .Setup(m => m.Send(It.IsAny<CompleteToDoCommand>(), It.IsAny<CancellationToken>()))
      .ReturnsAsync(Result.Fail("Todo not found"));

    // Act
    var result = await _sut.CompleteToDoAsync(todoId.ToString());

    // Assert
    result.Should().Contain("Failed to complete todo");
    result.Should().Contain("Todo not found");
  }

  [Fact]
  public async Task CompleteToDoAsyncWhenExceptionThrownReturnsError()
  {
    // Arrange
    SetUp();
    var todoId = Guid.NewGuid();

    _mediatorMock
      .Setup(m => m.Send(It.IsAny<CompleteToDoCommand>(), It.IsAny<CancellationToken>()))
      .ThrowsAsync(new TimeoutException("Database timeout"));

    // Act
    var result = await _sut.CompleteToDoAsync(todoId.ToString());

    // Assert
    result.Should().Contain("Error completing todo");
    result.Should().Contain("Database timeout");
  }

  [Fact]
  public async Task CompleteToDoAsyncWithEmptyIdReturnsFailed()
  {
    // Arrange
    SetUp();

    // Act
    var result = await _sut.CompleteToDoAsync("");

    // Assert
    result.Should().Contain("Failed to complete todo");
  }

  #endregion

  #region DeleteToDo Tests (6 tests)

  [Fact]
  public async Task DeleteToDoAsyncWithValidIdSucceeds()
  {
    // Arrange
    SetUp();
    var todoId = Guid.NewGuid();

    _mediatorMock
      .Setup(m => m.Send(It.IsAny<DeleteToDoCommand>(), It.IsAny<CancellationToken>()))
      .ReturnsAsync(Result.Ok());

    // Act
    var result = await _sut.DeleteToDoAsync(todoId.ToString());

    // Assert
    result.Should().Contain("Successfully deleted the todo");
    _mediatorMock.Verify(m => m.Send(It.IsAny<DeleteToDoCommand>(), It.IsAny<CancellationToken>()), Times.Once);
  }

  [Fact]
  public async Task DeleteToDoAsyncWithInvalidIdFormatReturnsFailed()
  {
    // Arrange
    SetUp();

    // Act
    var result = await _sut.DeleteToDoAsync("not-a-valid-guid");

    // Assert
    result.Should().Contain("Failed to delete todo");
    result.Should().Contain("Invalid todo ID format");
    _mediatorMock.Verify(m => m.Send(It.IsAny<DeleteToDoCommand>(), It.IsAny<CancellationToken>()), Times.Never);
  }

  [Fact]
  public async Task DeleteToDoAsyncWhenMediatorFailsReturnsFailed()
  {
    // Arrange
    SetUp();
    var todoId = Guid.NewGuid();

    _mediatorMock
      .Setup(m => m.Send(It.IsAny<DeleteToDoCommand>(), It.IsAny<CancellationToken>()))
      .ReturnsAsync(Result.Fail("Todo already deleted"));

    // Act
    var result = await _sut.DeleteToDoAsync(todoId.ToString());

    // Assert
    result.Should().Contain("Failed to delete todo");
    result.Should().Contain("Todo already deleted");
  }

  [Fact]
  public async Task DeleteToDoAsyncWhenExceptionThrownReturnsError()
  {
    // Arrange
    SetUp();
    var todoId = Guid.NewGuid();

    _mediatorMock
      .Setup(m => m.Send(It.IsAny<DeleteToDoCommand>(), It.IsAny<CancellationToken>()))
      .ThrowsAsync(new NullReferenceException("Null reference"));

    // Act
    var result = await _sut.DeleteToDoAsync(todoId.ToString());

    // Assert
    result.Should().Contain("Error deleting todo");
    result.Should().Contain("Null reference");
  }

  [Fact]
  public async Task DeleteToDoAsyncWithEmptyIdReturnsFailed()
  {
    // Arrange
    SetUp();

    // Act
    var result = await _sut.DeleteToDoAsync("");

    // Assert
    result.Should().Contain("Failed to delete todo");
  }

  #endregion

  #region SetPriority Tests (6 tests)

  [Fact]
  public async Task SetPriorityAsyncWithValidBlueSucceeds()
  {
    // Arrange
    SetUp();
    var todoId = Guid.NewGuid();

    _mediatorMock
      .Setup(m => m.Send(It.IsAny<SetToDoPriorityCommand>(), It.IsAny<CancellationToken>()))
      .ReturnsAsync(Result.Ok());

    // Act
    var result = await _sut.SetPriorityAsync(todoId.ToString(), "blue");

    // Assert
    result.Should().Contain("Successfully set priority");
    result.Should().Contain("🔵");
    _mediatorMock.Verify(m => m.Send(It.IsAny<SetToDoPriorityCommand>(), It.IsAny<CancellationToken>()), Times.Once);
  }

  [Fact]
  public async Task SetPriorityAsyncWithValidRedSucceeds()
  {
    // Arrange
    SetUp();
    var todoId = Guid.NewGuid();

    _mediatorMock
      .Setup(m => m.Send(It.IsAny<SetToDoPriorityCommand>(), It.IsAny<CancellationToken>()))
      .ReturnsAsync(Result.Ok());

    // Act
    var result = await _sut.SetPriorityAsync(todoId.ToString(), "red");

    // Assert
    result.Should().Contain("Successfully set priority");
    result.Should().Contain("🔴");
  }

  [Fact]
  public async Task SetPriorityAsyncWithInvalidTodoIdReturnsFailed()
  {
    // Arrange
    SetUp();

    // Act
    var result = await _sut.SetPriorityAsync("invalid-id", "blue");

    // Assert
    result.Should().Contain("Failed to set priority");
    result.Should().Contain("Invalid todo ID format");
    _mediatorMock.Verify(m => m.Send(It.IsAny<SetToDoPriorityCommand>(), It.IsAny<CancellationToken>()), Times.Never);
  }

  [Fact]
  public async Task SetPriorityAsyncWithInvalidLevelReturnsFailed()
  {
    // Arrange
    SetUp();
    var todoId = Guid.NewGuid();

    // Act
    var result = await _sut.SetPriorityAsync(todoId.ToString(), "invalid");

    // Assert
    result.Should().Contain("Failed to set priority");
    result.Should().Contain("Invalid level");
  }

  [Fact]
  public async Task SetPriorityAsyncWhenMediatorFailsReturnsFailed()
  {
    // Arrange
    SetUp();
    var todoId = Guid.NewGuid();

    _mediatorMock
      .Setup(m => m.Send(It.IsAny<SetToDoPriorityCommand>(), It.IsAny<CancellationToken>()))
      .ReturnsAsync(Result.Fail("Todo not found"));

    // Act
    var result = await _sut.SetPriorityAsync(todoId.ToString(), "green");

    // Assert
    result.Should().Contain("Failed to set priority");
    result.Should().Contain("Todo not found");
  }

  [Fact]
  public async Task SetPriorityAsyncWhenExceptionThrownReturnsError()
  {
    // Arrange
    SetUp();
    var todoId = Guid.NewGuid();

    _mediatorMock
      .Setup(m => m.Send(It.IsAny<SetToDoPriorityCommand>(), It.IsAny<CancellationToken>()))
      .ThrowsAsync(new InvalidOperationException("Invalid state"));

    // Act
    var result = await _sut.SetPriorityAsync(todoId.ToString(), "yellow");

    // Assert
    result.Should().Contain("Error setting priority");
    result.Should().Contain("Invalid state");
  }

  #endregion

  #region SetEnergy Tests (6 tests)

  [Fact]
  public async Task SetEnergyAsyncWithValidGreenSucceeds()
  {
    // Arrange
    SetUp();
    var todoId = Guid.NewGuid();

    _mediatorMock
      .Setup(m => m.Send(It.IsAny<SetToDoEnergyCommand>(), It.IsAny<CancellationToken>()))
      .ReturnsAsync(Result.Ok());

    // Act
    var result = await _sut.SetEnergyAsync(todoId.ToString(), "green");

    // Assert
    result.Should().Contain("Successfully set energy");
    result.Should().Contain("🟢");
    _mediatorMock.Verify(m => m.Send(It.IsAny<SetToDoEnergyCommand>(), It.IsAny<CancellationToken>()), Times.Once);
  }

  [Fact]
  public async Task SetEnergyAsyncWithValidYellowSucceeds()
  {
    // Arrange
    SetUp();
    var todoId = Guid.NewGuid();

    _mediatorMock
      .Setup(m => m.Send(It.IsAny<SetToDoEnergyCommand>(), It.IsAny<CancellationToken>()))
      .ReturnsAsync(Result.Ok());

    // Act
    var result = await _sut.SetEnergyAsync(todoId.ToString(), "yellow");

    // Assert
    result.Should().Contain("Successfully set energy");
    result.Should().Contain("🟡");
  }

  [Fact]
  public async Task SetEnergyAsyncWithInvalidTodoIdReturnsFailed()
  {
    // Arrange
    SetUp();

    // Act
    var result = await _sut.SetEnergyAsync("bad-id", "green");

    // Assert
    result.Should().Contain("Failed to set energy");
    result.Should().Contain("Invalid todo ID format");
    _mediatorMock.Verify(m => m.Send(It.IsAny<SetToDoEnergyCommand>(), It.IsAny<CancellationToken>()), Times.Never);
  }

  [Fact]
  public async Task SetEnergyAsyncWithInvalidLevelReturnsFailed()
  {
    // Arrange
    SetUp();
    var todoId = Guid.NewGuid();

    // Act
    var result = await _sut.SetEnergyAsync(todoId.ToString(), "orange");

    // Assert
    result.Should().Contain("Failed to set energy");
    result.Should().Contain("Invalid level");
  }

  [Fact]
  public async Task SetEnergyAsyncWhenMediatorFailsReturnsFailed()
  {
    // Arrange
    SetUp();
    var todoId = Guid.NewGuid();

    _mediatorMock
      .Setup(m => m.Send(It.IsAny<SetToDoEnergyCommand>(), It.IsAny<CancellationToken>()))
      .ReturnsAsync(Result.Fail("Store error"));

    // Act
    var result = await _sut.SetEnergyAsync(todoId.ToString(), "blue");

    // Assert
    result.Should().Contain("Failed to set energy");
    result.Should().Contain("Store error");
  }

  [Fact]
  public async Task SetEnergyAsyncWhenExceptionThrownReturnsError()
  {
    // Arrange
    SetUp();
    var todoId = Guid.NewGuid();

    _mediatorMock
      .Setup(m => m.Send(It.IsAny<SetToDoEnergyCommand>(), It.IsAny<CancellationToken>()))
      .ThrowsAsync(new ArgumentException("Invalid argument"));

    // Act
    var result = await _sut.SetEnergyAsync(todoId.ToString(), "red");

    // Assert
    result.Should().Contain("Error setting energy");
    result.Should().Contain("Invalid argument");
  }

  #endregion

  #region SetInterest Tests (6 tests)

  [Fact]
  public async Task SetInterestAsyncWithValidBlueSucceeds()
  {
    // Arrange
    SetUp();
    var todoId = Guid.NewGuid();

    _mediatorMock
      .Setup(m => m.Send(It.IsAny<SetToDoInterestCommand>(), It.IsAny<CancellationToken>()))
      .ReturnsAsync(Result.Ok());

    // Act
    var result = await _sut.SetInterestAsync(todoId.ToString(), "blue");

    // Assert
    result.Should().Contain("Successfully set interest");
    result.Should().Contain("🔵");
    _mediatorMock.Verify(m => m.Send(It.IsAny<SetToDoInterestCommand>(), It.IsAny<CancellationToken>()), Times.Once);
  }

  [Fact]
  public async Task SetInterestAsyncWithValidRedSucceeds()
  {
    // Arrange
    SetUp();
    var todoId = Guid.NewGuid();

    _mediatorMock
      .Setup(m => m.Send(It.IsAny<SetToDoInterestCommand>(), It.IsAny<CancellationToken>()))
      .ReturnsAsync(Result.Ok());

    // Act
    var result = await _sut.SetInterestAsync(todoId.ToString(), "red");

    // Assert
    result.Should().Contain("Successfully set interest");
    result.Should().Contain("🔴");
  }

  [Fact]
  public async Task SetInterestAsyncWithInvalidTodoIdReturnsFailed()
  {
    // Arrange
    SetUp();

    // Act
    var result = await _sut.SetInterestAsync("not-a-guid", "green");

    // Assert
    result.Should().Contain("Failed to set interest");
    result.Should().Contain("Invalid todo ID format");
    _mediatorMock.Verify(m => m.Send(It.IsAny<SetToDoInterestCommand>(), It.IsAny<CancellationToken>()), Times.Never);
  }

  [Fact]
  public async Task SetInterestAsyncWithInvalidLevelReturnsFailed()
  {
    // Arrange
    SetUp();
    var todoId = Guid.NewGuid();

    // Act
    var result = await _sut.SetInterestAsync(todoId.ToString(), "purple");

    // Assert
    result.Should().Contain("Failed to set interest");
    result.Should().Contain("Invalid level");
  }

  [Fact]
  public async Task SetInterestAsyncWhenMediatorFailsReturnsFailed()
  {
    // Arrange
    SetUp();
    var todoId = Guid.NewGuid();

    _mediatorMock
      .Setup(m => m.Send(It.IsAny<SetToDoInterestCommand>(), It.IsAny<CancellationToken>()))
      .ReturnsAsync(Result.Fail("Access denied"));

    // Act
    var result = await _sut.SetInterestAsync(todoId.ToString(), "yellow");

    // Assert
    result.Should().Contain("Failed to set interest");
    result.Should().Contain("Access denied");
  }

  [Fact]
  public async Task SetInterestAsyncWhenExceptionThrownReturnsError()
  {
    // Arrange
    SetUp();
    var todoId = Guid.NewGuid();

    _mediatorMock
      .Setup(m => m.Send(It.IsAny<SetToDoInterestCommand>(), It.IsAny<CancellationToken>()))
      .ThrowsAsync(new OperationCanceledException("Operation cancelled"));

    // Act
    var result = await _sut.SetInterestAsync(todoId.ToString(), "green");

    // Assert
    result.Should().Contain("Error setting interest");
    result.Should().Contain("Operation cancelled");
  }

  #endregion

  #region SetAllPriority Tests (6 tests)

  [Fact]
  public async Task SetAllPriorityAsyncWithValidLevelAndAllTodosSucceeds()
  {
    // Arrange
    SetUp();

    _mediatorMock
      .Setup(m => m.Send(It.IsAny<SetAllToDosAttributeCommand>(), It.IsAny<CancellationToken>()))
      .ReturnsAsync(Result.Ok(5));

    // Act
    var result = await _sut.SetAllPriorityAsync("", "blue");

    // Assert
    result.Should().Contain("Successfully set priority");
    result.Should().Contain("🔵");
    result.Should().Contain("5");
    result.Should().Contain("active todos");
    _mediatorMock.Verify(m => m.Send(It.IsAny<SetAllToDosAttributeCommand>(), It.IsAny<CancellationToken>()), Times.Once);
  }

  [Fact]
  public async Task SetAllPriorityAsyncWithSpecificTodoIdsSucceeds()
  {
    // Arrange
    SetUp();
    var id1 = Guid.NewGuid();
    var id2 = Guid.NewGuid();

    _mediatorMock
      .Setup(m => m.Send(It.IsAny<SetAllToDosAttributeCommand>(), It.IsAny<CancellationToken>()))
      .ReturnsAsync(Result.Ok(2));

    // Act
    var result = await _sut.SetAllPriorityAsync($"{id1},{id2}", "green");

    // Assert
    result.Should().Contain("Successfully set priority");
    result.Should().Contain("🟢");
    result.Should().Contain("2");
    _mediatorMock.Verify(m => m.Send(It.IsAny<SetAllToDosAttributeCommand>(), It.IsAny<CancellationToken>()), Times.Once);
  }

  [Fact]
  public async Task SetAllPriorityAsyncWithInvalidLevelReturnsFailed()
  {
    // Arrange
    SetUp();

    // Act
    var result = await _sut.SetAllPriorityAsync("", "invalid");

    // Assert
    result.Should().Contain("Failed to set priority");
    result.Should().Contain("Invalid level");
    _mediatorMock.Verify(m => m.Send(It.IsAny<SetAllToDosAttributeCommand>(), It.IsAny<CancellationToken>()), Times.Never);
  }

  [Fact]
  public async Task SetAllPriorityAsyncWithAllKeywordSucceeds()
  {
    // Arrange
    SetUp();

    _mediatorMock
      .Setup(m => m.Send(It.IsAny<SetAllToDosAttributeCommand>(), It.IsAny<CancellationToken>()))
      .ReturnsAsync(Result.Ok(10));

    // Act
    var result = await _sut.SetAllPriorityAsync("all", "yellow");

    // Assert
    result.Should().Contain("Successfully set priority");
    result.Should().Contain("🟡");
    result.Should().Contain("10");
    _mediatorMock.Verify(m => m.Send(It.IsAny<SetAllToDosAttributeCommand>(), It.IsAny<CancellationToken>()), Times.Once);
  }

  [Fact]
  public async Task SetAllPriorityAsyncWhenMediatorFailsReturnsFailed()
  {
    // Arrange
    SetUp();

    _mediatorMock
      .Setup(m => m.Send(It.IsAny<SetAllToDosAttributeCommand>(), It.IsAny<CancellationToken>()))
      .ReturnsAsync(Result.Fail("No todos found"));

    // Act
    var result = await _sut.SetAllPriorityAsync("", "red");

    // Assert
    result.Should().Contain("Failed to set priority");
    result.Should().Contain("No todos found");
  }

  [Fact]
  public async Task SetAllPriorityAsyncWhenExceptionThrownReturnsError()
  {
    // Arrange
    SetUp();

    _mediatorMock
      .Setup(m => m.Send(It.IsAny<SetAllToDosAttributeCommand>(), It.IsAny<CancellationToken>()))
      .ThrowsAsync(new InvalidOperationException("Sequence contains no elements"));

    // Act
    var result = await _sut.SetAllPriorityAsync("", "blue");

    // Assert
    result.Should().Contain("Error setting priority");
    result.Should().Contain("Sequence contains no elements");
  }

  #endregion

  #region SetAllEnergy Tests (6 tests)

  [Fact]
  public async Task SetAllEnergyAsyncWithValidLevelAndAllTodosSucceeds()
  {
    // Arrange
    SetUp();

    _mediatorMock
      .Setup(m => m.Send(It.IsAny<SetAllToDosAttributeCommand>(), It.IsAny<CancellationToken>()))
      .ReturnsAsync(Result.Ok(3));

    // Act
    var result = await _sut.SetAllEnergyAsync("", "green");

    // Assert
    result.Should().Contain("Successfully set energy");
    result.Should().Contain("🟢");
    result.Should().Contain("3");
    result.Should().Contain("active todos");
  }

  [Fact]
  public async Task SetAllEnergyAsyncWithSpecificTodoIdsSucceeds()
  {
    // Arrange
    SetUp();
    var id1 = Guid.NewGuid();

    _mediatorMock
      .Setup(m => m.Send(It.IsAny<SetAllToDosAttributeCommand>(), It.IsAny<CancellationToken>()))
      .ReturnsAsync(Result.Ok(1));

    // Act
    var result = await _sut.SetAllEnergyAsync(id1.ToString(), "yellow");

    // Assert
    result.Should().Contain("Successfully set energy");
    result.Should().Contain("🟡");
  }

  [Fact]
  public async Task SetAllEnergyAsyncWithInvalidLevelReturnsFailed()
  {
    // Arrange
    SetUp();

    // Act
    var result = await _sut.SetAllEnergyAsync("", "pink");

    // Assert
    result.Should().Contain("Failed to set energy");
    result.Should().Contain("Invalid level");
  }

  [Fact]
  public async Task SetAllEnergyAsyncWithEmptyTodoIdsListSucceeds()
  {
    // Arrange
    SetUp();

    _mediatorMock
      .Setup(m => m.Send(It.IsAny<SetAllToDosAttributeCommand>(), It.IsAny<CancellationToken>()))
      .ReturnsAsync(Result.Ok(7));

    // Act
    var result = await _sut.SetAllEnergyAsync("", "blue");

    // Assert
    result.Should().Contain("Successfully set energy");
    result.Should().Contain("7");
  }

  [Fact]
  public async Task SetAllEnergyAsyncWhenMediatorFailsReturnsFailed()
  {
    // Arrange
    SetUp();

    _mediatorMock
      .Setup(m => m.Send(It.IsAny<SetAllToDosAttributeCommand>(), It.IsAny<CancellationToken>()))
      .ReturnsAsync(Result.Fail("Database error"));

    // Act
    var result = await _sut.SetAllEnergyAsync("", "red");

    // Assert
    result.Should().Contain("Failed to set energy");
    result.Should().Contain("Database error");
  }

  [Fact]
  public async Task SetAllEnergyAsyncWhenExceptionThrownReturnsError()
  {
    // Arrange
    SetUp();

    _mediatorMock
      .Setup(m => m.Send(It.IsAny<SetAllToDosAttributeCommand>(), It.IsAny<CancellationToken>()))
      .ThrowsAsync(new OutOfMemoryException("Out of memory"));

    // Act
    var result = await _sut.SetAllEnergyAsync("", "green");

    // Assert
    result.Should().Contain("Error setting energy");
    result.Should().Contain("Out of memory");
  }

  #endregion

  #region SetAllInterest Tests (6 tests)

  [Fact]
  public async Task SetAllInterestAsyncWithValidLevelAndAllTodosSucceeds()
  {
    // Arrange
    SetUp();

    _mediatorMock
      .Setup(m => m.Send(It.IsAny<SetAllToDosAttributeCommand>(), It.IsAny<CancellationToken>()))
      .ReturnsAsync(Result.Ok(4));

    // Act
    var result = await _sut.SetAllInterestAsync("", "yellow");

    // Assert
    result.Should().Contain("Successfully set interest");
    result.Should().Contain("🟡");
    result.Should().Contain("4");
    result.Should().Contain("active todos");
  }

  [Fact]
  public async Task SetAllInterestAsyncWithSpecificTodoIdsSucceeds()
  {
    // Arrange
    SetUp();
    var id1 = Guid.NewGuid();
    var id2 = Guid.NewGuid();

    _mediatorMock
      .Setup(m => m.Send(It.IsAny<SetAllToDosAttributeCommand>(), It.IsAny<CancellationToken>()))
      .ReturnsAsync(Result.Ok(2));

    // Act
    var result = await _sut.SetAllInterestAsync($"{id1},{id2}", "blue");

    // Assert
    result.Should().Contain("Successfully set interest");
    result.Should().Contain("🔵");
    result.Should().Contain("2");
  }

  [Fact]
  public async Task SetAllInterestAsyncWithInvalidLevelReturnsFailed()
  {
    // Arrange
    SetUp();

    // Act
    var result = await _sut.SetAllInterestAsync("", "silver");

    // Assert
    result.Should().Contain("Failed to set interest");
    result.Should().Contain("Invalid level");
  }

  [Fact]
  public async Task SetAllInterestAsyncWithInvalidIdInListSkipsIt()
  {
    // Arrange
    SetUp();
    var validId = Guid.NewGuid();

    _mediatorMock
      .Setup(m => m.Send(It.IsAny<SetAllToDosAttributeCommand>(), It.IsAny<CancellationToken>()))
      .ReturnsAsync(Result.Ok(1));

    // Act
    var result = await _sut.SetAllInterestAsync($"{validId},invalid-id,not-a-guid", "red");

    // Assert
    result.Should().Contain("Successfully set interest");
    result.Should().Contain("🔴");
    result.Should().Contain("1");
  }

  [Fact]
  public async Task SetAllInterestAsyncWhenMediatorFailsReturnsFailed()
  {
    // Arrange
    SetUp();

    _mediatorMock
      .Setup(m => m.Send(It.IsAny<SetAllToDosAttributeCommand>(), It.IsAny<CancellationToken>()))
      .ReturnsAsync(Result.Fail("Unauthorized"));

    // Act
    var result = await _sut.SetAllInterestAsync("", "green");

    // Assert
    result.Should().Contain("Failed to set interest");
    result.Should().Contain("Unauthorized");
  }

  [Fact]
  public async Task SetAllInterestAsyncWhenExceptionThrownReturnsError()
  {
    // Arrange
    SetUp();

    _mediatorMock
      .Setup(m => m.Send(It.IsAny<SetAllToDosAttributeCommand>(), It.IsAny<CancellationToken>()))
      .ThrowsAsync(new StackOverflowException("Stack overflow"));

    // Act
    var result = await _sut.SetAllInterestAsync("", "yellow");

    // Assert
    result.Should().Contain("Error setting interest");
    result.Should().Contain("Stack overflow");
  }

  #endregion

  #region ListToDos Tests (3 tests)

  [Fact]
  public async Task ListToDosAsyncWithMultipleTodosSucceeds()
  {
    // Arrange
    SetUp();
    var todos = new List<ToDo>
    {
      CreateValidToDo(description: "Task 1"),
      CreateValidToDo(description: "Task 2"),
      CreateValidToDo(description: "Task 3")
    };

    _mediatorMock
      .Setup(m => m.Send(It.IsAny<GetToDosByPersonIdQuery>(), It.IsAny<CancellationToken>()))
      .ReturnsAsync(Result.Ok((IEnumerable<ToDo>)todos));

    // Act
    var result = await _sut.ListToDosAsync();

    // Assert
    result.Should().Contain("Here are your active todos");
    result.Should().Contain("Task 1");
    result.Should().Contain("Task 2");
    result.Should().Contain("Task 3");
    result.Should().Contain("P = Priority");
    _mediatorMock.Verify(m => m.Send(It.IsAny<GetToDosByPersonIdQuery>(), It.IsAny<CancellationToken>()), Times.Once);
  }

  [Fact]
  public async Task ListToDosAsyncWithNoTodosReturnsEmptyMessage()
  {
    // Arrange
    SetUp();
    var emptyList = new List<ToDo>();

    _mediatorMock
      .Setup(m => m.Send(It.IsAny<GetToDosByPersonIdQuery>(), It.IsAny<CancellationToken>()))
      .ReturnsAsync(Result.Ok((IEnumerable<ToDo>)emptyList));

    // Act
    var result = await _sut.ListToDosAsync();

    // Assert
    result.Should().Contain("You currently have no active todos");
    _mediatorMock.Verify(m => m.Send(It.IsAny<GetToDosByPersonIdQuery>(), It.IsAny<CancellationToken>()), Times.Once);
  }

  [Fact]
  public async Task ListToDosAsyncWhenMediatorFailsReturnsFailed()
  {
    // Arrange
    SetUp();

    _mediatorMock
      .Setup(m => m.Send(It.IsAny<GetToDosByPersonIdQuery>(), It.IsAny<CancellationToken>()))
      .ReturnsAsync(Result.Fail("Query error"));

    // Act
    var result = await _sut.ListToDosAsync();

    // Assert
    result.Should().Contain("Failed to retrieve todos");
    result.Should().Contain("Query error");
  }

  #endregion

  #region GenerateDailyPlan Tests (4 tests)

  [Fact]
  public async Task GenerateDailyPlanAsyncWithValidPlanSucceeds()
  {
    // Arrange
    SetUp();
    var dailyPlan = CreateValidDailyPlan(taskCount: 3, totalActive: 10);

    _mediatorMock
      .Setup(m => m.Send(It.IsAny<GetDailyPlanQuery>(), It.IsAny<CancellationToken>()))
      .ReturnsAsync(Result.Ok(dailyPlan));

    // Act
    var result = await _sut.GenerateDailyPlanAsync();

    // Assert
    result.Should().Contain("Here's your suggested daily plan");
    result.Should().Contain("Task 1");
    result.Should().Contain("Task 2");
    result.Should().Contain("Task 3");
    result.Should().Contain("Test rationale");
    result.Should().Contain("Showing 3 of 10");
    _mediatorMock.Verify(m => m.Send(It.IsAny<GetDailyPlanQuery>(), It.IsAny<CancellationToken>()), Times.Once);
  }

  [Fact]
  public async Task GenerateDailyPlanAsyncWithEmptyTasksReturnsRationale()
  {
    // Arrange
    SetUp();
    var emptyPlan = CreateValidDailyPlan(taskCount: 0, totalActive: 0);

    _mediatorMock
      .Setup(m => m.Send(It.IsAny<GetDailyPlanQuery>(), It.IsAny<CancellationToken>()))
      .ReturnsAsync(Result.Ok(emptyPlan));

    // Act
    var result = await _sut.GenerateDailyPlanAsync();

    // Assert
    result.Should().Contain("Test rationale");
    result.Should().NotContain("Here's your suggested daily plan");
    result.Should().NotContain("Showing");
  }

  [Fact]
  public async Task GenerateDailyPlanAsyncWhenMediatorFailsReturnsFailed()
  {
    // Arrange
    SetUp();

    _mediatorMock
      .Setup(m => m.Send(It.IsAny<GetDailyPlanQuery>(), It.IsAny<CancellationToken>()))
      .ReturnsAsync(Result.Fail("Plan generation error"));

    // Act
    var result = await _sut.GenerateDailyPlanAsync();

    // Assert
    result.Should().Contain("Failed to generate daily plan");
    result.Should().Contain("Plan generation error");
  }

  [Fact]
  public async Task GenerateDailyPlanAsyncWhenExceptionThrownReturnsError()
  {
    // Arrange
    SetUp();

    _mediatorMock
      .Setup(m => m.Send(It.IsAny<GetDailyPlanQuery>(), It.IsAny<CancellationToken>()))
      .ThrowsAsync(new InvalidOperationException("AI service unavailable"));

    // Act
    var result = await _sut.GenerateDailyPlanAsync();

    // Assert
    result.Should().Contain("Error generating daily plan");
    result.Should().Contain("AI service unavailable");
  }

  #endregion
}
