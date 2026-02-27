using Caramel.Application.People;
using Caramel.Core.People;
using Caramel.Domain.Common.Enums;
using Caramel.Domain.People.Models;
using Caramel.Domain.People.ValueObjects;

using FluentAssertions;

using FluentResults;

using Moq;

namespace Caramel.Application.Tests.People;

/// <summary>
/// Unit tests for PersonPlugin - AI kernel function for user preferences
/// Tests cover timezone management, daily task count configuration, and error handling
/// </summary>
public class PersonPluginTests
{
  private readonly Mock<IPersonStore> _personStoreMock = new();
  private readonly PersonConfig _personConfig = new() { DefaultTimeZoneId = "America/Chicago", DefaultDailyTaskCount = 5 };
  private readonly PersonId _personId = new(Guid.NewGuid());
  private PersonPlugin _sut = null!;

  private void SetUp()
  {
    _sut = new PersonPlugin(_personStoreMock.Object, _personConfig, _personId);
  }

  #region Helper Methods

  private static Person CreateValidPerson(
    Guid? personId = null,
    PersonTimeZoneId? timeZoneId = null,
    DailyTaskCount? dailyTaskCount = null)
  {
    return new()
    {
      Id = new(personId ?? Guid.NewGuid()),
      PlatformId = new("testuser", "123456", Platform.Discord),
      Username = new("testuser"),
      HasAccess = new(true),
      TimeZoneId = timeZoneId,
      DailyTaskCount = dailyTaskCount,
      NotificationChannels = [],
      CreatedOn = new(DateTime.UtcNow),
      UpdatedOn = new(DateTime.UtcNow)
    };
  }

  #endregion Helper Methods

  #region Set Timezone Tests

  [Fact]
  public async Task SetTimeZoneAsyncWithValidIanaIdSucceedsAsync()
  {
    // Arrange
    SetUp();
    const string timezone = "America/New_York";

    _ = _personStoreMock
      .Setup(ps => ps.SetTimeZoneAsync(_personId, It.IsAny<PersonTimeZoneId>(), It.IsAny<CancellationToken>()))
      .ReturnsAsync(Result.Ok());

    // Act
    var result = await _sut.SetTimeZoneAsync(timezone);

    // Assert
    _ = result.Should().Contain("Successfully set your timezone");
    _ = result.Should().Contain("America/New_York");
    _personStoreMock.Verify(
      ps => ps.SetTimeZoneAsync(_personId, It.IsAny<PersonTimeZoneId>(), It.IsAny<CancellationToken>()),
      Times.Once);
  }

  [Fact]
  public async Task SetTimeZoneAsyncWithValidAbbreviationEstSucceedsAsync()
  {
    // Arrange
    SetUp();
    const string timezone = "EST";

    _ = _personStoreMock
      .Setup(ps => ps.SetTimeZoneAsync(_personId, It.IsAny<PersonTimeZoneId>(), It.IsAny<CancellationToken>()))
      .ReturnsAsync(Result.Ok());

    // Act
    var result = await _sut.SetTimeZoneAsync(timezone);

    // Assert
    _ = result.Should().Contain("Successfully set your timezone");
    _ = result.Should().Contain("America/New_York");
    _personStoreMock.Verify(
      ps => ps.SetTimeZoneAsync(_personId, It.IsAny<PersonTimeZoneId>(), It.IsAny<CancellationToken>()),
      Times.Once);
  }

  [Fact]
  public async Task SetTimeZoneAsyncWithValidAbbreviationCstSucceedsAsync()
  {
    // Arrange
    SetUp();
    const string timezone = "CST";

    _ = _personStoreMock
      .Setup(ps => ps.SetTimeZoneAsync(_personId, It.IsAny<PersonTimeZoneId>(), It.IsAny<CancellationToken>()))
      .ReturnsAsync(Result.Ok());

    // Act
    var result = await _sut.SetTimeZoneAsync(timezone);

    // Assert
    _ = result.Should().Contain("Successfully set your timezone");
    _ = result.Should().Contain("America/Chicago");
    _personStoreMock.Verify(
      ps => ps.SetTimeZoneAsync(_personId, It.IsAny<PersonTimeZoneId>(), It.IsAny<CancellationToken>()),
      Times.Once);
  }

  [Fact]
  public async Task SetTimeZoneAsyncWithInvalidTimezoneFailsGracefullyAsync()
  {
    // Arrange
    SetUp();
    const string timezone = "Invalid/Timezone";

    // Act
    var result = await _sut.SetTimeZoneAsync(timezone);

    // Assert
    _ = result.Should().Contain("Failed to set timezone");
    _ = result.Should().Contain("not recognized");
    _personStoreMock.Verify(
      ps => ps.SetTimeZoneAsync(It.IsAny<PersonId>(), It.IsAny<PersonTimeZoneId>(), It.IsAny<CancellationToken>()),
      Times.Never);
  }

  [Fact]
  public async Task SetTimeZoneAsyncWithNullTimezoneFailsGracefullyAsync()
  {
    // Arrange
    SetUp();

    // Act
    var result = await _sut.SetTimeZoneAsync(null!);

    // Assert
    _ = result.Should().Contain("Failed to set timezone");
    _ = result.Should().Contain("cannot be empty");
    _personStoreMock.Verify(
      ps => ps.SetTimeZoneAsync(It.IsAny<PersonId>(), It.IsAny<PersonTimeZoneId>(), It.IsAny<CancellationToken>()),
      Times.Never);
  }

  [Fact]
  public async Task SetTimeZoneAsyncWithEmptyTimezoneFailsGracefullyAsync()
  {
    // Arrange
    SetUp();

    // Act
    var result = await _sut.SetTimeZoneAsync("");

    // Assert
    _ = result.Should().Contain("Failed to set timezone");
    _ = result.Should().Contain("cannot be empty");
    _personStoreMock.Verify(
      ps => ps.SetTimeZoneAsync(It.IsAny<PersonId>(), It.IsAny<PersonTimeZoneId>(), It.IsAny<CancellationToken>()),
      Times.Never);
  }

  [Fact]
  public async Task SetTimeZoneAsyncWithWhitespaceTimezoneFailsGracefullyAsync()
  {
    // Arrange
    SetUp();

    // Act
    var result = await _sut.SetTimeZoneAsync("   ");

    // Assert
    _ = result.Should().Contain("Failed to set timezone");
    _ = result.Should().Contain("cannot be empty");
    _personStoreMock.Verify(
      ps => ps.SetTimeZoneAsync(It.IsAny<PersonId>(), It.IsAny<PersonTimeZoneId>(), It.IsAny<CancellationToken>()),
      Times.Never);
  }

  [Fact]
  public async Task SetTimeZoneAsyncWithStoreFailureReturnsFailAsync()
  {
    // Arrange
    SetUp();
    const string timezone = "UTC";

    _ = _personStoreMock
      .Setup(ps => ps.SetTimeZoneAsync(_personId, It.IsAny<PersonTimeZoneId>(), It.IsAny<CancellationToken>()))
      .ReturnsAsync(Result.Fail("Database error"));

    // Act
    var result = await _sut.SetTimeZoneAsync(timezone);

    // Assert
    _ = result.Should().Contain("Failed to set timezone");
    _ = result.Should().Contain("Database error");
  }

  [Fact]
  public async Task SetTimeZoneAsyncWithExceptionReturnsErrorMessageAsync()
  {
    // Arrange
    SetUp();
    const string timezone = "UTC";

    _ = _personStoreMock
      .Setup(ps => ps.SetTimeZoneAsync(_personId, It.IsAny<PersonTimeZoneId>(), It.IsAny<CancellationToken>()))
      .ThrowsAsync(new InvalidOperationException("System error"));

    // Act
    var result = await _sut.SetTimeZoneAsync(timezone);

    // Assert
    _ = result.Should().Contain("Timezone operation failed");
    _ = result.Should().Contain("invalid state");
  }

  #endregion Set Timezone Tests

  #region Get Timezone Tests

  [Fact]
  public async Task GetTimeZoneAsyncWithCustomTimezoneReturnsCustomValueAsync()
  {
    // Arrange
    SetUp();
    _ = PersonTimeZoneId.TryParse("Europe/London", out var customTimeZone, out _);
    var person = CreateValidPerson(_personId.Value, customTimeZone);

    _ = _personStoreMock
      .Setup(ps => ps.GetAsync(_personId, It.IsAny<CancellationToken>()))
      .ReturnsAsync(Result.Ok(person));

    // Act
    var result = await _sut.GetTimeZoneAsync();

    // Assert
    _ = result.Should().Contain("Your timezone is set to");
    _ = result.Should().Contain("Europe/London");
    _personStoreMock.Verify(
      ps => ps.GetAsync(_personId, It.IsAny<CancellationToken>()),
      Times.Once);
  }

  [Fact]
  public async Task GetTimeZoneAsyncWithoutCustomTimezoneReturnsDefaultAsync()
  {
    // Arrange
    SetUp();
    var person = CreateValidPerson(_personId.Value, null);

    _ = _personStoreMock
      .Setup(ps => ps.GetAsync(_personId, It.IsAny<CancellationToken>()))
      .ReturnsAsync(Result.Ok(person));

    // Act
    var result = await _sut.GetTimeZoneAsync();

    // Assert
    _ = result.Should().Contain("You are currently using the default timezone");
    _ = result.Should().Contain(_personConfig.DefaultTimeZoneId);
  }

  [Fact]
  public async Task GetTimeZoneAsyncWithStoreFailureReturnsFailAsync()
  {
    // Arrange
    SetUp();

    _ = _personStoreMock
      .Setup(ps => ps.GetAsync(_personId, It.IsAny<CancellationToken>()))
      .ReturnsAsync(Result.Fail("Person not found"));

    // Act
    var result = await _sut.GetTimeZoneAsync();

    // Assert
    _ = result.Should().Contain("Failed to retrieve timezone");
    _ = result.Should().Contain("Person not found");
  }

  [Fact]
  public async Task GetTimeZoneAsyncWithExceptionReturnsErrorMessageAsync()
  {
    // Arrange
    SetUp();

    _ = _personStoreMock
      .Setup(ps => ps.GetAsync(_personId, It.IsAny<CancellationToken>()))
      .ThrowsAsync(new InvalidOperationException("Database connection failed"));

    // Act
    var result = await _sut.GetTimeZoneAsync();

    // Assert
    _ = result.Should().Contain("Timezone lookup failed");
    _ = result.Should().Contain("invalid state");
  }

  #endregion Get Timezone Tests

  #region Set Daily Task Count Tests

  [Fact]
  public async Task SetDailyTaskCountAsyncWithValidCountOneSucceedsAsync()
  {
    // Arrange
    SetUp();
    const int count = 1;

    _ = _personStoreMock
      .Setup(ps => ps.SetDailyTaskCountAsync(_personId, It.IsAny<DailyTaskCount>(), It.IsAny<CancellationToken>()))
      .ReturnsAsync(Result.Ok());

    // Act
    var result = await _sut.SetDailyTaskCountAsync(count);

    // Assert
    _ = result.Should().Contain("Successfully set your daily task count");
    _ = result.Should().Contain("1 tasks");
    _personStoreMock.Verify(
      ps => ps.SetDailyTaskCountAsync(_personId, It.IsAny<DailyTaskCount>(), It.IsAny<CancellationToken>()),
      Times.Once);
  }

  [Fact]
  public async Task SetDailyTaskCountAsyncWithValidCountTenSucceedsAsync()
  {
    // Arrange
    SetUp();
    const int count = 10;

    _ = _personStoreMock
      .Setup(ps => ps.SetDailyTaskCountAsync(_personId, It.IsAny<DailyTaskCount>(), It.IsAny<CancellationToken>()))
      .ReturnsAsync(Result.Ok());

    // Act
    var result = await _sut.SetDailyTaskCountAsync(count);

    // Assert
    _ = result.Should().Contain("Successfully set your daily task count");
    _ = result.Should().Contain("10 tasks");
    _personStoreMock.Verify(
      ps => ps.SetDailyTaskCountAsync(_personId, It.IsAny<DailyTaskCount>(), It.IsAny<CancellationToken>()),
      Times.Once);
  }

  [Fact]
  public async Task SetDailyTaskCountAsyncWithValidCountTwentySucceedsAsync()
  {
    // Arrange
    SetUp();
    const int count = 20;

    _ = _personStoreMock
      .Setup(ps => ps.SetDailyTaskCountAsync(_personId, It.IsAny<DailyTaskCount>(), It.IsAny<CancellationToken>()))
      .ReturnsAsync(Result.Ok());

    // Act
    var result = await _sut.SetDailyTaskCountAsync(count);

    // Assert
    _ = result.Should().Contain("Successfully set your daily task count");
    _ = result.Should().Contain("20 tasks");
    _personStoreMock.Verify(
      ps => ps.SetDailyTaskCountAsync(_personId, It.IsAny<DailyTaskCount>(), It.IsAny<CancellationToken>()),
      Times.Once);
  }

  [Fact]
  public async Task SetDailyTaskCountAsyncWithCountZeroFailsGracefullyAsync()
  {
    // Arrange
    SetUp();
    const int count = 0;

    // Act
    var result = await _sut.SetDailyTaskCountAsync(count);

    // Assert
    _ = result.Should().Contain("Failed to set daily task count");
    _ = result.Should().Contain("between 1 and 20");
    _personStoreMock.Verify(
      ps => ps.SetDailyTaskCountAsync(It.IsAny<PersonId>(), It.IsAny<DailyTaskCount>(), It.IsAny<CancellationToken>()),
      Times.Never);
  }

  [Fact]
  public async Task SetDailyTaskCountAsyncWithCountNegativeFailsGracefullyAsync()
  {
    // Arrange
    SetUp();
    const int count = -5;

    // Act
    var result = await _sut.SetDailyTaskCountAsync(count);

    // Assert
    _ = result.Should().Contain("Failed to set daily task count");
    _ = result.Should().Contain("between 1 and 20");
    _personStoreMock.Verify(
      ps => ps.SetDailyTaskCountAsync(It.IsAny<PersonId>(), It.IsAny<DailyTaskCount>(), It.IsAny<CancellationToken>()),
      Times.Never);
  }

  [Fact]
  public async Task SetDailyTaskCountAsyncWithCountTwentyOneFailsGracefullyAsync()
  {
    // Arrange
    SetUp();
    const int count = 21;

    // Act
    var result = await _sut.SetDailyTaskCountAsync(count);

    // Assert
    _ = result.Should().Contain("Failed to set daily task count");
    _ = result.Should().Contain("between 1 and 20");
    _personStoreMock.Verify(
      ps => ps.SetDailyTaskCountAsync(It.IsAny<PersonId>(), It.IsAny<DailyTaskCount>(), It.IsAny<CancellationToken>()),
      Times.Never);
  }

  [Fact]
  public async Task SetDailyTaskCountAsyncWithCountExceedsMaxFailsGracefullyAsync()
  {
    // Arrange
    SetUp();
    const int count = 100;

    // Act
    var result = await _sut.SetDailyTaskCountAsync(count);

    // Assert
    _ = result.Should().Contain("Failed to set daily task count");
    _ = result.Should().Contain("between 1 and 20");
    _personStoreMock.Verify(
      ps => ps.SetDailyTaskCountAsync(It.IsAny<PersonId>(), It.IsAny<DailyTaskCount>(), It.IsAny<CancellationToken>()),
      Times.Never);
  }

  [Fact]
  public async Task SetDailyTaskCountAsyncWithStoreFailureReturnsFailAsync()
  {
    // Arrange
    SetUp();
    const int count = 5;

    _ = _personStoreMock
      .Setup(ps => ps.SetDailyTaskCountAsync(_personId, It.IsAny<DailyTaskCount>(), It.IsAny<CancellationToken>()))
      .ReturnsAsync(Result.Fail("Database error"));

    // Act
    var result = await _sut.SetDailyTaskCountAsync(count);

    // Assert
    _ = result.Should().Contain("Failed to set daily task count");
    _ = result.Should().Contain("Database error");
  }

  [Fact]
  public async Task SetDailyTaskCountAsyncWithExceptionReturnsErrorMessageAsync()
  {
    // Arrange
    SetUp();
    const int count = 5;

    _ = _personStoreMock
      .Setup(ps => ps.SetDailyTaskCountAsync(_personId, It.IsAny<DailyTaskCount>(), It.IsAny<CancellationToken>()))
      .ThrowsAsync(new InvalidOperationException("System error"));

    // Act
    var result = await _sut.SetDailyTaskCountAsync(count);

    // Assert
    _ = result.Should().Contain("Task count operation failed");
    _ = result.Should().Contain("invalid state");
  }

  #endregion Set Daily Task Count Tests

  #region Get Daily Task Count Tests

  [Fact]
  public async Task GetDailyTaskCountAsyncWithCustomCountReturnsCustomValueAsync()
  {
    // Arrange
    SetUp();
    var customCount = new DailyTaskCount(15);
    var person = CreateValidPerson(_personId.Value, null, customCount);

    _ = _personStoreMock
      .Setup(ps => ps.GetAsync(_personId, It.IsAny<CancellationToken>()))
      .ReturnsAsync(Result.Ok(person));

    // Act
    var result = await _sut.GetDailyTaskCountAsync();

    // Assert
    _ = result.Should().Contain("Your daily task count is set to");
    _ = result.Should().Contain("15 tasks per day");
    _personStoreMock.Verify(
      ps => ps.GetAsync(_personId, It.IsAny<CancellationToken>()),
      Times.Once);
  }

  [Fact]
  public async Task GetDailyTaskCountAsyncWithoutCustomCountReturnsDefaultAsync()
  {
    // Arrange
    SetUp();
    var person = CreateValidPerson(_personId.Value, null, null);

    _ = _personStoreMock
      .Setup(ps => ps.GetAsync(_personId, It.IsAny<CancellationToken>()))
      .ReturnsAsync(Result.Ok(person));

    // Act
    var result = await _sut.GetDailyTaskCountAsync();

    // Assert
    _ = result.Should().Contain("You are currently using the default daily task count");
    _ = result.Should().Contain("5 tasks per day");
  }

  [Fact]
  public async Task GetDailyTaskCountAsyncWithStoreFailureReturnsFailAsync()
  {
    // Arrange
    SetUp();

    _ = _personStoreMock
      .Setup(ps => ps.GetAsync(_personId, It.IsAny<CancellationToken>()))
      .ReturnsAsync(Result.Fail("Person not found"));

    // Act
    var result = await _sut.GetDailyTaskCountAsync();

    // Assert
    _ = result.Should().Contain("Failed to retrieve daily task count");
    _ = result.Should().Contain("Person not found");
  }

  [Fact]
  public async Task GetDailyTaskCountAsyncWithExceptionReturnsErrorMessageAsync()
  {
    // Arrange
    SetUp();

    _ = _personStoreMock
      .Setup(ps => ps.GetAsync(_personId, It.IsAny<CancellationToken>()))
      .ThrowsAsync(new InvalidOperationException("Database connection failed"));

    // Act
    var result = await _sut.GetDailyTaskCountAsync();

    // Assert
    _ = result.Should().Contain("Task count lookup failed");
    _ = result.Should().Contain("invalid state");
  }

  #endregion Get Daily Task Count Tests
}
