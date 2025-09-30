using System;
using System.Collections.Generic;
using System.Linq;
using UserTasks.Api.Models;

namespace UserTasks.Api.Services;

public class InMemoryDataStore
{
    private readonly List<UserTask> _tasks = new();
    private readonly List<UserAccount> _users = new();
    private readonly List<TaskAssignment> _taskAssignments = new();
    private readonly Queue<TaskAssignment> _queuedAssignments = new();
    private bool _isSeeded;

    public IReadOnlyCollection<UserTask> Tasks => _tasks.AsReadOnly();
    public IList<UserAccount> Users => _users;
    public IList<TaskAssignment> TaskAssignments => _taskAssignments;
    public IReadOnlyCollection<TaskAssignment> QueuedAssignments => _queuedAssignments.ToArray();

    public void AddUser(UserAccount user)
    {
        if (user is null)
        {
            throw new ArgumentNullException(nameof(user));
        }

        if (_users.Any(existing => existing.Id == user.Id))
        {
            return;
        }

        _users.Add(user);
    }

    public void AddTaskAssignment(TaskAssignment assignment)
    {
        if (assignment is null)
        {
            throw new ArgumentNullException(nameof(assignment));
        }

        if (_taskAssignments.Any(existing => existing.Id == assignment.Id))
        {
            return;
        }

        _taskAssignments.Add(assignment);
    }

    public void EnqueueAssignment(TaskAssignment assignment)
    {
        if (assignment is null)
        {
            throw new ArgumentNullException(nameof(assignment));
        }

        if (_queuedAssignments.Contains(assignment))
        {
            return;
        }

        _queuedAssignments.Enqueue(assignment);
    }

    public TaskAssignment? DequeueNextQueuedAssignment()
    {
        while (_queuedAssignments.Count > 0)
        {
            var next = _queuedAssignments.Dequeue();
            if (next.State == TaskState.Pending)
            {
                return next;
            }
        }

        return null;
    }

    public bool SeedInitialData()
    {
        if (_isSeeded)
        {
            return false;
        }

        _tasks.Clear();
        _taskAssignments.Clear();
        _queuedAssignments.Clear();
        _tasks.AddRange(new[]
        {
            new UserTask(Guid.NewGuid(), "Write API docs", "Document the seedTestData endpoint", DateTime.UtcNow.AddDays(1), false),
            new UserTask(Guid.NewGuid(), "Create demo user", "Add a sample user for testing", DateTime.UtcNow.AddDays(2), false),
            new UserTask(Guid.NewGuid(), "Schedule reminder", "Configure reminder notifications", DateTime.UtcNow.AddDays(3), false)
        });

        if (_users.Count == 0)
        {
            _users.AddRange(new[]
            {
                new UserAccount(Guid.NewGuid(), "Alex"),
                new UserAccount(Guid.NewGuid(), "Bailey"),
                new UserAccount(Guid.NewGuid(), "Cameron")
            });
        }

        _isSeeded = true;
        return true;
    }
}
