using Caramel.Core.Logging;
using Caramel.Core.Notifications;
using Caramel.Core.People;
using Caramel.Core.ToDos;
using Caramel.Domain.Common.Enums;
using Caramel.Domain.Common.ValueObjects;
using Caramel.Domain.People.Models;
using Caramel.Domain.People.ValueObjects;
using Caramel.Domain.ToDos.Models;
using Caramel.Domain.ToDos.ValueObjects;
using Caramel.Service.Jobs;

using FluentResults;

using Microsoft.Extensions.Logging;

using Moq;

using Quartz;

namespace Caramel.Service.Tests.Jobs;

/// <summary>
/// Tests for ToDoReminderJob - Quartz scheduled task execution
/// Tests cover successful reminder delivery and failure handling
/// </summary>
public class ToDoReminderJobTests
{
  private readonly Mock<IReminderStore> _reminderStoreMock = new();
  private readonly Mock<IPersonStore> _personStoreMock = new();
  private readonly Mock<IPersonNotificationClient> _notificationClientMock = new();
  private readonly Mock<IReminderMessageGenerator> _messageGeneratorMock = new();
  private readonly Mock<ILogger<ToDoReminderJob>> _loggerMock = new();
  private readonly Mock<TimeProvider> _timeProviderMock = new();
  private readonly Mock<IScheduler> _schedulerMock = new();
  private ToDoReminderJob _sut = null!;

  private void SetUp()
  {
    _sut = new ToDoReminderJob(
      _reminderStoreMock.Object,
      _personStoreMock.Object,
      _notificationClientMock.Object,
      _messageGeneratorMock.Object,
      _loggerMock.Object,
      _timeProviderMock.Object
    );
  }

  #region Helper Methods

  private Reminder CreateValidReminder(PersonId? personId = null, Guid? reminderId = null)
  {
    return new()
    {
      Id = new(reminderId ?? Guid.NewGuid()),
      PersonId = personId ?? new(Guid.NewGuid()),
      Details = new("Test reminder"),
      ReminderTime = new(DateTime.UtcNow.AddHours(1)),
      QuartzJobId = new(Guid.NewGuid()),
      CreatedOn = new(DateTime.UtcNow),
      UpdatedOn = new(DateTime.UtcNow),
      AcknowledgedOn = null
    };
  }

  private Person CreateValidPerson(Guid? personId = null)
  {
    return new()
    {
      Id = new(personId ?? Guid.NewGuid()),
      PlatformId = new("testuser", "123456", Platform.Discord),
      Username = new("testuser"),
      HasAccess = new(true),
      NotificationChannels = [],
      CreatedOn = new(DateTime.UtcNow),
      UpdatedOn = new(DateTime.UtcNow)
    };
  }

  private IJobExecutionContext CreateJobContext(string jobId)
  {
    var jobKey = new JobKey(jobId);
    var jobDetail = new Mock<IJobDetail>();
    jobDetail.Setup(j => j.Key).Returns(jobKey);

    var contextMock = new Mock<IJobExecutionContext>();
    contextMock.Setup(c => c.JobDetail).Returns(jobDetail.Object);
    contextMock.Setup(c => c.Scheduler).Returns(_schedulerMock.Object);
    contextMock.Setup(c => c.CancellationToken).Returns(CancellationToken.None);

    return contextMock.Object;
  }

  #endregion

  #region Success Path Tests

  [Fact]
  public async Task ExecuteWithValidReminderSendsNotification()
  {
    // Arrange
    SetUp();
    var reminderId = Guid.NewGuid();
    var personId = Guid.NewGuid();
    var reminder = CreateValidReminder(new(personId), reminderId);
    var person = CreateValidPerson(personId);
    var context = CreateJobContext(reminderId.ToString());

    _timeProviderMock.Setup(tp => tp.GetUtcNow()).Returns(new DateTimeOffset(DateTime.UtcNow));
    _reminderStoreMock
      .Setup(rs => rs.GetByQuartzJobIdAsync(It.IsAny<QuartzJobId>(), It.IsAny<CancellationToken>()))
      .ReturnsAsync(Result.Ok(new List<Reminder> { reminder }.AsEnumerable()));
    _personStoreMock
      .Setup(ps => ps.GetAsync(new(personId), It.IsAny<CancellationToken>()))
      .ReturnsAsync(Result.Ok(person));
    _messageGeneratorMock
      .Setup(mg => mg.GenerateReminderMessageAsync(person, It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
      .ReturnsAsync(Result.Ok("Generated reminder message"));
    _notificationClientMock
      .Setup(nc => nc.SendNotificationAsync(person, It.IsAny<Notification>(), It.IsAny<CancellationToken>()))
      .ReturnsAsync(Result.Ok());
    _reminderStoreMock
      .Setup(rs => rs.MarkAsSentAsync(reminder.Id, It.IsAny<CancellationToken>()))
      .ReturnsAsync(Result.Ok());
    _schedulerMock
      .Setup(s => s.DeleteJob(It.IsAny<JobKey>(), It.IsAny<CancellationToken>()))
      .ReturnsAsync(true);

    // Act
    await _sut.Execute(context);

    // Assert
    _reminderStoreMock.Verify(rs => rs.GetByQuartzJobIdAsync(It.IsAny<QuartzJobId>(), It.IsAny<CancellationToken>()), Times.Once);
    _notificationClientMock.Verify(nc => nc.SendNotificationAsync(It.IsAny<Person>(), It.IsAny<Notification>(), It.IsAny<CancellationToken>()), Times.Once);
    _schedulerMock.Verify(s => s.DeleteJob(It.IsAny<JobKey>(), It.IsAny<CancellationToken>()), Times.Once);
  }

  [Fact]
  public async Task ExecuteWithMultipleRemindersGroupsByPerson()
  {
    // Arrange
    SetUp();
    var reminderId1 = Guid.NewGuid();
    var reminderId2 = Guid.NewGuid();
    var personId = Guid.NewGuid();
    var reminder1 = CreateValidReminder(new(personId), reminderId1);
    var reminder2 = CreateValidReminder(new(personId), reminderId2);
    var person = CreateValidPerson(personId);
    var context = CreateJobContext(reminderId1.ToString());

    _timeProviderMock.Setup(tp => tp.GetUtcNow()).Returns(new DateTimeOffset(DateTime.UtcNow));
    _reminderStoreMock
      .Setup(rs => rs.GetByQuartzJobIdAsync(It.IsAny<QuartzJobId>(), It.IsAny<CancellationToken>()))
      .ReturnsAsync(Result.Ok(new List<Reminder> { reminder1, reminder2 }.AsEnumerable()));
    _personStoreMock
      .Setup(ps => ps.GetAsync(It.IsAny<PersonId>(), It.IsAny<CancellationToken>()))
      .ReturnsAsync(Result.Ok(person));
    _messageGeneratorMock
      .Setup(mg => mg.GenerateReminderMessageAsync(person, It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
      .ReturnsAsync(Result.Ok("Generated reminder message"));
    _notificationClientMock
      .Setup(nc => nc.SendNotificationAsync(person, It.IsAny<Notification>(), It.IsAny<CancellationToken>()))
      .ReturnsAsync(Result.Ok());
    _reminderStoreMock
      .Setup(rs => rs.MarkAsSentAsync(It.IsAny<ReminderId>(), It.IsAny<CancellationToken>()))
      .ReturnsAsync(Result.Ok());
    _schedulerMock
      .Setup(s => s.DeleteJob(It.IsAny<JobKey>(), It.IsAny<CancellationToken>()))
      .ReturnsAsync(true);

    // Act
    await _sut.Execute(context);

    // Assert
    // Should send only one notification for both reminders
    _notificationClientMock.Verify(nc => nc.SendNotificationAsync(It.IsAny<Person>(), It.IsAny<Notification>(), It.IsAny<CancellationToken>()), Times.Once);
    // Should mark both as sent
    _reminderStoreMock.Verify(rs => rs.MarkAsSentAsync(It.IsAny<ReminderId>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
  }

  [Fact]
  public async Task ExecuteWithEmptyReminderListCompletes()
  {
    // Arrange
    SetUp();
    var context = CreateJobContext(Guid.NewGuid().ToString());

    _timeProviderMock.Setup(tp => tp.GetUtcNow()).Returns(new DateTimeOffset(DateTime.UtcNow));
    _reminderStoreMock
      .Setup(rs => rs.GetByQuartzJobIdAsync(It.IsAny<QuartzJobId>(), It.IsAny<CancellationToken>()))
      .ReturnsAsync(Result.Ok(new List<Reminder>().AsEnumerable()));
    _schedulerMock
      .Setup(s => s.DeleteJob(It.IsAny<JobKey>(), It.IsAny<CancellationToken>()))
      .ReturnsAsync(true);

    // Act
    await _sut.Execute(context);

    // Assert
    _schedulerMock.Verify(s => s.DeleteJob(It.IsAny<JobKey>(), It.IsAny<CancellationToken>()), Times.Once);
  }

  #endregion

  #region Failure Handling Tests

  [Fact]
  public async Task ExecuteWithInvalidJobIdLogsError()
  {
    // Arrange
    SetUp();
    var context = CreateJobContext("not-a-guid");

    // Act
    await _sut.Execute(context);

    // Assert
    _reminderStoreMock.Verify(rs => rs.GetByQuartzJobIdAsync(It.IsAny<QuartzJobId>(), It.IsAny<CancellationToken>()), Times.Never);
  }

  [Fact]
  public async Task ExecuteWithReminderStoreFailureSkipsProcessing()
  {
    // Arrange
    SetUp();
    var context = CreateJobContext(Guid.NewGuid().ToString());

    _timeProviderMock.Setup(tp => tp.GetUtcNow()).Returns(new DateTimeOffset(DateTime.UtcNow));
    _reminderStoreMock
      .Setup(rs => rs.GetByQuartzJobIdAsync(It.IsAny<QuartzJobId>(), It.IsAny<CancellationToken>()))
      .ReturnsAsync(Result.Fail<IEnumerable<Reminder>>("Database error"));

    // Act
    await _sut.Execute(context);

    // Assert
    _personStoreMock.Verify(ps => ps.GetAsync(It.IsAny<PersonId>(), It.IsAny<CancellationToken>()), Times.Never);
  }

  [Fact]
  public async Task ExecuteWithPersonNotFoundSkipsThatPerson()
  {
    // Arrange
    SetUp();
    var personId1 = Guid.NewGuid();
    var personId2 = Guid.NewGuid();
    var reminder1 = CreateValidReminder(new(personId1));
    var reminder2 = CreateValidReminder(new(personId2));
    var person2 = CreateValidPerson(personId2);
    var context = CreateJobContext(Guid.NewGuid().ToString());

    _timeProviderMock.Setup(tp => tp.GetUtcNow()).Returns(new DateTimeOffset(DateTime.UtcNow));
    _reminderStoreMock
      .Setup(rs => rs.GetByQuartzJobIdAsync(It.IsAny<QuartzJobId>(), It.IsAny<CancellationToken>()))
      .ReturnsAsync(Result.Ok(new List<Reminder> { reminder1, reminder2 }.AsEnumerable()));
    _personStoreMock
      .Setup(ps => ps.GetAsync(new(personId1), It.IsAny<CancellationToken>()))
      .ReturnsAsync(Result.Fail<Person>("Not found"));
    _personStoreMock
      .Setup(ps => ps.GetAsync(new(personId2), It.IsAny<CancellationToken>()))
      .ReturnsAsync(Result.Ok(person2));
    _messageGeneratorMock
      .Setup(mg => mg.GenerateReminderMessageAsync(person2, It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
      .ReturnsAsync(Result.Ok("Message"));
    _notificationClientMock
      .Setup(nc => nc.SendNotificationAsync(person2, It.IsAny<Notification>(), It.IsAny<CancellationToken>()))
      .ReturnsAsync(Result.Ok());
    _reminderStoreMock
      .Setup(rs => rs.MarkAsSentAsync(reminder2.Id, It.IsAny<CancellationToken>()))
      .ReturnsAsync(Result.Ok());
    _schedulerMock
      .Setup(s => s.DeleteJob(It.IsAny<JobKey>(), It.IsAny<CancellationToken>()))
      .ReturnsAsync(true);

    // Act
    await _sut.Execute(context);

    // Assert
    _notificationClientMock.Verify(nc => nc.SendNotificationAsync(person2, It.IsAny<Notification>(), It.IsAny<CancellationToken>()), Times.Once);
    _reminderStoreMock.Verify(rs => rs.MarkAsSentAsync(reminder2.Id, It.IsAny<CancellationToken>()), Times.Once);
  }

  [Fact]
  public async Task ExecuteWithMessageGenerationFailureUsesFallback()
  {
    // Arrange
    SetUp();
    var reminder = CreateValidReminder();
    var person = CreateValidPerson(reminder.PersonId.Value);
    var context = CreateJobContext(Guid.NewGuid().ToString());

    _timeProviderMock.Setup(tp => tp.GetUtcNow()).Returns(new DateTimeOffset(DateTime.UtcNow));
    _reminderStoreMock
      .Setup(rs => rs.GetByQuartzJobIdAsync(It.IsAny<QuartzJobId>(), It.IsAny<CancellationToken>()))
      .ReturnsAsync(Result.Ok(new List<Reminder> { reminder }.AsEnumerable()));
    _personStoreMock
      .Setup(ps => ps.GetAsync(It.IsAny<PersonId>(), It.IsAny<CancellationToken>()))
      .ReturnsAsync(Result.Ok(person));
    _messageGeneratorMock
      .Setup(mg => mg.GenerateReminderMessageAsync(person, It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
      .ReturnsAsync(Result.Fail<string>("AI service error"));
    _notificationClientMock
      .Setup(nc => nc.SendNotificationAsync(person, It.IsAny<Notification>(), It.IsAny<CancellationToken>()))
      .ReturnsAsync(Result.Ok());
    _reminderStoreMock
      .Setup(rs => rs.MarkAsSentAsync(reminder.Id, It.IsAny<CancellationToken>()))
      .ReturnsAsync(Result.Ok());
    _schedulerMock
      .Setup(s => s.DeleteJob(It.IsAny<JobKey>(), It.IsAny<CancellationToken>()))
      .ReturnsAsync(true);

    // Act
    await _sut.Execute(context);

    // Assert
    _notificationClientMock.Verify(nc => nc.SendNotificationAsync(person, It.IsAny<Notification>(), It.IsAny<CancellationToken>()), Times.Once);
  }

  [Fact]
  public async Task ExecuteWithNotificationFailureContinuesToNextPerson()
  {
    // Arrange
    SetUp();
    var personId1 = Guid.NewGuid();
    var personId2 = Guid.NewGuid();
    var reminder1 = CreateValidReminder(new(personId1));
    var reminder2 = CreateValidReminder(new(personId2));
    var person1 = CreateValidPerson(personId1);
    var person2 = CreateValidPerson(personId2);
    var context = CreateJobContext(Guid.NewGuid().ToString());

    _timeProviderMock.Setup(tp => tp.GetUtcNow()).Returns(new DateTimeOffset(DateTime.UtcNow));
    _reminderStoreMock
      .Setup(rs => rs.GetByQuartzJobIdAsync(It.IsAny<QuartzJobId>(), It.IsAny<CancellationToken>()))
      .ReturnsAsync(Result.Ok(new List<Reminder> { reminder1, reminder2 }.AsEnumerable()));
    _personStoreMock
      .Setup(ps => ps.GetAsync(new(personId1), It.IsAny<CancellationToken>()))
      .ReturnsAsync(Result.Ok(person1));
    _personStoreMock
      .Setup(ps => ps.GetAsync(new(personId2), It.IsAny<CancellationToken>()))
      .ReturnsAsync(Result.Ok(person2));
    _messageGeneratorMock
      .Setup(mg => mg.GenerateReminderMessageAsync(It.IsAny<Person>(), It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
      .ReturnsAsync(Result.Ok("Message"));
    _notificationClientMock
      .Setup(nc => nc.SendNotificationAsync(person1, It.IsAny<Notification>(), It.IsAny<CancellationToken>()))
      .ReturnsAsync(Result.Fail("Send failed"));
    _notificationClientMock
      .Setup(nc => nc.SendNotificationAsync(person2, It.IsAny<Notification>(), It.IsAny<CancellationToken>()))
      .ReturnsAsync(Result.Ok());
    _reminderStoreMock
      .Setup(rs => rs.MarkAsSentAsync(reminder2.Id, It.IsAny<CancellationToken>()))
      .ReturnsAsync(Result.Ok());
    _schedulerMock
      .Setup(s => s.DeleteJob(It.IsAny<JobKey>(), It.IsAny<CancellationToken>()))
      .ReturnsAsync(true);

    // Act
    await _sut.Execute(context);

    // Assert
    _reminderStoreMock.Verify(rs => rs.MarkAsSentAsync(reminder2.Id, It.IsAny<CancellationToken>()), Times.Once);
  }

  [Fact]
  public async Task ExecuteDeletesJobAfterProcessing()
  {
    // Arrange
    SetUp();
    var reminder = CreateValidReminder();
    var person = CreateValidPerson(reminder.PersonId.Value);
    var context = CreateJobContext(Guid.NewGuid().ToString());

    _timeProviderMock.Setup(tp => tp.GetUtcNow()).Returns(new DateTimeOffset(DateTime.UtcNow));
    _reminderStoreMock
      .Setup(rs => rs.GetByQuartzJobIdAsync(It.IsAny<QuartzJobId>(), It.IsAny<CancellationToken>()))
      .ReturnsAsync(Result.Ok(new List<Reminder> { reminder }.AsEnumerable()));
    _personStoreMock
      .Setup(ps => ps.GetAsync(It.IsAny<PersonId>(), It.IsAny<CancellationToken>()))
      .ReturnsAsync(Result.Ok(person));
    _messageGeneratorMock
      .Setup(mg => mg.GenerateReminderMessageAsync(person, It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
      .ReturnsAsync(Result.Ok("Message"));
    _notificationClientMock
      .Setup(nc => nc.SendNotificationAsync(person, It.IsAny<Notification>(), It.IsAny<CancellationToken>()))
      .ReturnsAsync(Result.Ok());
    _reminderStoreMock
      .Setup(rs => rs.MarkAsSentAsync(reminder.Id, It.IsAny<CancellationToken>()))
      .ReturnsAsync(Result.Ok());
    _schedulerMock
      .Setup(s => s.DeleteJob(It.IsAny<JobKey>(), It.IsAny<CancellationToken>()))
      .ReturnsAsync(true);

    // Act
    await _sut.Execute(context);

    // Assert
    _schedulerMock.Verify(s => s.DeleteJob(It.IsAny<JobKey>(), It.IsAny<CancellationToken>()), Times.Once);
  }

  [Fact]
  public async Task ExecuteHandlesExceptionGracefully()
  {
    // Arrange
    SetUp();
    var context = CreateJobContext(Guid.NewGuid().ToString());

    _timeProviderMock.Setup(tp => tp.GetUtcNow()).Throws<InvalidOperationException>();

    // Act & Assert - Should not throw
    await _sut.Execute(context);
  }

  #endregion
}
