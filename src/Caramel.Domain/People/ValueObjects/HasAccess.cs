namespace Caramel.Domain.People.ValueObjects;

/// <summary>
/// Indicates whether a person has access to the system.
/// </summary>
/// <param name="Value"></param>
public readonly record struct HasAccess(bool Value);

