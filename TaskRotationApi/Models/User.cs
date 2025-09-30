namespace TaskRotationApi.Models;

/// <summary>
///     Represents a user that can have tasks assigned.
/// </summary>
public class User
{
    /// <summary>
    ///     Gets the unique identifier for the user.
    /// </summary>
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>
    ///     Gets or sets the display name of the user.
    /// </summary>
    public required string Name { get; set; }
}
