using System;
using System.Collections.Generic;
using UserTasks.Api.Models;

namespace UserTasks.Api.Services;

public class InMemoryDataStore
{
    private readonly List<UserTask> _tasks = new();
    private bool _isSeeded;

    public IReadOnlyCollection<UserTask> Tasks => _tasks.AsReadOnly();

    public bool SeedInitialData()
    {
        if (_isSeeded)
        {
            return false;
        }

        _tasks.Clear();
        _tasks.AddRange(new[]
        {
            new UserTask(Guid.NewGuid(), "Write API docs", "Document the seedTestData endpoint", DateTime.UtcNow.AddDays(1), false),
            new UserTask(Guid.NewGuid(), "Create demo user", "Add a sample user for testing", DateTime.UtcNow.AddDays(2), false),
            new UserTask(Guid.NewGuid(), "Schedule reminder", "Configure reminder notifications", DateTime.UtcNow.AddDays(3), false)
        });

        _isSeeded = true;
        return true;
    }
}
