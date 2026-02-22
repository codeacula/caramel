using Caramel.Application.People;
using Caramel.Core.People;
using Caramel.Domain.Common.Enums;
using Caramel.Domain.Common.ValueObjects;
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

  #endregion

  #region Set Timezone Tests

  [Fact]
  public async Task SetTimeZoneAsyncWithValidIanaIdSucceeds()
  {
    // Arrange
    SetUp();
    const string timezone = "America/New_York";

    _personStoreMock
      .Setup(ps => ps.SetTimeZoneAsync(_personId, It.IsAny<PersonTimeZoneId>(), It.IsAny<CancellationToken>()))
      .ReturnsAsync(Result.Ok());

    // Act
    var result = await _sut.SetTimeZoneAsync(timezone);

    // Assert
    result.Should().Contain("Successfully set your timezone");
    result.Should().Contain("America/New_York");
    _personStoreMock.Verify(
      ps => ps.SetTimeZoneAsync(_personId, It.IsAny<PersonTimeZoneId>(), It.IsAny<CancellationToken>()),
      Times.Once);
  }

  [Fact]
  public async Task SetTimeZoneAsyncWithValidAbbreviationEstSucceeds()
  {
    // Arrange
    SetUp();
    const string timezone = "EST";

    _personStoreMock
      .Setup(ps => ps.SetTimeZoneAsync(_personId, It.IsAny<PersonTimeZoneId>(), It.IsAny<CancellationToken>()))
      .ReturnsAsync(Result.Ok());

    // Act
    var result = await _sut.SetTimeZoneAsync(timezone);

    // Assert
    result.Should().Contain("Successfully set your timezone");
    result.Should().Contain("America/New_York");
    _personStoreMock.Verify(
      ps => ps.SetTimeZoneAsync(_personId, It.IsAny<PersonTimeZoneId>(), It.IsAny<CancellationToken>()),
      Times.Once);
  }

  [Fact]
  public async Task SetTimeZoneAsyncWithValidAbbreviationCstSucceeds()
  {
    // Arrange
    SetUp();
    const string timezone = "CST";

    _personStoreMock
      .Setup(ps => ps.SetTimeZoneAsync(_personId, It.IsAny<PersonTimeZoneId>(), It.IsAny<CancellationToken>()))
      .ReturnsAsync(Result.Ok());

    // Act
    var result = await _sut.SetTimeZoneAsync(timezone);

    // Assert
    result.Should().Contain("Successfully set your timezone");
    result.Should().Contain("America/Chicago");
    _personStoreMock.Verify(
      ps => ps.SetTimeZoneAsync(_personId, It.IsAny<PersonTimeZoneId>(), It.IsAny<CancellationToken>()),
      Times.Once);
  }

  [Fact]
  public async Task SetTimeZoneAsyncWithInvalidTimezoneFailsGracefully()
  {
    // Arrange
    SetUp();
    const string timezone = "Invalid/Timezone";

    // Act
    var result = await _sut.SetTimeZoneAsync(timezone);

    // Assert
    result.Should().Contain("Failed to set timezone");
    result.Should().Contain("not recognized");
    _personStoreMock.Verify(
      ps => ps.SetTimeZoneAsync(It.IsAny<PersonId>(), It.IsAny<PersonTimeZoneId>(), It.IsAny<CancellationToken>()),
      Times.Never);
  }

  [Fact]
  public async Task SetTimeZoneAsyncWithNullTimezoneFailsGracefully()
  {
    // Arrange
    SetUp();

    // Act
    var result = await _sut.SetTimeZoneAsync(null!);

    // Assert
    result.Should().Contain("Failed to set timezone");
    result.Should().Contain("cannot be empty");
    _personStoreMock.Verify(
      ps => ps.SetTimeZoneAsync(It.IsAny<PersonId>(), It.IsAny<PersonTimeZoneId>(), It.IsAny<CancellationToken>()),
      Times.Never);
  }

  [Fact]
  public async Task SetTimeZoneAsyncWithEmptyTimezoneFailsGracefully()
  {
    // Arrange
    SetUp();

    // Act
    var result = await _sut.SetTimeZoneAsync("");

    // Assert
    result.Should().Contain("Failed to set timezone");
    result.Should().Contain("cannot be empty");
    _personStoreMock.Verify(
      ps => ps.SetTimeZoneAsync(It.IsAny<PersonId>(), It.IsAny<PersonTimeZoneId>(), It.IsAny<CancellationToken>()),
      Times.Never);
  }

  [Fact]
  public async Task SetTimeZoneAsyncWithWhitespaceTimezoneFailsGracefully()
  {
    // Arrange
    SetUp();

    // Act
    var result = await _sut.SetTimeZoneAsync("   ");

    // Assert
    result.Should().Contain("Failed to set timezone");
    result.Should().Contain("cannot be empty");
    _personStoreMock.Verify(
      ps => ps.SetTimeZoneAsync(It.IsAny<PersonId>(), It.IsAny<PersonTimeZoneId>(), It.IsAny<CancellationToken>()),
      Times.Never);
  }

  [Fact]
  public async Task SetTimeZoneAsyncWithStoreFailureReturnsFail()
  {
    // Arrange
    SetUp();
    const string timezone = "UTC";

    _personStoreMock
      .Setup(ps => ps.SetTimeZoneAsync(_personId, It.IsAny<PersonTimeZoneId>(), It.IsAny<CancellationToken>()))
      .ReturnsAsync(Result.Fail("Database error"));

    // Act
    var result = await _sut.SetTimeZoneAsync(timezone);

    // Assert
    result.Should().Contain("Failed to set timezone");
    result.Should().Contain("Database error");
  }

  [Fact]
  public async Task SetTimeZoneAsyncWithExceptionReturnsErrorMessage()
  {
    // Arrange
    SetUp();
    const string timezone = "UTC";

    _personStoreMock
      .Setup(ps => ps.SetTimeZoneAsync(_personId, It.IsAny<PersonTimeZoneId>(), It.IsAny<CancellationToken>()))
      .ThrowsAsync(new InvalidOperationException("System error"));

    // Act
    var result = await _sut.SetTimeZoneAsync(timezone);

    // Assert
    result.Should().Contain("Error setting timezone");
    result.Should().Contain("System error");
  }

  #endregion

  #region Get Timezone Tests

  [Fact]
  public async Task GetTimeZoneAsyncWithCustomTimezoneReturnsCustomValue()
  {
    // Arrange
    SetUp();
    PersonTimeZoneId.TryParse("Europe/London", out var customTimeZone, out _);
    var person = CreateValidPerson(_personId.Value, customTimeZone);

    _personStoreMock
      .Setup(ps => ps.GetAsync(_personId, It.IsAny<CancellationToken>()))
      .ReturnsAsync(Result.Ok(person));

    // Act
    var result = await _sut.GetTimeZoneAsync();

    // Assert
    result.Should().Contain("Your timezone is set to");
    result.Should().Contain("Europe/London");
    _personStoreMock.Verify(
      ps => ps.GetAsync(_personId, It.IsAny<CancellationToken>()),
      Times.Once);
  }

  [Fact]
  public async Task GetTimeZoneAsyncWithoutCustomTimezoneReturnsDefault()
  {
    // Arrange
    SetUp();
    var person = CreateValidPerson(_personId.Value, null);

    _personStoreMock
      .Setup(ps => ps.GetAsync(_personId, It.IsAny<CancellationToken>()))
      .ReturnsAsync(Result.Ok(person));

    // Act
    var result = await _sut.GetTimeZoneAsync();

    // Assert
    result.Should().Contain("You are currently using the default timezone");
    result.Should().Contain(_personConfig.DefaultTimeZoneId);
  }

  [Fact]
  public async Task GetTimeZoneAsyncWithStoreFailureReturnsFail()
  {
    // Arrange
    SetUp();

    _personStoreMock
      .Setup(ps => ps.GetAsync(_personId, It.IsAny<CancellationToken>()))
      .ReturnsAsync(Result.Fail("Person not found"));

    // Act
    var result = await _sut.GetTimeZoneAsync();

    // Assert
    result.Should().Contain("Failed to retrieve timezone");
    result.Should().Contain("Person not found");
  }

  [Fact]
  public async Task GetTimeZoneAsyncWithExceptionReturnsErrorMessage()
  {
    // Arrange
    SetUp();

    _personStoreMock
      .Setup(ps => ps.GetAsync(_personId, It.IsAny<CancellationToken>()))
      .ThrowsAsync(new InvalidOperationException("Database connection failed"));

    // Act
    var result = await _sut.GetTimeZoneAsync();

    // Assert
    result.Should().Contain("Error retrieving timezone");
    result.Should().Contain("Database connection failed");
  }

  #endregion

  #region Set Daily Task Count Tests

  [Fact]
  public async Task SetDailyTaskCountAsyncWithValidCountOneSucceeds()
  {
    // Arrange
    SetUp();
    const int count = 1;

    _personStoreMock
      .Setup(ps => ps.SetDailyTaskCountAsync(_personId, It.IsAny<DailyTaskCount>(), It.IsAny<CancellationToken>()))
      .ReturnsAsync(Result.Ok());

    // Act
    var result = await _sut.SetDailyTaskCountAsync(count);

    // Assert
    result.Should().Contain("Successfully set your daily task count");
    result.Should().Contain("1 tasks");
    _personStoreMock.Verify(
      ps => ps.SetDailyTaskCountAsync(_personId, It.IsAny<DailyTaskCount>(), It.IsAny<CancellationToken>()),
      Times.Once);
  }

  [Fact]
  public async Task SetDailyTaskCountAsyncWithValidCountTenSucceeds()
  {
    // Arrange
    SetUp();
    const int count = 10;

    _personStoreMock
      .Setup(ps => ps.SetDailyTaskCountAsync(_personId, It.IsAny<DailyTaskCount>(), It.IsAny<CancellationToken>()))
      .ReturnsAsync(Result.Ok());

    // Act
    var result = await _sut.SetDailyTaskCountAsync(count);

    // Assert
    result.Should().Contain("Successfully set your daily task count");
    result.Should().Contain("10 tasks");
    _personStoreMock.Verify(
      ps => ps.SetDailyTaskCountAsync(_personId, It.IsAny<DailyTaskCount>(), It.IsAny<CancellationToken>()),
      Times.Once);
  }

  [Fact]
  public async Task SetDailyTaskCountAsyncWithValidCountTwentySucceeds()
  {
    // Arrange
    SetUp();
    const int count = 20;

    _personStoreMock
      .Setup(ps => ps.SetDailyTaskCountAsync(_personId, It.IsAny<DailyTaskCount>(), It.IsAny<CancellationToken>()))
      .ReturnsAsync(Result.Ok());

    // Act
    var result = await _sut.SetDailyTaskCountAsync(count);

    // Assert
    result.Should().Contain("Successfully set your daily task count");
    result.Should().Contain("20 tasks");
    _personStoreMock.Verify(
      ps => ps.SetDailyTaskCountAsync(_personId, It.IsAny<DailyTaskCount>(), It.IsAny<CancellationToken>()),
      Times.Once);
  }

  [Fact]
  public async Task SetDailyTaskCountAsyncWithCountZeroFailsGracefully()
  {
    // Arrange
    SetUp();
    const int count = 0;

    // Act
    var result = await _sut.SetDailyTaskCountAsync(count);

    // Assert
    result.Should().Contain("Failed to set daily task count");
    result.Should().Contain("between 1 and 20");
    _personStoreMock.Verify(
      ps => ps.SetDailyTaskCountAsync(It.IsAny<PersonId>(), It.IsAny<DailyTaskCount>(), It.IsAny<CancellationToken>()),
      Times.Never);
  }

  [Fact]
  public async Task SetDailyTaskCountAsyncWithCountNegativeFailsGracefully()
  {
    // Arrange
    SetUp();
    const int count = -5;

    // Act
    var result = await _sut.SetDailyTaskCountAsync(count);

    // Assert
    result.Should().Contain("Failed to set daily task count");
    result.Should().Contain("between 1 and 20");
    _personStoreMock.Verify(
      ps => ps.SetDailyTaskCountAsync(It.IsAny<PersonId>(), It.IsAny<DailyTaskCount>(), It.IsAny<CancellationToken>()),
      Times.Never);
  }

  [Fact]
  public async Task SetDailyTaskCountAsyncWithCountTwentyOneFailsGracefully()
  {
    // Arrange
    SetUp();
    const int count = 21;

    // Act
    var result = await _sut.SetDailyTaskCountAsync(count);

    // Assert
    result.Should().Contain("Failed to set daily task count");
    result.Should().Contain("between 1 and 20");
    _personStoreMock.Verify(
      ps => ps.SetDailyTaskCountAsync(It.IsAny<PersonId>(), It.IsAny<DailyTaskCount>(), It.IsAny<CancellationToken>()),
      Times.Never);
  }

  [Fact]
  public async Task SetDailyTaskCountAsyncWithCountExceedsMaxFailsGracefully()
  {
    // Arrange
    SetUp();
    const int count = 100;

    // Act
    var result = await _sut.SetDailyTaskCountAsync(count);

    // Assert
    result.Should().Contain("Failed to set daily task count");
    result.Should().Contain("between 1 and 20");
    _personStoreMock.Verify(
      ps => ps.SetDailyTaskCountAsync(It.IsAny<PersonId>(), It.IsAny<DailyTaskCount>(), It.IsAny<CancellationToken>()),
      Times.Never);
  }

  [Fact]
  public async Task SetDailyTaskCountAsyncWithStoreFailureReturnsFail()
  {
    // Arrange
    SetUp();
    const int count = 5;

    _personStoreMock
      .Setup(ps => ps.SetDailyTaskCountAsync(_personId, It.IsAny<DailyTaskCount>(), It.IsAny<CancellationToken>()))
      .ReturnsAsync(Result.Fail("Database error"));

    // Act
    var result = await _sut.SetDailyTaskCountAsync(count);

    // Assert
    result.Should().Contain("Failed to set daily task count");
    result.Should().Contain("Database error");
  }

  [Fact]
  public async Task SetDailyTaskCountAsyncWithExceptionReturnsErrorMessage()
  {
    // Arrange
    SetUp();
    const int count = 5;

    _personStoreMock
      .Setup(ps => ps.SetDailyTaskCountAsync(_personId, It.IsAny<DailyTaskCount>(), It.IsAny<CancellationToken>()))
      .ThrowsAsync(new InvalidOperationException("System error"));

    // Act
    var result = await _sut.SetDailyTaskCountAsync(count);

    // Assert
    result.Should().Contain("Error setting daily task count");
    result.Should().Contain("System error");
  }

  #endregion

  #region Get Daily Task Count Tests

  [Fact]
  public async Task GetDailyTaskCountAsyncWithCustomCountReturnsCustomValue()
  {
    // Arrange
    SetUp();
    var customCount = new DailyTaskCount(15);
    var person = CreateValidPerson(_personId.Value, null, customCount);

    _personStoreMock
      .Setup(ps => ps.GetAsync(_personId, It.IsAny<CancellationToken>()))
      .ReturnsAsync(Result.Ok(person));

    // Act
    var result = await _sut.GetDailyTaskCountAsync();

    // Assert
    result.Should().Contain("Your daily task count is set to");
    result.Should().Contain("15 tasks per day");
    _personStoreMock.Verify(
      ps => ps.GetAsync(_personId, It.IsAny<CancellationToken>()),
      Times.Once);
  }

  [Fact]
  public async Task GetDailyTaskCountAsyncWithoutCustomCountReturnsDefault()
  {
    // Arrange
    SetUp();
    var person = CreateValidPerson(_personId.Value, null, null);

    _personStoreMock
      .Setup(ps => ps.GetAsync(_personId, It.IsAny<CancellationToken>()))
      .ReturnsAsync(Result.Ok(person));

    // Act
    var result = await _sut.GetDailyTaskCountAsync();

    // Assert
    result.Should().Contain("You are currently using the default daily task count");
    result.Should().Contain("5 tasks per day");
  }

  [Fact]
  public async Task GetDailyTaskCountAsyncWithStoreFailureReturnsFail()
  {
    // Arrange
    SetUp();

    _personStoreMock
      .Setup(ps => ps.GetAsync(_personId, It.IsAny<CancellationToken>()))
      .ReturnsAsync(Result.Fail("Person not found"));

    // Act
    var result = await _sut.GetDailyTaskCountAsync();

    // Assert
    result.Should().Contain("Failed to retrieve daily task count");
    result.Should().Contain("Person not found");
  }

  [Fact]
  public async Task GetDailyTaskCountAsyncWithExceptionReturnsErrorMessage()
  {
    // Arrange
    SetUp();

    _personStoreMock
      .Setup(ps => ps.GetAsync(_personId, It.IsAny<CancellationToken>()))
      .ThrowsAsync(new InvalidOperationException("Database connection failed"));

    // Act
    var result = await _sut.GetDailyTaskCountAsync();

    // Assert
    result.Should().Contain("Error retrieving daily task count");
    result.Should().Contain("Database connection failed");
  }

  #endregion
}
