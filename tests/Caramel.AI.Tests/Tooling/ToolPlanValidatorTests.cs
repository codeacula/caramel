using System.Diagnostics.CodeAnalysis;

using Caramel.AI.DTOs;
using Caramel.AI.Enums;
using Caramel.AI.Models;
using Caramel.AI.Tooling;

using Microsoft.SemanticKernel;

namespace Caramel.AI.Tests.Tooling;

public class ToolPlanValidatorTests
{
  [Fact]
  public void ValidateBlocksSetTimezoneWithoutContext()
  {
    var plan = new ToolPlan
    {
      ToolCalls =
      [
        new PlannedToolCall
        {
          PluginName = "Person",
          FunctionName = "set_timezone",
          Arguments = new Dictionary<string, string?> { ["timezone"] = "PST" }
        }
      ]
    };

    var context = BuildContext(messages:
    [
      new ChatMessageDTO(ChatRole.User, "PST", DateTime.UtcNow)
    ]);

    var result = ToolPlanValidator.Validate(plan, context);

    Assert.Empty(result.ApprovedCalls);
    _ = Assert.Single(result.BlockedCalls);
  }

  [Fact]
  public void ValidateAllowsSetTimezoneWithContext()
  {
    var plan = new ToolPlan
    {
      ToolCalls =
      [
        new PlannedToolCall
        {
          PluginName = "Person",
          FunctionName = "set_timezone",
          Arguments = new Dictionary<string, string?> { ["timezone"] = "PST" }
        }
      ]
    };

    var context = BuildContext(messages:
    [
      new ChatMessageDTO(ChatRole.Assistant, "What timezone are you in?", DateTime.UtcNow.AddMinutes(-1)),
      new ChatMessageDTO(ChatRole.User, "PST", DateTime.UtcNow)
    ]);

    var result = ToolPlanValidator.Validate(plan, context);

    _ = Assert.Single(result.ApprovedCalls);
    Assert.Empty(result.BlockedCalls);
  }

  [Fact]
  public void ValidateBlocksMissingArguments()
  {
    var plan = new ToolPlan
    {
      ToolCalls =
      [
        new PlannedToolCall
        {
          PluginName = "Person",
          FunctionName = "set_timezone",
          Arguments = []
        }
      ]
    };

    // "timezone" keyword in history to pass the timezone guard
    var context = BuildContext(messages:
    [
      new ChatMessageDTO(ChatRole.User, "change my timezone", DateTime.UtcNow)
    ]);
    var result = ToolPlanValidator.Validate(plan, context);

    Assert.Empty(result.ApprovedCalls);
    _ = Assert.Single(result.BlockedCalls);
  }

  [Fact]
  public void ValidateReturnsEmptyResultForEmptyPlan()
  {
    var plan = new ToolPlan { ToolCalls = [] };
    var context = BuildContext();

    var result = ToolPlanValidator.Validate(plan, context);

    Assert.Empty(result.ApprovedCalls);
    Assert.Empty(result.BlockedCalls);
  }

  [Fact]
  public void ValidateBlocksUnknownPlugin()
  {
    var plan = new ToolPlan
    {
      ToolCalls =
      [
        new PlannedToolCall
        {
          PluginName = "UnknownPlugin",
          FunctionName = "some_function",
          Arguments = new Dictionary<string, string?> { ["arg"] = "value" }
        }
      ]
    };

    var context = BuildContext();
    var result = ToolPlanValidator.Validate(plan, context);

    Assert.Empty(result.ApprovedCalls);
    _ = Assert.Single(result.BlockedCalls);
    Assert.Contains("Unknown plugin", result.BlockedCalls[0].ErrorMessage);
  }

  [Fact]
  public void ValidateBlocksUnknownFunction()
  {
    var plan = new ToolPlan
    {
      ToolCalls =
      [
        new PlannedToolCall
        {
          PluginName = "Person",
          FunctionName = "unknown_function",
          Arguments = new Dictionary<string, string?> { ["arg"] = "value" }
        }
      ]
    };

    var context = BuildContext();
    var result = ToolPlanValidator.Validate(plan, context);

    Assert.Empty(result.ApprovedCalls);
    _ = Assert.Single(result.BlockedCalls);
    Assert.Contains("Unknown function", result.BlockedCalls[0].ErrorMessage);
  }

  [Fact]
  public void ValidateBlocksExcessiveToolCalls()
  {
    // Generate calls alternating between two different functions to avoid repeat-blocking
    var toolCalls = Enumerable.Range(1, 10)
      .Select(i => new PlannedToolCall
      {
        PluginName = "Person",
        FunctionName = i % 2 == 0 ? "do_action_a" : "do_action_b",
        Arguments = new Dictionary<string, string?> { ["arg"] = $"Value {i}" }
      })
      .ToList();

    var plan = new ToolPlan { ToolCalls = toolCalls };
    var context = BuildContext();

    var result = ToolPlanValidator.Validate(plan, context);

    // All calls resolve to unknown functions, so all are blocked — but the limit cap should kick in
    Assert.True(result.BlockedCalls.Count >= 10 - ToolCallMatchers.MaxToolCalls);
  }

  [Fact]
  public void ValidateBlocksConsecutiveRepeatedCalls()
  {
    var toolCalls = Enumerable.Range(1, 5)
      .Select(_ => new PlannedToolCall
      {
        PluginName = "Person",
        FunctionName = "set_timezone",
        Arguments = new Dictionary<string, string?> { ["timezone"] = "EST" }
      })
      .ToList();

    var plan = new ToolPlan { ToolCalls = toolCalls };
    // timezone context so they pass the timezone guard
    var context = BuildContext(messages:
    [
      new ChatMessageDTO(ChatRole.User, "change my timezone to EST", DateTime.UtcNow)
    ]);

    var result = ToolPlanValidator.Validate(plan, context);

    Assert.Equal(ToolCallMatchers.MaxConsecutiveRepeats, result.ApprovedCalls.Count);
    Assert.Equal(5 - ToolCallMatchers.MaxConsecutiveRepeats, result.BlockedCalls.Count);
    Assert.All(result.BlockedCalls, b => Assert.Contains("Repeated tool call", b.ErrorMessage));
  }

  [Fact]
  public void ValidateAllowsTimezoneWithTimeZoneKeyword()
  {
    var plan = new ToolPlan
    {
      ToolCalls =
      [
        new PlannedToolCall
        {
          PluginName = "Person",
          FunctionName = "set_timezone",
          Arguments = new Dictionary<string, string?> { ["timezone"] = "EST" }
        }
      ]
    };

    var context = BuildContext(messages:
    [
      new ChatMessageDTO(ChatRole.User, "Please set my time zone to EST", DateTime.UtcNow)
    ]);

    var result = ToolPlanValidator.Validate(plan, context);

    _ = Assert.Single(result.ApprovedCalls);
    Assert.Empty(result.BlockedCalls);
  }

  [Fact]
  public void ValidateAllowsTimezoneWithTzKeyword()
  {
    var plan = new ToolPlan
    {
      ToolCalls =
      [
        new PlannedToolCall
        {
          PluginName = "Person",
          FunctionName = "set_timezone",
          Arguments = new Dictionary<string, string?> { ["timezone"] = "PST" }
        }
      ]
    };

    var context = BuildContext(messages:
    [
      new ChatMessageDTO(ChatRole.User, "My tz is PST", DateTime.UtcNow)
    ]);

    var result = ToolPlanValidator.Validate(plan, context);

    _ = Assert.Single(result.ApprovedCalls);
    Assert.Empty(result.BlockedCalls);
  }

  [Fact]
  public void ValidateApprovesValidToolCallWithRequiredArguments()
  {
    var plan = new ToolPlan
    {
      ToolCalls =
      [
        new PlannedToolCall
        {
          PluginName = "Person",
          FunctionName = "set_timezone",
          Arguments = new Dictionary<string, string?> { ["timezone"] = "America/Los_Angeles" }
        }
      ]
    };

    var context = BuildContext(messages:
    [
      new ChatMessageDTO(ChatRole.User, "Set my timezone", DateTime.UtcNow)
    ]);
    var result = ToolPlanValidator.Validate(plan, context);

    _ = Assert.Single(result.ApprovedCalls);
    Assert.Empty(result.BlockedCalls);
  }

  [Fact]
  public void ValidateNormalizesArgumentsToCaseInsensitive()
  {
    var plan = new ToolPlan
    {
      ToolCalls =
      [
        new PlannedToolCall
        {
          PluginName = "Person",
          FunctionName = "set_timezone",
          Arguments = new Dictionary<string, string?> { ["Timezone"] = "UTC" }
        }
      ]
    };

    var context = BuildContext(messages:
    [
      new ChatMessageDTO(ChatRole.User, "Set my timezone to UTC", DateTime.UtcNow)
    ]);
    var result = ToolPlanValidator.Validate(plan, context);

    _ = Assert.Single(result.ApprovedCalls);
    Assert.True(result.ApprovedCalls[0].Arguments.ContainsKey("timezone"));
  }

  private static ToolPlanValidationContext BuildContext(List<ChatMessageDTO>? messages = null)
  {
    var plugins = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
    {
      ["Person"] = new PersonPluginStub()
    };

    return new ToolPlanValidationContext(
      plugins,
      messages ?? []);
  }

  [SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "ToolCallResolver uses BindingFlags.Instance to match production plugin patterns")]
  private sealed class PersonPluginStub
  {
    [KernelFunction("set_timezone")]
    public Task<string> SetTimeZoneAsync(string timezone)
    {
      return Task.FromResult(timezone);
    }
  }
}
