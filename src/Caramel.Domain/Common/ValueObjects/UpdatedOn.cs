namespace Caramel.Domain.Common.ValueObjects;

/// <summary>
/// Represents the UTC timestamp when an entity was last updated.
/// </summary>
public readonly record struct UpdatedOn(DateTime Value);
