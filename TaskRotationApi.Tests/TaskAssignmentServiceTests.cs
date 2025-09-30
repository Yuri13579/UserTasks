using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using TaskRotationApi.Dtos;
using TaskRotationApi.Models;
using TaskRotationApi.Services;
using TaskRotationApi.Storage;
using Xunit;

namespace TaskRotationApi.Tests;

public class TaskAssignmentServiceTests
{
    [Fact]
    public void CreateTask_AssignsUser_WhenAvailable()
    {
        var service = CreateService(out var store);

        var userId = AddUser(service, "Alice");

        var result = service.CreateTask("Task A");

        Assert.True(result.Success);
        Assert.NotNull(result.Value);
        Assert.Equal(TaskState.InProgress, result.Value!.State);
        Assert.Equal(userId, result.Value.AssignedUserId);

        var storedTask = store.Read((_, tasks) => tasks.Single());
        Assert.Equal(TaskState.InProgress, storedTask.State);
        Assert.Equal(userId, storedTask.AssignedUserId);
    }

    [Fact]
    public void CreateTask_StaysWaiting_WhenNoUsersAvailable()
    {
        var service = CreateService(out _);

        var result = service.CreateTask("Task B");

        Assert.True(result.Success);
        Assert.NotNull(result.Value);
        Assert.Equal(TaskState.Waiting, result.Value!.State);
        Assert.Null(result.Value.AssignedUserId);
    }

    [Fact]
    public void CreateTask_EnforcesMaximumOfThreeActiveTasksPerUser()
    {
        var service = CreateService(out var store);

        var userId = AddUser(service, "Bob");

        for (var i = 0; i < 4; i++)
        {
            var result = service.CreateTask($"Task {i}");
            Assert.True(result.Success);
            Assert.NotNull(result.Value);

            if (i < 3)
            {
                Assert.Equal(TaskState.InProgress, result.Value!.State);
                Assert.Equal(userId, result.Value.AssignedUserId);
            }
            else
            {
                Assert.Equal(TaskState.Waiting, result.Value!.State);
                Assert.Null(result.Value.AssignedUserId);
            }
        }

        var activeCount = store.Read((_, tasks) => tasks.Count(t => t.AssignedUserId == userId));
        Assert.Equal(3, activeCount);
    }

    [Fact]
    public void RotateAssignments_DoesNotReuseCurrentOrPreviousUser()
    {
        var service = CreateService(out var store);

        var user1 = AddUser(service, "User 1");
        var user2 = AddUser(service, "User 2");
        var user3 = AddUser(service, "User 3");

        var taskId = Guid.NewGuid();
        store.Write((users, tasks) =>
        {
            tasks.Add(new TaskItem
            {
                Id = taskId,
                Title = "Rotation",
                State = TaskState.InProgress,
                AssignedUserId = user1,
                AssignmentHistory = { user1 }
            });
        });

        var firstRotation = service.RotateAssignments();
        Assert.Single(firstRotation);
        var firstChange = firstRotation.Single();

        Assert.Equal(taskId, firstChange.TaskId);
        Assert.Equal(user1, firstChange.FromUserId);
        Assert.NotNull(firstChange.ToUserId);
        Assert.NotEqual(user1, firstChange.ToUserId);

        var secondRotation = service.RotateAssignments();
        Assert.Single(secondRotation);
        var secondChange = secondRotation.Single();

        Assert.Equal(taskId, secondChange.TaskId);
        Assert.Equal(firstChange.ToUserId, secondChange.FromUserId);
        Assert.NotNull(secondChange.ToUserId);
        Assert.NotEqual(secondChange.FromUserId, secondChange.ToUserId);
        Assert.NotEqual(user1, secondChange.ToUserId);

        var visitedUsers = new HashSet<Guid>();
        store.Read((_, tasks) =>
        {
            var task = tasks.Single(t => t.Id == taskId);
            visitedUsers.UnionWith(task.AssignmentHistory);
            return 0;
        });

        Assert.Contains(user1, visitedUsers);
        Assert.Contains(user2, visitedUsers);
        Assert.Contains(user3, visitedUsers);
    }

    private static TaskAssignmentService CreateService(out InMemoryDataStore store, int intervalSeconds = 120)
    {
        store = new InMemoryDataStore();
        store.Write((users, tasks) =>
        {
            users.Clear();
            tasks.Clear();
        });
       
        TimeSpan interval = TimeSpan.FromSeconds(intervalSeconds); 
        return new TaskAssignmentService(store,  NullLogger<TaskAssignmentService>.Instance, interval);
    }



    private static Guid AddUser(TaskAssignmentService service, string name)
    {
        var result = service.CreateUser(name);
        Assert.True(result.Success);
        Assert.NotNull(result.Value);
        return result.Value!.Id;
    }
}
