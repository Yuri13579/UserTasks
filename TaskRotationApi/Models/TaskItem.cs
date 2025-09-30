namespace TaskRotationApi.Models;

/// <summary>
///     Represents a unit of work to be transferred between users.
/// </summary>
public class TaskItem
{
    /// <summary>
    ///     Gets the unique identifier for the task.
    /// </summary>
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>
    ///     Gets or sets the descriptive title of the task.
    /// </summary>
    public required string Title { get; set; }

    /// <summary>
    ///     Gets or sets the current state of the task.
    /// </summary>
    public TaskState State { get; set; } = TaskState.Waiting;

    /// <summary>
    ///     Gets or sets the identifier of the user currently assigned to the task.
    /// </summary>
    public Guid? AssignedUserId { get; set; }

    /// <summary>
    ///     Gets or sets the identifier of the user that was most recently assigned before the current one.
    /// </summary>
    public Guid? PreviousUserId { get; set; }

    /// <summary>
    ///     Gets the chronological list of users that have been assigned to the task.
    /// </summary>
    public List<Guid> AssignmentHistory { get; } = [];

    /// <summary>
    ///     Gets the timestamp of when the task was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; } = DateTimeOffset.UtcNow;
}
