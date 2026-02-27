namespace Caramel.Domain.People.ValueObjects;

/// <summary>
/// Represents a unique identifier for a person in the system.
/// </summary>
public readonly record struct PersonId(Guid Value);
