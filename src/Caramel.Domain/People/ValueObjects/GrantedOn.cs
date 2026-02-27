namespace Caramel.Domain.People.ValueObjects;

/// <summary>
/// Represents the UTC timestamp when a person was granted access.
/// </summary>
public readonly record struct GrantedOn(DateTime Value);

