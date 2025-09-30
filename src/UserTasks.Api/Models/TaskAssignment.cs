using System;

namespace UserTasks.Api.Models;

public class TaskAssignment
{
    public TaskAssignment(Guid id, string title, string description)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new ArgumentException("Title is required.", nameof(title));
        }

        if (string.IsNullOrWhiteSpace(description))
        {
            throw new ArgumentException("Description is required.", nameof(description));
        }

        Id = id;
        Title = title.Trim();
        Description = description.Trim();
        CreatedAt = DateTime.UtcNow;
        State = TaskState.Pending;
    }

    public Guid Id { get; }

    public string Title { get; }

    public string Description { get; }

    public Guid? AssignedUserId { get; private set; }

    public TaskState State { get; private set; }

    public DateTime CreatedAt { get; }

    public DateTime? CompletedAt { get; private set; }

    public void AssignTo(Guid userId)
    {
        AssignedUserId = userId;
        State = TaskState.InProgress;
    }

    public void MarkPending()
    {
        AssignedUserId = null;
        State = TaskState.Pending;
    }

    public void Complete()
    {
        State = TaskState.Completed;
        CompletedAt = DateTime.UtcNow;
    }
}
