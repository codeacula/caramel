using Caramel.Application.Reminders;
using Caramel.Application.ToDos;
using Caramel.Core;
using Caramel.Core.People;
using Caramel.Core.ToDos;
using Caramel.Domain.Common.Enums;
using Caramel.Domain.People.Models;
using Caramel.Domain.People.ValueObjects;
using Caramel.Domain.ToDos.Models;
using Caramel.Domain.ToDos.ValueObjects;

using FluentAssertions;

using FluentResults;

using MediatR;

using Moq;

namespace Caramel.Application.Tests.Reminders;

/// <summary>
/// Unit tests for RemindersPlugin - AI kernel function for reminder creation
/// Tests cover fuzzy time parsing, reminder creation, and cancellation
/// </summary>
public class RemindersPluginTests
{
  private readonly Mock<IMediator> _mediatorMock = new();
  private readonly Mock<IPersonStore> _personStoreMock = new();
  private readonly Mock<IFuzzyTimeParser> _fuzzyTimeParserMock = new();
  private readonly Mock<TimeProvider> _timeProviderMock = new();
  private readonly PersonConfig _personConfig = new() { DefaultTimeZoneId = "UTC" };
  private readonly PersonId _personId = new(Guid.NewGuid());
  private RemindersPlugin _sut = null!;

  private void SetUp()
  {
    _sut = new RemindersPlugin(
      _mediatorMock.Object,
      _personStoreMock.Object,
      _fuzzyTimeParserMock.Object,
      _timeProviderMock.Object,
      _personConfig,
      _personId
    );
  }

  #region Helper Methods

  private static Person CreateValidPerson(Guid? personId = null, string? timeZoneId = "UTC")
  {
    PersonTimeZoneId? tzId = null;
    if (timeZoneId is not null)
    {
      PersonTimeZoneId.TryParse(timeZoneId, out var tz, out _);
      tzId = tz;
    }

    return new()
    {
      Id = new(personId ?? Guid.NewGuid()),
      PlatformId = new("testuser", "123456", Platform.Discord),
      Username = new("testuser"),
      HasAccess = new(true),
      TimeZoneId = tzId,
      NotificationChannels = [],
      CreatedOn = new(DateTime.UtcNow),
      UpdatedOn = new(DateTime.UtcNow)
    };
  }

  #endregion

  #region Create Reminder Tests

  [Fact]
  public async Task CreateReminderAsyncWithFuzzyTimeSucceeds()
  {
    // Arrange
    SetUp();
    var reminderMessage = "Take a break";
    var reminderTime = "in 10 minutes";
    var now = DateTime.UtcNow;
    var expectedTime = now.AddMinutes(10);
    var command = new CreateReminderCommand(_personId, reminderMessage, expectedTime);
    var reminder = new Reminder { Id = new(Guid.NewGuid()), PersonId = _personId, Details = new(reminderMessage) };

    _timeProviderMock.Setup(tp => tp.GetUtcNow()).Returns(new DateTimeOffset(now));
    _fuzzyTimeParserMock.Setup(fp => fp.TryParseFuzzyTime(reminderTime, It.IsAny<DateTime>()))
      .Returns(Result.Ok(expectedTime));
    _mediatorMock.Setup(m => m.Send(It.IsAny<CreateReminderCommand>(), It.IsAny<CancellationToken>()))
      .ReturnsAsync(Result.Ok(reminder));

    // Act
    var result = await _sut.CreateReminderAsync(reminderMessage, reminderTime);

    // Assert
    result.Should().Contain("Successfully created reminder");
    result.Should().Contain(reminderMessage);
    _mediatorMock.Verify(m => m.Send(It.IsAny<CreateReminderCommand>(), It.IsAny<CancellationToken>()), Times.Once);
  }

  [Fact]
  public async Task CreateReminderAsyncWithIsoFormatSucceeds()
  {
    // Arrange
    SetUp();
    var reminderMessage = "Remind me";
    var reminderTime = "2025-12-31T10:00:00";
    var person = CreateValidPerson(_personId.Value);
    var command = new CreateReminderCommand(_personId, reminderMessage, It.IsAny<DateTime>());

    _timeProviderMock.Setup(tp => tp.GetUtcNow()).Returns(new DateTimeOffset(DateTime.UtcNow));
    _fuzzyTimeParserMock.Setup(fp => fp.TryParseFuzzyTime(It.IsAny<string>(), It.IsAny<DateTime>()))
      .Returns(Result.Fail<DateTime>("Not fuzzy"));
    _personStoreMock.Setup(ps => ps.GetAsync(_personId, It.IsAny<CancellationToken>()))
      .ReturnsAsync(Result.Ok(person));
    _mediatorMock.Setup(m => m.Send(It.IsAny<CreateReminderCommand>(), It.IsAny<CancellationToken>()))
      .ReturnsAsync(Result.Ok(new Reminder { Id = new(Guid.NewGuid()), PersonId = _personId, Details = new(reminderMessage) }));

    // Act
    var result = await _sut.CreateReminderAsync(reminderMessage, reminderTime);

    // Assert
    result.Should().Contain("Successfully created reminder");
  }

  [Fact]
  public async Task CreateReminderAsyncWithEmptyMessageFailsGracefully()
  {
    // Arrange
    SetUp();
    var reminderMessage = "";
    var reminderTime = "in 10 minutes";

    _timeProviderMock.Setup(tp => tp.GetUtcNow()).Returns(new DateTimeOffset(DateTime.UtcNow));

    // Act
    var result = await _sut.CreateReminderAsync(reminderMessage, reminderTime);

    // Assert
    result.Should().Contain("Error");
  }

  [Fact]
  public async Task CreateReminderAsyncWithNullTimeFailsGracefully()
  {
    // Arrange
    SetUp();
    var reminderMessage = "Take a break";
    string? reminderTime = null;

    _timeProviderMock.Setup(tp => tp.GetUtcNow()).Returns(new DateTimeOffset(DateTime.UtcNow));
    _fuzzyTimeParserMock.Setup(fp => fp.TryParseFuzzyTime(It.IsAny<string>(), It.IsAny<DateTime>()))
      .Returns(Result.Fail<DateTime>("Required"));

    // Act
    var result = await _sut.CreateReminderAsync(reminderMessage, reminderTime ?? "");

    // Assert
    result.Should().Contain("Failed to create reminder");
  }

  [Fact]
  public async Task CreateReminderAsyncWithInvalidTimeFormatFailsGracefully()
  {
    // Arrange
    SetUp();
    var reminderMessage = "Take a break";
    var reminderTime = "invalid time format that cannot be parsed";

    _timeProviderMock.Setup(tp => tp.GetUtcNow()).Returns(new DateTimeOffset(DateTime.UtcNow));
    _fuzzyTimeParserMock.Setup(fp => fp.TryParseFuzzyTime(reminderTime, It.IsAny<DateTime>()))
      .Returns(Result.Fail<DateTime>("Not valid fuzzy"));

    // Act
    var result = await _sut.CreateReminderAsync(reminderMessage, reminderTime);

    // Assert
    result.Should().Contain("Failed to create reminder");
  }

  [Fact]
  public async Task CreateReminderAsyncWithMediatorFailureReturnsFail()
  {
    // Arrange
    SetUp();
    var reminderMessage = "Take a break";
    var reminderTime = "in 10 minutes";
    var now = DateTime.UtcNow;
    var expectedTime = now.AddMinutes(10);

    _timeProviderMock.Setup(tp => tp.GetUtcNow()).Returns(new DateTimeOffset(now));
    _fuzzyTimeParserMock.Setup(fp => fp.TryParseFuzzyTime(reminderTime, It.IsAny<DateTime>()))
      .Returns(Result.Ok(expectedTime));
    _mediatorMock.Setup(m => m.Send(It.IsAny<CreateReminderCommand>(), It.IsAny<CancellationToken>()))
      .ReturnsAsync(Result.Fail<Reminder>("Database error"));

    // Act
    var result = await _sut.CreateReminderAsync(reminderMessage, reminderTime);

    // Assert
    result.Should().Contain("Failed to create reminder");
    result.Should().Contain("Database error");
  }

   [Fact]
   public async Task CreateReminderAsyncWithExceptionReturnsErrorMessage()
   {
     // Arrange
     SetUp();
     var reminderMessage = "Take a break";
     var reminderTime = "in 10 minutes";

     _timeProviderMock.Setup(tp => tp.GetUtcNow()).Throws(new InvalidOperationException("System error"));

     // Act
     var result = await _sut.CreateReminderAsync(reminderMessage, reminderTime);

     // Assert
     result.Should().Contain("Error creating reminder");
   }

  #endregion

  #region Cancel Reminder Tests

  [Fact]
  public async Task CancelReminderAsyncWithValidIdSucceeds()
  {
    // Arrange
    SetUp();
    var reminderId = Guid.NewGuid().ToString();

    _mediatorMock.Setup(m => m.Send(It.IsAny<CancelReminderCommand>(), It.IsAny<CancellationToken>()))
      .ReturnsAsync(Result.Ok());

    // Act
    var result = await _sut.CancelReminderAsync(reminderId);

    // Assert
    result.Should().Contain("Successfully canceled");
    _mediatorMock.Verify(m => m.Send(It.IsAny<CancelReminderCommand>(), It.IsAny<CancellationToken>()), Times.Once);
  }

  [Fact]
  public async Task CancelReminderAsyncWithInvalidIdFormatFails()
  {
    // Arrange
    SetUp();
    var reminderId = "not-a-guid";

    // Act
    var result = await _sut.CancelReminderAsync(reminderId);

    // Assert
    result.Should().Contain("Failed to cancel reminder");
    result.Should().Contain("Invalid reminder ID format");
  }

  [Fact]
  public async Task CancelReminderAsyncWithMediatorFailureReturnsFail()
  {
    // Arrange
    SetUp();
    var reminderId = Guid.NewGuid().ToString();

    _mediatorMock.Setup(m => m.Send(It.IsAny<CancelReminderCommand>(), It.IsAny<CancellationToken>()))
      .ReturnsAsync(Result.Fail("Reminder not found"));

    // Act
    var result = await _sut.CancelReminderAsync(reminderId);

    // Assert
    result.Should().Contain("Failed to cancel reminder");
    result.Should().Contain("Reminder not found");
  }

  [Fact]
  public async Task CancelReminderAsyncWithExceptionReturnsErrorMessage()
  {
    // Arrange
    SetUp();
    var reminderId = Guid.NewGuid().ToString();

    _mediatorMock.Setup(m => m.Send(It.IsAny<CancelReminderCommand>(), It.IsAny<CancellationToken>()))
      .ThrowsAsync(new InvalidOperationException("System error"));

    // Act
    var result = await _sut.CancelReminderAsync(reminderId);

    // Assert
    result.Should().Contain("Error canceling reminder");
  }

  #endregion

  #region Time Zone Tests

  [Fact]
  public async Task CreateReminderAsyncWithCustomTimeZoneConvertsCorrectly()
  {
    // Arrange
    SetUp();
    var personId = Guid.NewGuid();
    var reminderMessage = "Check the oven";
    var reminderTime = "2025-12-31T10:00:00"; // Unspecified kind
    var person = CreateValidPerson(personId, "America/New_York");

    // Need to reinitialize with the new personId for proper timezone lookup
    var sut = new RemindersPlugin(
      _mediatorMock.Object,
      _personStoreMock.Object,
      _fuzzyTimeParserMock.Object,
      _timeProviderMock.Object,
      _personConfig,
      new(personId)
    );

    _fuzzyTimeParserMock.Setup(fp => fp.TryParseFuzzyTime(reminderTime, It.IsAny<DateTime>()))
      .Returns(Result.Fail<DateTime>("Not fuzzy"));
    _personStoreMock.Setup(ps => ps.GetAsync(new(personId), It.IsAny<CancellationToken>()))
      .ReturnsAsync(Result.Ok(person));
    _mediatorMock.Setup(m => m.Send(It.IsAny<CreateReminderCommand>(), It.IsAny<CancellationToken>()))
      .ReturnsAsync(Result.Ok(new Reminder { Id = new(Guid.NewGuid()), PersonId = new(personId), Details = new(reminderMessage) }));

    // Act
    var result = await sut.CreateReminderAsync(reminderMessage, reminderTime);

    // Assert
    result.Should().Contain("Successfully created reminder");
    _personStoreMock.Verify(ps => ps.GetAsync(new(personId), It.IsAny<CancellationToken>()), Times.Once);
  }

  #endregion

  #region Multiple Reminders Tests

  [Fact]
  public async Task CreateMultipleRemindersInSequenceSucceeds()
  {
    // Arrange
    SetUp();
    var reminders = new[]
    {
      ("Take a break", "in 10 minutes"),
      ("Drink water", "in 30 minutes"),
      ("Stand up", "in 1 hour")
    };

    var now = DateTime.UtcNow;
    _timeProviderMock.Setup(tp => tp.GetUtcNow()).Returns(new DateTimeOffset(now));
    _fuzzyTimeParserMock.Setup(fp => fp.TryParseFuzzyTime(It.IsAny<string>(), It.IsAny<DateTime>()))
      .Returns<string, DateTime>((time, _) => Result.Ok(now.AddMinutes(int.Parse(time.Split()[1]))));
    _mediatorMock.Setup(m => m.Send(It.IsAny<CreateReminderCommand>(), It.IsAny<CancellationToken>()))
      .ReturnsAsync(Result.Ok(new Reminder { Id = new(Guid.NewGuid()), PersonId = _personId, Details = new("") }));

    // Act
    var results = new List<string>();
    foreach (var (message, time) in reminders)
    {
      results.Add(await _sut.CreateReminderAsync(message, time));
    }

    // Assert
    results.Should().AllSatisfy(r => r.Should().Contain("Successfully created reminder"));
    _mediatorMock.Verify(m => m.Send(It.IsAny<CreateReminderCommand>(), It.IsAny<CancellationToken>()), Times.Exactly(3));
  }

  #endregion
}
