namespace Caramel.Domain.People.ValueObjects;

/// <summary>
/// Represents the UTC timestamp when a person was granted access.
/// </summary>
/// <param name="Value"></param>
public readonly record struct GrantedOn(DateTime Value);

