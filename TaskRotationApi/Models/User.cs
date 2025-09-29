namespace TaskRotationApi.Models;

/// <summary>
///     Represents a user that can have tasks assigned.
/// </summary>
public class User
{
    public Guid Id { get; init; } = Guid.NewGuid();

    public required string Name { get; set; }
}