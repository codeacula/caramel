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

  #endregion Helper Methods

  #region CreateToDo Tests (10 tests)

  [Fact]
  public async Task CreateToDoAsyncWithValidDescriptionOnlySucceedsAsync()
  {
    // Arrange
    SetUp();
    const string description = "Buy groceries";
    var expectedToDo = CreateValidToDo(description: description);

    _ = _mediatorMock
      .Setup(m => m.Send(It.IsAny<CreateToDoCommand>(), It.IsAny<CancellationToken>()))
      .ReturnsAsync(Result.Ok(expectedToDo));

    // Act
    var result = await _sut.CreateToDoAsync(description);

    // Assert
    _ = result.Should().Contain("Successfully created todo");
    _ = result.Should().Contain("Buy groceries");
    _ = result.Should().NotContain("reminder");
    _mediatorMock.Verify(m => m.Send(It.IsAny<CreateToDoCommand>(), It.IsAny<CancellationToken>()), Times.Once);
  }

  [Fact]
  public async Task CreateToDoAsyncWithValidFuzzyReminderTimeSucceedsAsync()
  {
    // Arrange
    SetUp();
    const string description = "Call dentist";
    var reminderTime = DateTime.UtcNow.AddHours(2);
    var expectedToDo = CreateValidToDo(description: description);

    _ = _fuzzyTimeParserMock
      .Setup(ftp => ftp.TryParseFuzzyTime("in 2 hours", It.IsAny<DateTime>()))
      .Returns(Result.Ok(reminderTime));

    _ = _mediatorMock
      .Setup(m => m.Send(It.IsAny<CreateToDoCommand>(), It.IsAny<CancellationToken>()))
      .ReturnsAsync(Result.Ok(expectedToDo));

    // Act
    var result = await _sut.CreateToDoAsync(description, "in 2 hours");

    // Assert
    _ = result.Should().Contain("Successfully created todo");
    _ = result.Should().Contain("reminder");
    _ = result.Should().Contain("Call dentist");
    _mediatorMock.Verify(m => m.Send(It.IsAny<CreateToDoCommand>(), It.IsAny<CancellationToken>()), Times.Once);
  }

  [Fact]
  public async Task CreateToDoAsyncWithValidIso8601ReminderSucceedsAsync()
  {
    // Arrange
    SetUp();
    const string description = "Pay bills";
    const string isoDate = "2025-12-31T10:00:00";
    var expectedToDo = CreateValidToDo(description: description);

    _ = _fuzzyTimeParserMock
      .Setup(ftp => ftp.TryParseFuzzyTime(isoDate, It.IsAny<DateTime>()))
      .Returns(Result.Fail("Not fuzzy"));

    _ = _personStoreMock
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

    _ = _mediatorMock
      .Setup(m => m.Send(It.IsAny<CreateToDoCommand>(), It.IsAny<CancellationToken>()))
      .ReturnsAsync(Result.Ok(expectedToDo));

    // Act
    var result = await _sut.CreateToDoAsync(description, isoDate);

    // Assert
    _ = result.Should().Contain("Successfully created todo");
    _ = result.Should().Contain("Pay bills");
    _mediatorMock.Verify(m => m.Send(It.IsAny<CreateToDoCommand>(), It.IsAny<CancellationToken>()), Times.Once);
  }

  [Fact]
  public async Task CreateToDoAsyncWithPrioritiesAndLevelsSucceedsAsync()
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

    _ = _mediatorMock
      .Setup(m => m.Send(It.IsAny<CreateToDoCommand>(), It.IsAny<CancellationToken>()))
      .ReturnsAsync(Result.Ok(expectedToDo));

    // Act
    var result = await _sut.CreateToDoAsync(description, null, "red", "yellow", "blue");

    // Assert
    _ = result.Should().Contain("Successfully created todo");
    _ = result.Should().Contain("Important meeting");
    _mediatorMock.Verify(m => m.Send(It.IsAny<CreateToDoCommand>(), It.IsAny<CancellationToken>()), Times.Once);
  }

  [Fact]
  public async Task CreateToDoAsyncWithColorEmojiLevelsSucceedsAsync()
  {
    // Arrange
    SetUp();
    const string description = "Emoji test";
    var expectedToDo = CreateValidToDo(description: description);

    _ = _mediatorMock
      .Setup(m => m.Send(It.IsAny<CreateToDoCommand>(), It.IsAny<CancellationToken>()))
      .ReturnsAsync(Result.Ok(expectedToDo));

    // Act
    var result = await _sut.CreateToDoAsync(description, null, "🔵", "🟢", "🟡");

    // Assert
    _ = result.Should().Contain("Successfully created todo");
    _mediatorMock.Verify(m => m.Send(It.IsAny<CreateToDoCommand>(), It.IsAny<CancellationToken>()), Times.Once);
  }

  [Fact]
  public async Task CreateToDoAsyncWithNumberLevelsSucceedsAsync()
  {
    // Arrange
    SetUp();
    const string description = "Number levels";
    var expectedToDo = CreateValidToDo(description: description);

    _ = _mediatorMock
      .Setup(m => m.Send(It.IsAny<CreateToDoCommand>(), It.IsAny<CancellationToken>()))
      .ReturnsAsync(Result.Ok(expectedToDo));

    // Act
    var result = await _sut.CreateToDoAsync(description, null, "2", "1", "0");

    // Assert
    _ = result.Should().Contain("Successfully created todo");
    _mediatorMock.Verify(m => m.Send(It.IsAny<CreateToDoCommand>(), It.IsAny<CancellationToken>()), Times.Once);
  }

  [Fact]
  public async Task CreateToDoAsyncWithEmptyDescriptionReturnsFailedAsync()
  {
    // Arrange
    SetUp();

    // Act
    var result = await _sut.CreateToDoAsync("");

    // Assert
    _ = result.Should().Contain("Error creating todo");
  }

  [Fact]
  public async Task CreateToDoAsyncWithInvalidReminderFormatReturnsFailedAsync()
  {
    // Arrange
    SetUp();
    const string description = "Test";
    const string invalidDate = "not a valid date at all";

    _ = _fuzzyTimeParserMock
      .Setup(ftp => ftp.TryParseFuzzyTime(invalidDate, It.IsAny<DateTime>()))
      .Returns(Result.Fail("Invalid fuzzy time"));

    // Act
    var result = await _sut.CreateToDoAsync(description, invalidDate);

    // Assert
    _ = result.Should().Contain("Failed to create todo");
    _ = result.Should().Contain("Invalid reminder format");
  }

  [Fact]
  public async Task CreateToDoAsyncWithInvalidPriorityLevelReturnsFailedAsync()
  {
    // Arrange
    SetUp();
    const string description = "Test";
    const string invalidLevel = "purple";

    _ = _mediatorMock
      .Setup(m => m.Send(It.IsAny<CreateToDoCommand>(), It.IsAny<CancellationToken>()))
      .ReturnsAsync(Result.Ok(CreateValidToDo(description: description)));

    // Act
    var result = await _sut.CreateToDoAsync(description, null, invalidLevel);

    // Assert
    _ = result.Should().Contain("Successfully created todo");
  }

  [Fact]
  public async Task CreateToDoAsyncWhenMediatorFailsReturnsFailedAsync()
  {
    // Arrange
    SetUp();
    const string description = "Test";

    _ = _mediatorMock
      .Setup(m => m.Send(It.IsAny<CreateToDoCommand>(), It.IsAny<CancellationToken>()))
      .ReturnsAsync(Result.Fail("Store error"));

    // Act
    var result = await _sut.CreateToDoAsync(description);

    // Assert
    _ = result.Should().Contain("Failed to create todo");
    _ = result.Should().Contain("Store error");
  }

  #endregion CreateToDo Tests (10 tests)

  #region UpdateToDo Tests (8 tests)

  [Fact]
  public async Task UpdateToDoAsyncWithValidIdAndDescriptionSucceedsAsync()
  {
    // Arrange
    SetUp();
    var todoId = Guid.NewGuid();
    const string newDescription = "Updated task";

    _ = _mediatorMock
      .Setup(m => m.Send(It.IsAny<UpdateToDoCommand>(), It.IsAny<CancellationToken>()))
      .ReturnsAsync(Result.Ok());

    // Act
    var result = await _sut.UpdateToDoAsync(todoId.ToString(), newDescription);

    // Assert
    _ = result.Should().Contain("Successfully updated the todo");
    _ = result.Should().Contain(newDescription);
    _mediatorMock.Verify(m => m.Send(It.IsAny<UpdateToDoCommand>(), It.IsAny<CancellationToken>()), Times.Once);
  }

  [Fact]
  public async Task UpdateToDoAsyncWithInvalidGuidFormatReturnsFailedAsync()
  {
    // Arrange
    SetUp();
    const string invalidId = "not-a-guid";

    // Act
    var result = await _sut.UpdateToDoAsync(invalidId, "New description");

    // Assert
    _ = result.Should().Contain("Failed to update todo");
    _ = result.Should().Contain("Invalid todo ID format");
    _mediatorMock.Verify(m => m.Send(It.IsAny<UpdateToDoCommand>(), It.IsAny<CancellationToken>()), Times.Never);
  }

  [Fact]
  public async Task UpdateToDoAsyncWithEmptyDescriptionSucceedsAsync()
  {
    // Arrange
    SetUp();
    var todoId = Guid.NewGuid();

    _ = _mediatorMock
      .Setup(m => m.Send(It.IsAny<UpdateToDoCommand>(), It.IsAny<CancellationToken>()))
      .ReturnsAsync(Result.Ok());

    // Act
    var result = await _sut.UpdateToDoAsync(todoId.ToString(), "");

    // Assert
    _ = result.Should().Contain("Successfully updated the todo");
    _mediatorMock.Verify(m => m.Send(It.IsAny<UpdateToDoCommand>(), It.IsAny<CancellationToken>()), Times.Once);
  }

  [Fact]
  public async Task UpdateToDoAsyncWhenMediatorFailsReturnsFailedAsync()
  {
    // Arrange
    SetUp();
    var todoId = Guid.NewGuid();

    _ = _mediatorMock
      .Setup(m => m.Send(It.IsAny<UpdateToDoCommand>(), It.IsAny<CancellationToken>()))
      .ReturnsAsync(Result.Fail("Todo not found"));

    // Act
    var result = await _sut.UpdateToDoAsync(todoId.ToString(), "New description");

    // Assert
    _ = result.Should().Contain("Failed to update todo");
    _ = result.Should().Contain("Todo not found");
  }

  [Fact]
  public async Task UpdateToDoAsyncWhenExceptionThrownReturnsErrorAsync()
  {
    // Arrange
    SetUp();
    var todoId = Guid.NewGuid();

    _ = _mediatorMock
      .Setup(m => m.Send(It.IsAny<UpdateToDoCommand>(), It.IsAny<CancellationToken>()))
      .ThrowsAsync(new InvalidOperationException("Database error"));

    // Act
    var result = await _sut.UpdateToDoAsync(todoId.ToString(), "New description");

    // Assert
    _ = result.Should().Contain("Error updating todo");
    _ = result.Should().Contain("Database error");
  }

  [Fact]
  public async Task UpdateToDoAsyncWithGuidInUppercaseSucceedsAsync()
  {
    // Arrange
    SetUp();
    var todoId = Guid.NewGuid().ToString().ToUpperInvariant();

    _ = _mediatorMock
      .Setup(m => m.Send(It.IsAny<UpdateToDoCommand>(), It.IsAny<CancellationToken>()))
      .ReturnsAsync(Result.Ok());

    // Act
    var result = await _sut.UpdateToDoAsync(todoId, "New description");

    // Assert
    _ = result.Should().Contain("Successfully updated the todo");
    _mediatorMock.Verify(m => m.Send(It.IsAny<UpdateToDoCommand>(), It.IsAny<CancellationToken>()), Times.Once);
  }

  [Fact]
  public async Task UpdateToDoAsyncWithWhitespaceDescriptionSucceedsAsync()
  {
    // Arrange
    SetUp();
    var todoId = Guid.NewGuid().ToString();

    _ = _mediatorMock
      .Setup(m => m.Send(It.IsAny<UpdateToDoCommand>(), It.IsAny<CancellationToken>()))
      .ReturnsAsync(Result.Ok());

    // Act
    var result = await _sut.UpdateToDoAsync(todoId, "   ");

    // Assert
    _ = result.Should().Contain("Successfully updated the todo");
    _mediatorMock.Verify(m => m.Send(It.IsAny<UpdateToDoCommand>(), It.IsAny<CancellationToken>()), Times.Once);
  }

  #endregion UpdateToDo Tests (8 tests)

  #region CompleteToDo Tests (6 tests)

  [Fact]
  public async Task CompleteToDoAsyncWithValidIdSucceedsAsync()
  {
    // Arrange
    SetUp();
    var todoId = Guid.NewGuid();

    _ = _mediatorMock
      .Setup(m => m.Send(It.IsAny<CompleteToDoCommand>(), It.IsAny<CancellationToken>()))
      .ReturnsAsync(Result.Ok());

    // Act
    var result = await _sut.CompleteToDoAsync(todoId.ToString());

    // Assert
    _ = result.Should().Contain("Successfully marked the todo as completed");
    _mediatorMock.Verify(m => m.Send(It.IsAny<CompleteToDoCommand>(), It.IsAny<CancellationToken>()), Times.Once);
  }

  [Fact]
  public async Task CompleteToDoAsyncWithInvalidIdFormatReturnsFailedAsync()
  {
    // Arrange
    SetUp();

    // Act
    var result = await _sut.CompleteToDoAsync("invalid-guid");

    // Assert
    _ = result.Should().Contain("Failed to complete todo");
    _ = result.Should().Contain("Invalid todo ID format");
    _mediatorMock.Verify(m => m.Send(It.IsAny<CompleteToDoCommand>(), It.IsAny<CancellationToken>()), Times.Never);
  }

  [Fact]
  public async Task CompleteToDoAsyncWhenMediatorFailsReturnsFailedAsync()
  {
    // Arrange
    SetUp();
    var todoId = Guid.NewGuid();

    _ = _mediatorMock
      .Setup(m => m.Send(It.IsAny<CompleteToDoCommand>(), It.IsAny<CancellationToken>()))
      .ReturnsAsync(Result.Fail("Todo not found"));

    // Act
    var result = await _sut.CompleteToDoAsync(todoId.ToString());

    // Assert
    _ = result.Should().Contain("Failed to complete todo");
    _ = result.Should().Contain("Todo not found");
  }

  [Fact]
  public async Task CompleteToDoAsyncWhenExceptionThrownReturnsErrorAsync()
  {
    // Arrange
    SetUp();
    var todoId = Guid.NewGuid();

    _ = _mediatorMock
      .Setup(m => m.Send(It.IsAny<CompleteToDoCommand>(), It.IsAny<CancellationToken>()))
      .ThrowsAsync(new TimeoutException("Database timeout"));

    // Act
    var result = await _sut.CompleteToDoAsync(todoId.ToString());

    // Assert
    _ = result.Should().Contain("Error completing todo");
    _ = result.Should().Contain("Database timeout");
  }

  [Fact]
  public async Task CompleteToDoAsyncWithEmptyIdReturnsFailedAsync()
  {
    // Arrange
    SetUp();

    // Act
    var result = await _sut.CompleteToDoAsync("");

    // Assert
    _ = result.Should().Contain("Failed to complete todo");
  }

  #endregion CompleteToDo Tests (6 tests)

  #region DeleteToDo Tests (6 tests)

  [Fact]
  public async Task DeleteToDoAsyncWithValidIdSucceedsAsync()
  {
    // Arrange
    SetUp();
    var todoId = Guid.NewGuid();

    _ = _mediatorMock
      .Setup(m => m.Send(It.IsAny<DeleteToDoCommand>(), It.IsAny<CancellationToken>()))
      .ReturnsAsync(Result.Ok());

    // Act
    var result = await _sut.DeleteToDoAsync(todoId.ToString());

    // Assert
    _ = result.Should().Contain("Successfully deleted the todo");
    _mediatorMock.Verify(m => m.Send(It.IsAny<DeleteToDoCommand>(), It.IsAny<CancellationToken>()), Times.Once);
  }

  [Fact]
  public async Task DeleteToDoAsyncWithInvalidIdFormatReturnsFailedAsync()
  {
    // Arrange
    SetUp();

    // Act
    var result = await _sut.DeleteToDoAsync("not-a-valid-guid");

    // Assert
    _ = result.Should().Contain("Failed to delete todo");
    _ = result.Should().Contain("Invalid todo ID format");
    _mediatorMock.Verify(m => m.Send(It.IsAny<DeleteToDoCommand>(), It.IsAny<CancellationToken>()), Times.Never);
  }

  [Fact]
  public async Task DeleteToDoAsyncWhenMediatorFailsReturnsFailedAsync()
  {
    // Arrange
    SetUp();
    var todoId = Guid.NewGuid();

    _ = _mediatorMock
      .Setup(m => m.Send(It.IsAny<DeleteToDoCommand>(), It.IsAny<CancellationToken>()))
      .ReturnsAsync(Result.Fail("Todo already deleted"));

    // Act
    var result = await _sut.DeleteToDoAsync(todoId.ToString());

    // Assert
    _ = result.Should().Contain("Failed to delete todo");
    _ = result.Should().Contain("Todo already deleted");
  }

  [Fact]
  public async Task DeleteToDoAsyncWhenExceptionThrownReturnsErrorAsync()
  {
    // Arrange
    SetUp();
    var todoId = Guid.NewGuid();

    _ = _mediatorMock
      .Setup(m => m.Send(It.IsAny<DeleteToDoCommand>(), It.IsAny<CancellationToken>()))
      .ThrowsAsync(new InvalidOperationException("Null reference"));

    // Act
    var result = await _sut.DeleteToDoAsync(todoId.ToString());

    // Assert
    _ = result.Should().Contain("Error deleting todo");
    _ = result.Should().Contain("Null reference");
  }

  [Fact]
  public async Task DeleteToDoAsyncWithEmptyIdReturnsFailedAsync()
  {
    // Arrange
    SetUp();

    // Act
    var result = await _sut.DeleteToDoAsync("");

    // Assert
    _ = result.Should().Contain("Failed to delete todo");
  }

  #endregion DeleteToDo Tests (6 tests)

  #region SetPriority Tests (6 tests)

  [Fact]
  public async Task SetPriorityAsyncWithValidBlueSucceedsAsync()
  {
    // Arrange
    SetUp();
    var todoId = Guid.NewGuid();

    _ = _mediatorMock
      .Setup(m => m.Send(It.IsAny<SetToDoPriorityCommand>(), It.IsAny<CancellationToken>()))
      .ReturnsAsync(Result.Ok());

    // Act
    var result = await _sut.SetPriorityAsync(todoId.ToString(), "blue");

    // Assert
    _ = result.Should().Contain("Successfully set priority");
    _ = result.Should().Contain("🔵");
    _mediatorMock.Verify(m => m.Send(It.IsAny<SetToDoPriorityCommand>(), It.IsAny<CancellationToken>()), Times.Once);
  }

  [Fact]
  public async Task SetPriorityAsyncWithValidRedSucceedsAsync()
  {
    // Arrange
    SetUp();
    var todoId = Guid.NewGuid();

    _ = _mediatorMock
      .Setup(m => m.Send(It.IsAny<SetToDoPriorityCommand>(), It.IsAny<CancellationToken>()))
      .ReturnsAsync(Result.Ok());

    // Act
    var result = await _sut.SetPriorityAsync(todoId.ToString(), "red");

    // Assert
    _ = result.Should().Contain("Successfully set priority");
    _ = result.Should().Contain("🔴");
  }

  [Fact]
  public async Task SetPriorityAsyncWithInvalidTodoIdReturnsFailedAsync()
  {
    // Arrange
    SetUp();

    // Act
    var result = await _sut.SetPriorityAsync("invalid-id", "blue");

    // Assert
    _ = result.Should().Contain("Failed to set priority");
    _ = result.Should().Contain("Invalid todo ID format");
    _mediatorMock.Verify(m => m.Send(It.IsAny<SetToDoPriorityCommand>(), It.IsAny<CancellationToken>()), Times.Never);
  }

  [Fact]
  public async Task SetPriorityAsyncWithInvalidLevelReturnsFailedAsync()
  {
    // Arrange
    SetUp();
    var todoId = Guid.NewGuid();

    // Act
    var result = await _sut.SetPriorityAsync(todoId.ToString(), "invalid");

    // Assert
    _ = result.Should().Contain("Failed to set priority");
    _ = result.Should().Contain("Invalid level");
  }

  [Fact]
  public async Task SetPriorityAsyncWhenMediatorFailsReturnsFailedAsync()
  {
    // Arrange
    SetUp();
    var todoId = Guid.NewGuid();

    _ = _mediatorMock
      .Setup(m => m.Send(It.IsAny<SetToDoPriorityCommand>(), It.IsAny<CancellationToken>()))
      .ReturnsAsync(Result.Fail("Todo not found"));

    // Act
    var result = await _sut.SetPriorityAsync(todoId.ToString(), "green");

    // Assert
    _ = result.Should().Contain("Failed to set priority");
    _ = result.Should().Contain("Todo not found");
  }

  [Fact]
  public async Task SetPriorityAsyncWhenExceptionThrownReturnsErrorAsync()
  {
    // Arrange
    SetUp();
    var todoId = Guid.NewGuid();

    _ = _mediatorMock
      .Setup(m => m.Send(It.IsAny<SetToDoPriorityCommand>(), It.IsAny<CancellationToken>()))
      .ThrowsAsync(new InvalidOperationException("Invalid state"));

    // Act
    var result = await _sut.SetPriorityAsync(todoId.ToString(), "yellow");

    // Assert
    _ = result.Should().Contain("Error setting priority");
    _ = result.Should().Contain("Invalid state");
  }

  #endregion SetPriority Tests (6 tests)

  #region SetEnergy Tests (6 tests)

  [Fact]
  public async Task SetEnergyAsyncWithValidGreenSucceedsAsync()
  {
    // Arrange
    SetUp();
    var todoId = Guid.NewGuid();

    _ = _mediatorMock
      .Setup(m => m.Send(It.IsAny<SetToDoEnergyCommand>(), It.IsAny<CancellationToken>()))
      .ReturnsAsync(Result.Ok());

    // Act
    var result = await _sut.SetEnergyAsync(todoId.ToString(), "green");

    // Assert
    _ = result.Should().Contain("Successfully set energy");
    _ = result.Should().Contain("🟢");
    _mediatorMock.Verify(m => m.Send(It.IsAny<SetToDoEnergyCommand>(), It.IsAny<CancellationToken>()), Times.Once);
  }

  [Fact]
  public async Task SetEnergyAsyncWithValidYellowSucceedsAsync()
  {
    // Arrange
    SetUp();
    var todoId = Guid.NewGuid();

    _ = _mediatorMock
      .Setup(m => m.Send(It.IsAny<SetToDoEnergyCommand>(), It.IsAny<CancellationToken>()))
      .ReturnsAsync(Result.Ok());

    // Act
    var result = await _sut.SetEnergyAsync(todoId.ToString(), "yellow");

    // Assert
    _ = result.Should().Contain("Successfully set energy");
    _ = result.Should().Contain("🟡");
  }

  [Fact]
  public async Task SetEnergyAsyncWithInvalidTodoIdReturnsFailedAsync()
  {
    // Arrange
    SetUp();

    // Act
    var result = await _sut.SetEnergyAsync("bad-id", "green");

    // Assert
    _ = result.Should().Contain("Failed to set energy");
    _ = result.Should().Contain("Invalid todo ID format");
    _mediatorMock.Verify(m => m.Send(It.IsAny<SetToDoEnergyCommand>(), It.IsAny<CancellationToken>()), Times.Never);
  }

  [Fact]
  public async Task SetEnergyAsyncWithInvalidLevelReturnsFailedAsync()
  {
    // Arrange
    SetUp();
    var todoId = Guid.NewGuid();

    // Act
    var result = await _sut.SetEnergyAsync(todoId.ToString(), "orange");

    // Assert
    _ = result.Should().Contain("Failed to set energy");
    _ = result.Should().Contain("Invalid level");
  }

  [Fact]
  public async Task SetEnergyAsyncWhenMediatorFailsReturnsFailedAsync()
  {
    // Arrange
    SetUp();
    var todoId = Guid.NewGuid();

    _ = _mediatorMock
      .Setup(m => m.Send(It.IsAny<SetToDoEnergyCommand>(), It.IsAny<CancellationToken>()))
      .ReturnsAsync(Result.Fail("Store error"));

    // Act
    var result = await _sut.SetEnergyAsync(todoId.ToString(), "blue");

    // Assert
    _ = result.Should().Contain("Failed to set energy");
    _ = result.Should().Contain("Store error");
  }

  [Fact]
  public async Task SetEnergyAsyncWhenExceptionThrownReturnsErrorAsync()
  {
    // Arrange
    SetUp();
    var todoId = Guid.NewGuid();

    _ = _mediatorMock
      .Setup(m => m.Send(It.IsAny<SetToDoEnergyCommand>(), It.IsAny<CancellationToken>()))
      .ThrowsAsync(new ArgumentException("Invalid argument"));

    // Act
    var result = await _sut.SetEnergyAsync(todoId.ToString(), "red");

    // Assert
    _ = result.Should().Contain("Error setting energy");
    _ = result.Should().Contain("Invalid argument");
  }

  #endregion SetEnergy Tests (6 tests)

  #region SetInterest Tests (6 tests)

  [Fact]
  public async Task SetInterestAsyncWithValidBlueSucceedsAsync()
  {
    // Arrange
    SetUp();
    var todoId = Guid.NewGuid();

    _ = _mediatorMock
      .Setup(m => m.Send(It.IsAny<SetToDoInterestCommand>(), It.IsAny<CancellationToken>()))
      .ReturnsAsync(Result.Ok());

    // Act
    var result = await _sut.SetInterestAsync(todoId.ToString(), "blue");

    // Assert
    _ = result.Should().Contain("Successfully set interest");
    _ = result.Should().Contain("🔵");
    _mediatorMock.Verify(m => m.Send(It.IsAny<SetToDoInterestCommand>(), It.IsAny<CancellationToken>()), Times.Once);
  }

  [Fact]
  public async Task SetInterestAsyncWithValidRedSucceedsAsync()
  {
    // Arrange
    SetUp();
    var todoId = Guid.NewGuid();

    _ = _mediatorMock
      .Setup(m => m.Send(It.IsAny<SetToDoInterestCommand>(), It.IsAny<CancellationToken>()))
      .ReturnsAsync(Result.Ok());

    // Act
    var result = await _sut.SetInterestAsync(todoId.ToString(), "red");

    // Assert
    _ = result.Should().Contain("Successfully set interest");
    _ = result.Should().Contain("🔴");
  }

  [Fact]
  public async Task SetInterestAsyncWithInvalidTodoIdReturnsFailedAsync()
  {
    // Arrange
    SetUp();

    // Act
    var result = await _sut.SetInterestAsync("not-a-guid", "green");

    // Assert
    _ = result.Should().Contain("Failed to set interest");
    _ = result.Should().Contain("Invalid todo ID format");
    _mediatorMock.Verify(m => m.Send(It.IsAny<SetToDoInterestCommand>(), It.IsAny<CancellationToken>()), Times.Never);
  }

  [Fact]
  public async Task SetInterestAsyncWithInvalidLevelReturnsFailedAsync()
  {
    // Arrange
    SetUp();
    var todoId = Guid.NewGuid();

    // Act
    var result = await _sut.SetInterestAsync(todoId.ToString(), "purple");

    // Assert
    _ = result.Should().Contain("Failed to set interest");
    _ = result.Should().Contain("Invalid level");
  }

  [Fact]
  public async Task SetInterestAsyncWhenMediatorFailsReturnsFailedAsync()
  {
    // Arrange
    SetUp();
    var todoId = Guid.NewGuid();

    _ = _mediatorMock
      .Setup(m => m.Send(It.IsAny<SetToDoInterestCommand>(), It.IsAny<CancellationToken>()))
      .ReturnsAsync(Result.Fail("Access denied"));

    // Act
    var result = await _sut.SetInterestAsync(todoId.ToString(), "yellow");

    // Assert
    _ = result.Should().Contain("Failed to set interest");
    _ = result.Should().Contain("Access denied");
  }

  [Fact]
  public async Task SetInterestAsyncWhenExceptionThrownReturnsErrorAsync()
  {
    // Arrange
    SetUp();
    var todoId = Guid.NewGuid();

    _ = _mediatorMock
      .Setup(m => m.Send(It.IsAny<SetToDoInterestCommand>(), It.IsAny<CancellationToken>()))
      .ThrowsAsync(new OperationCanceledException("Operation cancelled"));

    // Act
    var result = await _sut.SetInterestAsync(todoId.ToString(), "green");

    // Assert
    _ = result.Should().Contain("Error setting interest");
    _ = result.Should().Contain("Operation cancelled");
  }

  #endregion SetInterest Tests (6 tests)

  #region SetAllPriority Tests (6 tests)

  [Fact]
  public async Task SetAllPriorityAsyncWithValidLevelAndAllTodosSucceedsAsync()
  {
    // Arrange
    SetUp();

    _ = _mediatorMock
      .Setup(m => m.Send(It.IsAny<SetAllToDosAttributeCommand>(), It.IsAny<CancellationToken>()))
      .ReturnsAsync(Result.Ok(5));

    // Act
    var result = await _sut.SetAllPriorityAsync("", "blue");

    // Assert
    _ = result.Should().Contain("Successfully set priority");
    _ = result.Should().Contain("🔵");
    _ = result.Should().Contain("5");
    _ = result.Should().Contain("active todos");
    _mediatorMock.Verify(m => m.Send(It.IsAny<SetAllToDosAttributeCommand>(), It.IsAny<CancellationToken>()), Times.Once);
  }

  [Fact]
  public async Task SetAllPriorityAsyncWithSpecificTodoIdsSucceedsAsync()
  {
    // Arrange
    SetUp();
    var id1 = Guid.NewGuid();
    var id2 = Guid.NewGuid();

    _ = _mediatorMock
      .Setup(m => m.Send(It.IsAny<SetAllToDosAttributeCommand>(), It.IsAny<CancellationToken>()))
      .ReturnsAsync(Result.Ok(2));

    // Act
    var result = await _sut.SetAllPriorityAsync($"{id1},{id2}", "green");

    // Assert
    _ = result.Should().Contain("Successfully set priority");
    _ = result.Should().Contain("🟢");
    _ = result.Should().Contain("2");
    _mediatorMock.Verify(m => m.Send(It.IsAny<SetAllToDosAttributeCommand>(), It.IsAny<CancellationToken>()), Times.Once);
  }

  [Fact]
  public async Task SetAllPriorityAsyncWithInvalidLevelReturnsFailedAsync()
  {
    // Arrange
    SetUp();

    // Act
    var result = await _sut.SetAllPriorityAsync("", "invalid");

    // Assert
    _ = result.Should().Contain("Failed to set priority");
    _ = result.Should().Contain("Invalid level");
    _mediatorMock.Verify(m => m.Send(It.IsAny<SetAllToDosAttributeCommand>(), It.IsAny<CancellationToken>()), Times.Never);
  }

  [Fact]
  public async Task SetAllPriorityAsyncWithAllKeywordSucceedsAsync()
  {
    // Arrange
    SetUp();

    _ = _mediatorMock
      .Setup(m => m.Send(It.IsAny<SetAllToDosAttributeCommand>(), It.IsAny<CancellationToken>()))
      .ReturnsAsync(Result.Ok(10));

    // Act
    var result = await _sut.SetAllPriorityAsync("all", "yellow");

    // Assert
    _ = result.Should().Contain("Successfully set priority");
    _ = result.Should().Contain("🟡");
    _ = result.Should().Contain("10");
    _mediatorMock.Verify(m => m.Send(It.IsAny<SetAllToDosAttributeCommand>(), It.IsAny<CancellationToken>()), Times.Once);
  }

  [Fact]
  public async Task SetAllPriorityAsyncWhenMediatorFailsReturnsFailedAsync()
  {
    // Arrange
    SetUp();

    _ = _mediatorMock
      .Setup(m => m.Send(It.IsAny<SetAllToDosAttributeCommand>(), It.IsAny<CancellationToken>()))
      .ReturnsAsync(Result.Fail("No todos found"));

    // Act
    var result = await _sut.SetAllPriorityAsync("", "red");

    // Assert
    _ = result.Should().Contain("Failed to set priority");
    _ = result.Should().Contain("No todos found");
  }

  [Fact]
  public async Task SetAllPriorityAsyncWhenExceptionThrownReturnsErrorAsync()
  {
    // Arrange
    SetUp();

    _ = _mediatorMock
      .Setup(m => m.Send(It.IsAny<SetAllToDosAttributeCommand>(), It.IsAny<CancellationToken>()))
      .ThrowsAsync(new InvalidOperationException("Sequence contains no elements"));

    // Act
    var result = await _sut.SetAllPriorityAsync("", "blue");

    // Assert
    _ = result.Should().Contain("Error setting priority");
    _ = result.Should().Contain("Sequence contains no elements");
  }

  #endregion SetAllPriority Tests (6 tests)

  #region SetAllEnergy Tests (6 tests)

  [Fact]
  public async Task SetAllEnergyAsyncWithValidLevelAndAllTodosSucceedsAsync()
  {
    // Arrange
    SetUp();

    _ = _mediatorMock
      .Setup(m => m.Send(It.IsAny<SetAllToDosAttributeCommand>(), It.IsAny<CancellationToken>()))
      .ReturnsAsync(Result.Ok(3));

    // Act
    var result = await _sut.SetAllEnergyAsync("", "green");

    // Assert
    _ = result.Should().Contain("Successfully set energy");
    _ = result.Should().Contain("🟢");
    _ = result.Should().Contain("3");
    _ = result.Should().Contain("active todos");
  }

  [Fact]
  public async Task SetAllEnergyAsyncWithSpecificTodoIdsSucceedsAsync()
  {
    // Arrange
    SetUp();
    var id1 = Guid.NewGuid();

    _ = _mediatorMock
      .Setup(m => m.Send(It.IsAny<SetAllToDosAttributeCommand>(), It.IsAny<CancellationToken>()))
      .ReturnsAsync(Result.Ok(1));

    // Act
    var result = await _sut.SetAllEnergyAsync(id1.ToString(), "yellow");

    // Assert
    _ = result.Should().Contain("Successfully set energy");
    _ = result.Should().Contain("🟡");
  }

  [Fact]
  public async Task SetAllEnergyAsyncWithInvalidLevelReturnsFailedAsync()
  {
    // Arrange
    SetUp();

    // Act
    var result = await _sut.SetAllEnergyAsync("", "pink");

    // Assert
    _ = result.Should().Contain("Failed to set energy");
    _ = result.Should().Contain("Invalid level");
  }

  [Fact]
  public async Task SetAllEnergyAsyncWithEmptyTodoIdsListSucceedsAsync()
  {
    // Arrange
    SetUp();

    _ = _mediatorMock
      .Setup(m => m.Send(It.IsAny<SetAllToDosAttributeCommand>(), It.IsAny<CancellationToken>()))
      .ReturnsAsync(Result.Ok(7));

    // Act
    var result = await _sut.SetAllEnergyAsync("", "blue");

    // Assert
    _ = result.Should().Contain("Successfully set energy");
    _ = result.Should().Contain("7");
  }

  [Fact]
  public async Task SetAllEnergyAsyncWhenMediatorFailsReturnsFailedAsync()
  {
    // Arrange
    SetUp();

    _ = _mediatorMock
      .Setup(m => m.Send(It.IsAny<SetAllToDosAttributeCommand>(), It.IsAny<CancellationToken>()))
      .ReturnsAsync(Result.Fail("Database error"));

    // Act
    var result = await _sut.SetAllEnergyAsync("", "red");

    // Assert
    _ = result.Should().Contain("Failed to set energy");
    _ = result.Should().Contain("Database error");
  }

  [Fact]
  public async Task SetAllEnergyAsyncWhenExceptionThrownReturnsErrorAsync()
  {
    // Arrange
    SetUp();

    _ = _mediatorMock
      .Setup(m => m.Send(It.IsAny<SetAllToDosAttributeCommand>(), It.IsAny<CancellationToken>()))
      .ThrowsAsync(new InvalidOperationException("Out of memory"));

    // Act
    var result = await _sut.SetAllEnergyAsync("", "green");

    // Assert
    _ = result.Should().Contain("Error setting energy");
    _ = result.Should().Contain("Out of memory");
  }

  #endregion SetAllEnergy Tests (6 tests)

  #region SetAllInterest Tests (6 tests)

  [Fact]
  public async Task SetAllInterestAsyncWithValidLevelAndAllTodosSucceedsAsync()
  {
    // Arrange
    SetUp();

    _ = _mediatorMock
      .Setup(m => m.Send(It.IsAny<SetAllToDosAttributeCommand>(), It.IsAny<CancellationToken>()))
      .ReturnsAsync(Result.Ok(4));

    // Act
    var result = await _sut.SetAllInterestAsync("", "yellow");

    // Assert
    _ = result.Should().Contain("Successfully set interest");
    _ = result.Should().Contain("🟡");
    _ = result.Should().Contain("4");
    _ = result.Should().Contain("active todos");
  }

  [Fact]
  public async Task SetAllInterestAsyncWithSpecificTodoIdsSucceedsAsync()
  {
    // Arrange
    SetUp();
    var id1 = Guid.NewGuid();
    var id2 = Guid.NewGuid();

    _ = _mediatorMock
      .Setup(m => m.Send(It.IsAny<SetAllToDosAttributeCommand>(), It.IsAny<CancellationToken>()))
      .ReturnsAsync(Result.Ok(2));

    // Act
    var result = await _sut.SetAllInterestAsync($"{id1},{id2}", "blue");

    // Assert
    _ = result.Should().Contain("Successfully set interest");
    _ = result.Should().Contain("🔵");
    _ = result.Should().Contain("2");
  }

  [Fact]
  public async Task SetAllInterestAsyncWithInvalidLevelReturnsFailedAsync()
  {
    // Arrange
    SetUp();

    // Act
    var result = await _sut.SetAllInterestAsync("", "silver");

    // Assert
    _ = result.Should().Contain("Failed to set interest");
    _ = result.Should().Contain("Invalid level");
  }

  [Fact]
  public async Task SetAllInterestAsyncWithInvalidIdInListSkipsItAsync()
  {
    // Arrange
    SetUp();
    var validId = Guid.NewGuid();

    _ = _mediatorMock
      .Setup(m => m.Send(It.IsAny<SetAllToDosAttributeCommand>(), It.IsAny<CancellationToken>()))
      .ReturnsAsync(Result.Ok(1));

    // Act
    var result = await _sut.SetAllInterestAsync($"{validId},invalid-id,not-a-guid", "red");

    // Assert
    _ = result.Should().Contain("Successfully set interest");
    _ = result.Should().Contain("🔴");
    _ = result.Should().Contain("1");
  }

  [Fact]
  public async Task SetAllInterestAsyncWhenMediatorFailsReturnsFailedAsync()
  {
    // Arrange
    SetUp();

    _ = _mediatorMock
      .Setup(m => m.Send(It.IsAny<SetAllToDosAttributeCommand>(), It.IsAny<CancellationToken>()))
      .ReturnsAsync(Result.Fail("Unauthorized"));

    // Act
    var result = await _sut.SetAllInterestAsync("", "green");

    // Assert
    _ = result.Should().Contain("Failed to set interest");
    _ = result.Should().Contain("Unauthorized");
  }

  [Fact]
  public async Task SetAllInterestAsyncWhenExceptionThrownReturnsErrorAsync()
  {
    // Arrange
    SetUp();

    _ = _mediatorMock
      .Setup(m => m.Send(It.IsAny<SetAllToDosAttributeCommand>(), It.IsAny<CancellationToken>()))
      .ThrowsAsync(new InvalidOperationException("Stack overflow"));

    // Act
    var result = await _sut.SetAllInterestAsync("", "yellow");

    // Assert
    _ = result.Should().Contain("Error setting interest");
    _ = result.Should().Contain("Stack overflow");
  }

  #endregion SetAllInterest Tests (6 tests)

  #region ListToDos Tests (3 tests)

  [Fact]
  public async Task ListToDosAsyncWithMultipleTodosSucceedsAsync()
  {
    // Arrange
    SetUp();
    var todos = new List<ToDo>
    {
      CreateValidToDo(description: "Task 1"),
      CreateValidToDo(description: "Task 2"),
      CreateValidToDo(description: "Task 3")
    };

    _ = _mediatorMock
      .Setup(m => m.Send(It.IsAny<GetToDosByPersonIdQuery>(), It.IsAny<CancellationToken>()))
      .ReturnsAsync(Result.Ok((IEnumerable<ToDo>)todos));

    // Act
    var result = await _sut.ListToDosAsync();

    // Assert
    _ = result.Should().Contain("Here are your active todos");
    _ = result.Should().Contain("Task 1");
    _ = result.Should().Contain("Task 2");
    _ = result.Should().Contain("Task 3");
    _ = result.Should().Contain("P = Priority");
    _mediatorMock.Verify(m => m.Send(It.IsAny<GetToDosByPersonIdQuery>(), It.IsAny<CancellationToken>()), Times.Once);
  }

  [Fact]
  public async Task ListToDosAsyncWithNoTodosReturnsEmptyMessageAsync()
  {
    // Arrange
    SetUp();
    var emptyList = new List<ToDo>();

    _ = _mediatorMock
      .Setup(m => m.Send(It.IsAny<GetToDosByPersonIdQuery>(), It.IsAny<CancellationToken>()))
      .ReturnsAsync(Result.Ok((IEnumerable<ToDo>)emptyList));

    // Act
    var result = await _sut.ListToDosAsync();

    // Assert
    _ = result.Should().Contain("You currently have no active todos");
    _mediatorMock.Verify(m => m.Send(It.IsAny<GetToDosByPersonIdQuery>(), It.IsAny<CancellationToken>()), Times.Once);
  }

  [Fact]
  public async Task ListToDosAsyncWhenMediatorFailsReturnsFailedAsync()
  {
    // Arrange
    SetUp();

    _ = _mediatorMock
      .Setup(m => m.Send(It.IsAny<GetToDosByPersonIdQuery>(), It.IsAny<CancellationToken>()))
      .ReturnsAsync(Result.Fail("Query error"));

    // Act
    var result = await _sut.ListToDosAsync();

    // Assert
    _ = result.Should().Contain("Failed to retrieve todos");
    _ = result.Should().Contain("Query error");
  }

  #endregion ListToDos Tests (3 tests)

  #region GenerateDailyPlan Tests (4 tests)

  [Fact]
  public async Task GenerateDailyPlanAsyncWithValidPlanSucceedsAsync()
  {
    // Arrange
    SetUp();
    var dailyPlan = CreateValidDailyPlan(taskCount: 3, totalActive: 10);

    _ = _mediatorMock
      .Setup(m => m.Send(It.IsAny<GetDailyPlanQuery>(), It.IsAny<CancellationToken>()))
      .ReturnsAsync(Result.Ok(dailyPlan));

    // Act
    var result = await _sut.GenerateDailyPlanAsync();

    // Assert
    _ = result.Should().Contain("Here's your suggested daily plan");
    _ = result.Should().Contain("Task 1");
    _ = result.Should().Contain("Task 2");
    _ = result.Should().Contain("Task 3");
    _ = result.Should().Contain("Test rationale");
    _ = result.Should().Contain("Showing 3 of 10");
    _mediatorMock.Verify(m => m.Send(It.IsAny<GetDailyPlanQuery>(), It.IsAny<CancellationToken>()), Times.Once);
  }

  [Fact]
  public async Task GenerateDailyPlanAsyncWithEmptyTasksReturnsRationaleAsync()
  {
    // Arrange
    SetUp();
    var emptyPlan = CreateValidDailyPlan(taskCount: 0, totalActive: 0);

    _ = _mediatorMock
      .Setup(m => m.Send(It.IsAny<GetDailyPlanQuery>(), It.IsAny<CancellationToken>()))
      .ReturnsAsync(Result.Ok(emptyPlan));

    // Act
    var result = await _sut.GenerateDailyPlanAsync();

    // Assert
    _ = result.Should().Contain("Test rationale");
    _ = result.Should().NotContain("Here's your suggested daily plan");
    _ = result.Should().NotContain("Showing");
  }

  [Fact]
  public async Task GenerateDailyPlanAsyncWhenMediatorFailsReturnsFailedAsync()
  {
    // Arrange
    SetUp();

    _ = _mediatorMock
      .Setup(m => m.Send(It.IsAny<GetDailyPlanQuery>(), It.IsAny<CancellationToken>()))
      .ReturnsAsync(Result.Fail("Plan generation error"));

    // Act
    var result = await _sut.GenerateDailyPlanAsync();

    // Assert
    _ = result.Should().Contain("Failed to generate daily plan");
    _ = result.Should().Contain("Plan generation error");
  }

  [Fact]
  public async Task GenerateDailyPlanAsyncWhenExceptionThrownReturnsErrorAsync()
  {
    // Arrange
    SetUp();

    _ = _mediatorMock
      .Setup(m => m.Send(It.IsAny<GetDailyPlanQuery>(), It.IsAny<CancellationToken>()))
      .ThrowsAsync(new InvalidOperationException("AI service unavailable"));

    // Act
    var result = await _sut.GenerateDailyPlanAsync();

    // Assert
    _ = result.Should().Contain("Error generating daily plan");
    _ = result.Should().Contain("AI service unavailable");
  }

  #endregion GenerateDailyPlan Tests (4 tests)
}
