using Caramel.Core.Notifications;
using Caramel.Domain.Common.Enums;
using Caramel.Domain.People.Models;
using Caramel.Domain.People.ValueObjects;

using FluentAssertions;

namespace Caramel.Notifications.Tests;

public class NoOpPersonNotificationClientTests
{
  [Fact]
  public async Task SendNotificationAsyncReturnsSuccessAsync()
  {
    // Arrange
    var client = new NoOpPersonNotificationClient();
    var person = new Person
    {
      Id = new PersonId(Guid.NewGuid()),
      PlatformId = new PlatformId("testuser", "123", Platform.Discord),
      Username = new Username("testuser"),
      HasAccess = new HasAccess(true),
      NotificationChannels = [],
      CreatedOn = new Domain.Common.ValueObjects.CreatedOn(DateTime.UtcNow),
      UpdatedOn = new Domain.Common.ValueObjects.UpdatedOn(DateTime.UtcNow)
    };
    var notification = new Notification { Content = "Test message" };

    // Act
    var result = await client.SendNotificationAsync(person, notification);

    // Assert
    _ = result.IsSuccess.Should().BeTrue();
  }
}
