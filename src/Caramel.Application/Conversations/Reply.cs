using Caramel.Domain.Common.ValueObjects;

namespace Caramel.Application.Conversations;

/// <summary>
/// Represents a system reply to a user's incoming message.
/// Contains the response content and timing information.
/// </summary>
public sealed record Reply
{
  /// <summary>
  /// Gets the text content of the system's reply.
  /// </summary>
  public required Content Content { get; init; }

  /// <summary>
  /// Gets the UTC timestamp when the reply was created.
  /// </summary>
  public required CreatedOn CreatedOn { get; init; }

  /// <summary>
  /// Gets the UTC timestamp when the reply was last updated.
  /// </summary>
  public required UpdatedOn UpdatedOn { get; init; }
}
