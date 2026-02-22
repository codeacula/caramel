using Caramel.Domain.Common.Enums;
using Caramel.Domain.ToDos.ValueObjects;

namespace Caramel.Domain.Tests.ToDos.ValueObjects;

public class InterestTests
{
  [Fact]
  public void InterestStoresValue()
  {
    // Arrange & Act
    var interest = new Interest(Level.Green);

    // Assert
    Assert.Equal(Level.Green, interest.Value);
  }

  [Fact]
  public void InterestEqualityWorksCorrectly()
  {
    // Arrange
    var interest1 = new Interest(Level.Yellow);
    var interest2 = new Interest(Level.Yellow);
    var interest3 = new Interest(Level.Blue);

    // Act & Assert
    Assert.Equal(interest1, interest2);
    Assert.NotEqual(interest1, interest3);
  }
}
