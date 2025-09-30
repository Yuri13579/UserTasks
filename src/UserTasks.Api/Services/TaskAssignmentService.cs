using System;
using System.Linq;
using UserTasks.Api.Models;

namespace UserTasks.Api.Services;

public class TaskAssignmentService
{
    private readonly InMemoryDataStore _dataStore;

    public TaskAssignmentService(InMemoryDataStore dataStore)
    {
        _dataStore = dataStore;
    }

    public UserAccount RegisterUser(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Name is required.", nameof(name));
        }

        var user = new UserAccount(Guid.NewGuid(), name.Trim());
        _dataStore.AddUser(user);

        var queuedTask = _dataStore.DequeueNextQueuedAssignment();
        if (queuedTask is not null)
        {
            queuedTask.AssignTo(user.Id);
            user.MarkBusy();
        }

        return user;
    }

    public OperationResult<TaskAssignment> CreateTask(string title, string description)
    {
        TaskAssignment assignment;
        try
        {
            assignment = new TaskAssignment(Guid.NewGuid(), title, description);
        }
        catch (ArgumentException ex)
        {
            return OperationResult<TaskAssignment>.Failure(ex.Message);
        }

        var availableUser = _dataStore.Users.FirstOrDefault(user => user.IsAvailable);

        if (availableUser is not null)
        {
            assignment.AssignTo(availableUser.Id);
            availableUser.MarkBusy();
        }
        else
        {
            assignment.MarkPending();
            _dataStore.EnqueueAssignment(assignment);
        }

        _dataStore.AddTaskAssignment(assignment);

        return OperationResult<TaskAssignment>.Successful(assignment);
    }

    public OperationResult<TaskAssignment> CompleteTask(Guid taskId)
    {
        var assignment = _dataStore.TaskAssignments.FirstOrDefault(task => task.Id == taskId);
        if (assignment is null)
        {
            return OperationResult<TaskAssignment>.Failure($"Task '{taskId}' was not found.");
        }

        if (assignment.State == TaskState.Completed)
        {
            return OperationResult<TaskAssignment>.Failure("Task is already completed.");
        }

        if (assignment.State != TaskState.InProgress)
        {
            return OperationResult<TaskAssignment>.Failure("Only in-progress tasks can be completed.");
        }

        assignment.Complete();

        if (assignment.AssignedUserId is Guid userId)
        {
            var user = _dataStore.Users.FirstOrDefault(candidate => candidate.Id == userId);
            if (user is not null)
            {
                user.MarkAvailable();

                var nextTask = _dataStore.DequeueNextQueuedAssignment();
                if (nextTask is not null)
                {
                    nextTask.AssignTo(user.Id);
                    user.MarkBusy();
                }
            }
        }

        return OperationResult<TaskAssignment>.Successful(assignment);
    }
}
