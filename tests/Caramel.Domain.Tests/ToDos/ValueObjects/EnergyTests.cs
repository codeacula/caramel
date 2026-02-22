using Caramel.Domain.Common.Enums;
using Caramel.Domain.ToDos.ValueObjects;

namespace Caramel.Domain.Tests.ToDos.ValueObjects;

public class EnergyTests
{
  [Fact]
  public void EnergyStoresValue()
  {
    // Arrange & Act
    var energy = new Energy(Level.Red);

    // Assert
    Assert.Equal(Level.Red, energy.Value);
  }

  [Fact]
  public void EnergyEqualityWorksCorrectly()
  {
    // Arrange
    var energy1 = new Energy(Level.Yellow);
    var energy2 = new Energy(Level.Yellow);
    var energy3 = new Energy(Level.Blue);

    // Act & Assert
    Assert.Equal(energy1, energy2);
    Assert.NotEqual(energy1, energy3);
  }
}
