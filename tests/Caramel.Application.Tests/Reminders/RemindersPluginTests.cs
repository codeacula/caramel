using System.Globalization;

using Caramel.Application.Reminders;
using Caramel.Application.ToDos;
using Caramel.Core.People;
using Caramel.Core.ToDos;
using Caramel.Domain.Common.Enums;
using Caramel.Domain.People.Models;
using Caramel.Domain.People.ValueObjects;
using Caramel.Domain.ToDos.Models;

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
      _ = PersonTimeZoneId.TryParse(timeZoneId, out var tz, out _);
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

  #endregion Helper Methods

  #region Create Reminder Tests

  [Fact]
  public async Task CreateReminderAsyncWithFuzzyTimeSucceedsAsync()
  {
    // Arrange
    SetUp();
    const string reminderMessage = "Take a break";
    const string reminderTime = "in 10 minutes";
    var now = DateTime.UtcNow;
    var expectedTime = now.AddMinutes(10);
    var command = new CreateReminderCommand(_personId, reminderMessage, expectedTime);
    var reminder = new Reminder { Id = new(Guid.NewGuid()), PersonId = _personId, Details = new(reminderMessage) };

    _ = _timeProviderMock.Setup(tp => tp.GetUtcNow()).Returns(new DateTimeOffset(now));
    _ = _fuzzyTimeParserMock.Setup(fp => fp.TryParseFuzzyTime(reminderTime, It.IsAny<DateTime>()))
      .Returns(Result.Ok(expectedTime));
    _ = _mediatorMock.Setup(m => m.Send(It.IsAny<CreateReminderCommand>(), It.IsAny<CancellationToken>()))
      .ReturnsAsync(Result.Ok(reminder));

    // Act
    var result = await _sut.CreateReminderAsync(reminderMessage, reminderTime);

    // Assert
    _ = result.Should().Contain("Successfully created reminder");
    _ = result.Should().Contain(reminderMessage);
    _mediatorMock.Verify(m => m.Send(It.IsAny<CreateReminderCommand>(), It.IsAny<CancellationToken>()), Times.Once);
  }

  [Fact]
  public async Task CreateReminderAsyncWithIsoFormatSucceedsAsync()
  {
    // Arrange
    SetUp();
    const string reminderMessage = "Remind me";
    const string reminderTime = "2025-12-31T10:00:00";
    var person = CreateValidPerson(_personId.Value);
    var command = new CreateReminderCommand(_personId, reminderMessage, It.IsAny<DateTime>());

    _ = _timeProviderMock.Setup(tp => tp.GetUtcNow()).Returns(new DateTimeOffset(DateTime.UtcNow));
    _ = _fuzzyTimeParserMock.Setup(fp => fp.TryParseFuzzyTime(It.IsAny<string>(), It.IsAny<DateTime>()))
      .Returns(Result.Fail<DateTime>("Not fuzzy"));
    _ = _personStoreMock.Setup(ps => ps.GetAsync(_personId, It.IsAny<CancellationToken>()))
      .ReturnsAsync(Result.Ok(person));
    _ = _mediatorMock.Setup(m => m.Send(It.IsAny<CreateReminderCommand>(), It.IsAny<CancellationToken>()))
      .ReturnsAsync(Result.Ok(new Reminder { Id = new(Guid.NewGuid()), PersonId = _personId, Details = new(reminderMessage) }));

    // Act
    var result = await _sut.CreateReminderAsync(reminderMessage, reminderTime);

    // Assert
    _ = result.Should().Contain("Successfully created reminder");
  }

  [Fact]
  public async Task CreateReminderAsyncWithEmptyMessageFailsGracefullyAsync()
  {
    // Arrange
    SetUp();
    const string reminderMessage = "";
    const string reminderTime = "in 10 minutes";

    _ = _timeProviderMock.Setup(tp => tp.GetUtcNow()).Returns(new DateTimeOffset(DateTime.UtcNow));

    // Act
    var result = await _sut.CreateReminderAsync(reminderMessage, reminderTime);

    // Assert
    _ = result.Should().Contain("Error");
  }

  [Fact]
  public async Task CreateReminderAsyncWithNullTimeFailsGracefullyAsync()
  {
    // Arrange
    SetUp();
    const string reminderMessage = "Take a break";

    _ = _timeProviderMock.Setup(tp => tp.GetUtcNow()).Returns(new DateTimeOffset(DateTime.UtcNow));
    _ = _fuzzyTimeParserMock.Setup(fp => fp.TryParseFuzzyTime(It.IsAny<string>(), It.IsAny<DateTime>()))
      .Returns(Result.Fail<DateTime>("Required"));

    // Act
    var result = await _sut.CreateReminderAsync(reminderMessage, "");

    // Assert
    _ = result.Should().Contain("Failed to create reminder");
  }

  [Fact]
  public async Task CreateReminderAsyncWithInvalidTimeFormatFailsGracefullyAsync()
  {
    // Arrange
    SetUp();
    const string reminderMessage = "Take a break";
    const string reminderTime = "invalid time format that cannot be parsed";

    _ = _timeProviderMock.Setup(tp => tp.GetUtcNow()).Returns(new DateTimeOffset(DateTime.UtcNow));
    _ = _fuzzyTimeParserMock.Setup(fp => fp.TryParseFuzzyTime(reminderTime, It.IsAny<DateTime>()))
      .Returns(Result.Fail<DateTime>("Not valid fuzzy"));

    // Act
    var result = await _sut.CreateReminderAsync(reminderMessage, reminderTime);

    // Assert
    _ = result.Should().Contain("Failed to create reminder");
  }

  [Fact]
  public async Task CreateReminderAsyncWithMediatorFailureReturnsFailAsync()
  {
    // Arrange
    SetUp();
    const string reminderMessage = "Take a break";
    const string reminderTime = "in 10 minutes";
    var now = DateTime.UtcNow;
    var expectedTime = now.AddMinutes(10);

    _ = _timeProviderMock.Setup(tp => tp.GetUtcNow()).Returns(new DateTimeOffset(now));
    _ = _fuzzyTimeParserMock.Setup(fp => fp.TryParseFuzzyTime(reminderTime, It.IsAny<DateTime>()))
      .Returns(Result.Ok(expectedTime));
    _ = _mediatorMock.Setup(m => m.Send(It.IsAny<CreateReminderCommand>(), It.IsAny<CancellationToken>()))
      .ReturnsAsync(Result.Fail<Reminder>("Database error"));

    // Act
    var result = await _sut.CreateReminderAsync(reminderMessage, reminderTime);

    // Assert
    _ = result.Should().Contain("Failed to create reminder");
    _ = result.Should().Contain("Database error");
  }

  [Fact]
  public async Task CreateReminderAsyncWithExceptionReturnsErrorMessageAsync()
  {
    // Arrange
    SetUp();
    const string reminderMessage = "Take a break";
    const string reminderTime = "in 10 minutes";

    _ = _timeProviderMock.Setup(tp => tp.GetUtcNow()).Throws(new InvalidOperationException("System error"));

    // Act
    var result = await _sut.CreateReminderAsync(reminderMessage, reminderTime);

    // Assert
    _ = result.Should().Contain("Error creating reminder");
  }

  #endregion Create Reminder Tests

  #region Cancel Reminder Tests

  [Fact]
  public async Task CancelReminderAsyncWithValidIdSucceedsAsync()
  {
    // Arrange
    SetUp();
    var reminderId = Guid.NewGuid().ToString();

    _ = _mediatorMock.Setup(m => m.Send(It.IsAny<CancelReminderCommand>(), It.IsAny<CancellationToken>()))
      .ReturnsAsync(Result.Ok());

    // Act
    var result = await _sut.CancelReminderAsync(reminderId);

    // Assert
    _ = result.Should().Contain("Successfully canceled");
    _mediatorMock.Verify(m => m.Send(It.IsAny<CancelReminderCommand>(), It.IsAny<CancellationToken>()), Times.Once);
  }

  [Fact]
  public async Task CancelReminderAsyncWithInvalidIdFormatFailsAsync()
  {
    // Arrange
    SetUp();
    const string reminderId = "not-a-guid";

    // Act
    var result = await _sut.CancelReminderAsync(reminderId);

    // Assert
    _ = result.Should().Contain("Failed to cancel reminder");
    _ = result.Should().Contain("Invalid reminder ID format");
  }

  [Fact]
  public async Task CancelReminderAsyncWithMediatorFailureReturnsFailAsync()
  {
    // Arrange
    SetUp();
    var reminderId = Guid.NewGuid().ToString();

    _ = _mediatorMock.Setup(m => m.Send(It.IsAny<CancelReminderCommand>(), It.IsAny<CancellationToken>()))
      .ReturnsAsync(Result.Fail("Reminder not found"));

    // Act
    var result = await _sut.CancelReminderAsync(reminderId);

    // Assert
    _ = result.Should().Contain("Failed to cancel reminder");
    _ = result.Should().Contain("Reminder not found");
  }

  [Fact]
  public async Task CancelReminderAsyncWithExceptionReturnsErrorMessageAsync()
  {
    // Arrange
    SetUp();
    var reminderId = Guid.NewGuid().ToString();

    _ = _mediatorMock.Setup(m => m.Send(It.IsAny<CancelReminderCommand>(), It.IsAny<CancellationToken>()))
      .ThrowsAsync(new InvalidOperationException("System error"));

    // Act
    var result = await _sut.CancelReminderAsync(reminderId);

    // Assert
    _ = result.Should().Contain("Error canceling reminder");
  }

  #endregion Cancel Reminder Tests

  #region Time Zone Tests

  [Fact]
  public async Task CreateReminderAsyncWithCustomTimeZoneConvertsCorrectlyAsync()
  {
    // Arrange
    SetUp();
    var personId = Guid.NewGuid();
    const string reminderMessage = "Check the oven";
    const string reminderTime = "2025-12-31T10:00:00"; // Unspecified kind
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

    _ = _fuzzyTimeParserMock.Setup(fp => fp.TryParseFuzzyTime(reminderTime, It.IsAny<DateTime>()))
      .Returns(Result.Fail<DateTime>("Not fuzzy"));
    _ = _personStoreMock.Setup(ps => ps.GetAsync(new(personId), It.IsAny<CancellationToken>()))
      .ReturnsAsync(Result.Ok(person));
    _ = _mediatorMock.Setup(m => m.Send(It.IsAny<CreateReminderCommand>(), It.IsAny<CancellationToken>()))
      .ReturnsAsync(Result.Ok(new Reminder { Id = new(Guid.NewGuid()), PersonId = new(personId), Details = new(reminderMessage) }));

    // Act
    var result = await sut.CreateReminderAsync(reminderMessage, reminderTime);

    // Assert
    _ = result.Should().Contain("Successfully created reminder");
    _personStoreMock.Verify(ps => ps.GetAsync(new(personId), It.IsAny<CancellationToken>()), Times.Once);
  }

  #endregion Time Zone Tests

  #region Multiple Reminders Tests

  [Fact]
  public async Task CreateMultipleRemindersInSequenceSucceedsAsync()
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
    _ = _timeProviderMock.Setup(tp => tp.GetUtcNow()).Returns(new DateTimeOffset(now));
    _ = _fuzzyTimeParserMock.Setup(fp => fp.TryParseFuzzyTime(It.IsAny<string>(), It.IsAny<DateTime>()))
      .Returns<string, DateTime>((time, _) =>
      {
        var minutes = int.Parse(time.Split()[1], CultureInfo.InvariantCulture);
        return Result.Ok(now.AddMinutes(minutes));
      });
    _ = _mediatorMock.Setup(m => m.Send(It.IsAny<CreateReminderCommand>(), It.IsAny<CancellationToken>()))
      .ReturnsAsync(Result.Ok(new Reminder { Id = new(Guid.NewGuid()), PersonId = _personId, Details = new("") }));

    // Act
    var results = new List<string>();
    foreach (var (message, time) in reminders)
    {
      results.Add(await _sut.CreateReminderAsync(message, time));
    }

    // Assert
    _ = results.Should().AllSatisfy(r => r.Should().Contain("Successfully created reminder"));
    _mediatorMock.Verify(m => m.Send(It.IsAny<CreateReminderCommand>(), It.IsAny<CancellationToken>()), Times.Exactly(3));
  }

  #endregion Multiple Reminders Tests
}
