namespace TaskRotationApi.Models;

/// <summary>
///     Represents a unit of work to be transferred between users.
/// </summary>
public class TaskItem
{
    public Guid Id { get; init; } = Guid.NewGuid();

    public required string Title { get; set; }

    public TaskState State { get; set; } = TaskState.Waiting;

    public Guid? AssignedUserId { get; set; }

    public Guid? PreviousUserId { get; set; }

    public List<Guid> AssignmentHistory { get; } = [];

    public DateTimeOffset CreatedAt { get; } = DateTimeOffset.UtcNow;
}