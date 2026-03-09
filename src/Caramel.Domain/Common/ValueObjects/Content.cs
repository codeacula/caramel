namespace Caramel.Domain.Common.ValueObjects;

/// <summary>
/// Represents text content such as a message or response body.
/// </summary>
/// <param name="Value"></param>
public readonly record struct Content(string Value);
