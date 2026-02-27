namespace Caramel.Domain.Common.ValueObjects;

/// <summary>
/// Represents the UTC timestamp when an entity was created.
/// </summary>
public readonly record struct CreatedOn(DateTime Value);
