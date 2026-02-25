namespace Caramel.AI.Tooling;

/// <summary>
/// Centralized tool call matching logic and constants used across AI components.
/// Consolidates common validation patterns to prevent duplication.
/// </summary>
public static class ToolCallMatchers
{
  /// <summary>Plugin names</summary>
  public const string PersonPluginName = "Person";

  /// <summary>Function names</summary>
  public const string SetTimezoneFunction = "set_timezone";

  /// <summary>Constraints</summary>
  public const int MaxToolCalls = 5;
  public const int MaxConsecutiveRepeats = 3;

  public static bool IsSetTimezone(string? pluginName, string? functionName)
  {
    return string.Equals(pluginName, PersonPluginName, StringComparison.OrdinalIgnoreCase)
      && string.Equals(functionName, SetTimezoneFunction, StringComparison.OrdinalIgnoreCase);
  }
}
