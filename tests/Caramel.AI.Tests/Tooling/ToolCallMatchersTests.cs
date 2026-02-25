using Caramel.AI.Tooling;

namespace Caramel.AI.Tests.Tooling;

public class ToolCallMatchersTests
{
  [Fact]
  public void IsSetTimezoneReturnsTrueForMatchingPluginAndFunction()
  {
    var result = ToolCallMatchers.IsSetTimezone("Person", "set_timezone");

    Assert.True(result);
  }

  [Fact]
  public void IsSetTimezoneIsCaseInsensitive()
  {
    var result = ToolCallMatchers.IsSetTimezone("PERSON", "SET_TIMEZONE");

    Assert.True(result);
  }

  [Fact]
  public void IsSetTimezoneReturnsFalseForWrongPlugin()
  {
    var result = ToolCallMatchers.IsSetTimezone("Other", "set_timezone");

    Assert.False(result);
  }

  [Fact]
  public void IsSetTimezoneReturnsFalseForWrongFunction()
  {
    var result = ToolCallMatchers.IsSetTimezone("Person", "get_timezone");

    Assert.False(result);
  }

  [Fact]
  public void IsSetTimezoneReturnsFalseForNullValues()
  {
    Assert.False(ToolCallMatchers.IsSetTimezone(null, "set_timezone"));
    Assert.False(ToolCallMatchers.IsSetTimezone("Person", null));
    Assert.False(ToolCallMatchers.IsSetTimezone(null, null));
  }

  [Fact]
  public void ConstantsHaveExpectedValues()
  {
    Assert.Equal("Person", ToolCallMatchers.PersonPluginName);
    Assert.Equal("set_timezone", ToolCallMatchers.SetTimezoneFunction);
    Assert.Equal(5, ToolCallMatchers.MaxToolCalls);
    Assert.Equal(3, ToolCallMatchers.MaxConsecutiveRepeats);
  }
}
