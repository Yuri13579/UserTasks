using System.Security.Cryptography;
using TaskRotationApi.Dtos;
using TaskRotationApi.Models;
using TaskRotationApi.Storage;

namespace TaskRotationApi.Services;

/// <summary>
/// Contains all domain rules for creating users, tasks and orchestrating the
/// periodic reassignments.
/// </summary>
public class TaskAssignmentService(InMemoryDataStore store, ILogger<TaskAssignmentService> logger)
{
    public IReadOnlyCollection<UserResponse> GetUsers()
    {
        return store.Read((users, tasks) =>
        {
            var response = new List<UserResponse>(users.Count);
            foreach (var user in users)
            {
                response.Add(MapUser(user, tasks));
            }

            return response;
        });
    }

    public UserResponse? GetUser(Guid id)
    {
        return store.Read((users, tasks) =>
        {
            var user = users.FirstOrDefault(u => u.Id == id);
            return user is null ? null : MapUser(user, tasks);
        });
    }

    public (bool Success, string? Error, UserResponse? User) CreateUser(string name)
    {
        var trimmed = name.Trim();
        if (string.IsNullOrWhiteSpace(trimmed))
        {
            return (false, "Name is required.", null);
        }

        return store.Write((users, tasks) =>
        {
            if (users.Any(u => string.Equals(u.Name, trimmed, StringComparison.OrdinalIgnoreCase)))
            {
                return (false, "A user with the same name already exists.", (UserResponse?)null);
            }

            var user = new User
            {
                Name = trimmed
            };

            users.Add(user);

            // Adding a new user may free capacity for waiting tasks.
            foreach (var task in tasks.Where(t => t.State != TaskState.Completed && t.AssignedUserId is null).ToList())
            {
                TryAssignTask(task, users, tasks);
                TryFinalizeTask(task, users);
            }

            return (true, null, MapUser(user, tasks));
        });
    }

    public (bool Success, string? Error) DeleteUser(Guid id)
    {
        return store.Write((users, tasks) =>
        {
            var user = users.FirstOrDefault(u => u.Id == id);
            if (user is null)
            {
                return (false, "User not found.");
            }

            users.Remove(user);

            foreach (var task in tasks)
            {
                if (task.AssignedUserId == id)
                {
                    task.AssignedUserId = null;
                    if (task.PreviousUserId == id)
                    {
                        task.PreviousUserId = null;
                    }
                    task.State = TaskState.Waiting;
                }
                else if (task.PreviousUserId == id)
                {
                    task.PreviousUserId = null;
                }

                TryFinalizeTask(task, users);
            }

            // Removing a user may also free capacity for waiting tasks if others are idle.
            foreach (var task in tasks.Where(t => t.State != TaskState.Completed && t.AssignedUserId is null).ToList())
            {
                TryAssignTask(task, users, tasks);
                TryFinalizeTask(task, users);
            }

            return (true, null);
        });
    }

    public IReadOnlyCollection<TaskResponse> GetTasks()
    {
        return store.Read((users, tasks) => tasks.Select(t => MapTask(t, users)).ToList());
    }

    public TaskResponse? GetTask(Guid id)
    {
        return store.Read((users, tasks) =>
        {
            var task = tasks.FirstOrDefault(t => t.Id == id);
            return task is null ? null : MapTask(task, users);
        });
    }

    public (bool Success, string? Error, TaskResponse? Task) CreateTask(string title)
    {
        var trimmed = title.Trim();
        if (string.IsNullOrWhiteSpace(trimmed))
        {
            return (false, "Title is required.", null);
        }

        return store.Write((users, tasks) =>
        {
            if (tasks.Any(t => string.Equals(t.Title, trimmed, StringComparison.OrdinalIgnoreCase)))
            {
                return (false, "A task with the same title already exists.", (TaskResponse?)null);
            }

            var task = new TaskItem
            {
                Title = trimmed
            };

            tasks.Add(task);

            TryAssignTask(task, users, tasks);
            TryFinalizeTask(task, users);

            return (true, null, MapTask(task, users));
        });
    }

    /// <summary>
    /// Called by the hosted service every two minutes.
    /// </summary>
    public void RotateAssignments()
    {
        store.Write((users, tasks) =>
        {
            if (users.Count == 0)
            {
                foreach (var task in tasks)
                {
                    if (task.State != TaskState.Completed)
                    {
                        task.AssignedUserId = null;
                        task.State = TaskState.Waiting;
                    }
                }
                return;
            }

            foreach (var task in tasks)
            {
                if (task.State == TaskState.Completed)
                {
                    continue;
                }

                // The rotation rule requires every task to attempt a reassignment.
                var previousAssignee = task.AssignedUserId;
                var assigned = TryAssignTask(task, users, tasks, forceDifferent: true);

                if (!assigned)
                {
                    task.AssignedUserId = null;
                    task.State = TaskState.Waiting;
                }
                else if (previousAssignee != task.AssignedUserId)
                {
                    logger.LogInformation("Task {TaskTitle} moved from {PreviousUser} to {CurrentUser}", task.Title, previousAssignee, task.AssignedUserId);
                }

                TryFinalizeTask(task, users);
            }
        });
    }

    private bool TryAssignTask(TaskItem task, IReadOnlyList<User> users, IReadOnlyList<TaskItem> allTasks, bool forceDifferent = false)
    {
        if (task.State == TaskState.Completed)
        {
            return false;
        }

        var activeCounts = new Dictionary<Guid, int>();
        foreach (var other in allTasks)
        {
            if (other.State == TaskState.Completed || other.AssignedUserId is null)
            {
                continue;
            }

            if (!activeCounts.TryGetValue(other.AssignedUserId.Value, out var count))
            {
                count = 0;
            }

            activeCounts[other.AssignedUserId.Value] = count + 1;
        }

        var candidates = new List<User>();
        foreach (var user in users)
        {
            activeCounts.TryGetValue(user.Id, out var count);
            if (count >= 3)
            {
                continue;
            }

            if (forceDifferent && task.AssignedUserId == user.Id)
            {
                continue;
            }

            if (task.AssignedUserId == user.Id || task.PreviousUserId == user.Id)
            {
                continue;
            }

            candidates.Add(user);
        }

        if (candidates.Count == 0)
        {
            return false;
        }

        var historySet = new HashSet<Guid>(task.AssignmentHistory);
        var unseenCandidates = candidates.Where(c => !historySet.Contains(c.Id)).ToList();
        var selectionPool = unseenCandidates.Count > 0 ? unseenCandidates : candidates;

        var selected = selectionPool[RandomNumberGenerator.GetInt32(selectionPool.Count)];

        task.PreviousUserId = task.AssignedUserId;
        task.AssignedUserId = selected.Id;
        task.State = TaskState.InProgress;
        task.AssignmentHistory.Add(selected.Id);

        return true;
    }

    private void TryFinalizeTask(TaskItem task, IReadOnlyList<User> users)
    {
        if (task.State == TaskState.Completed)
        {
            return;
        }

        if (users.Count == 0)
        {
            task.AssignedUserId = null;
            task.State = TaskState.Waiting;
            return;
        }

        var uniqueHistory = task.AssignmentHistory.Distinct().ToHashSet();
        var allUsersVisited = users.All(u => uniqueHistory.Contains(u.Id));

        if (allUsersVisited)
        {
            task.State = TaskState.Completed;
            task.PreviousUserId = task.AssignedUserId;
            task.AssignedUserId = null;
        }
    }

    private UserResponse MapUser(User user, IReadOnlyList<TaskItem> tasks)
    {
        var activeCount = tasks.Count(t => t.AssignedUserId == user.Id && t.State != TaskState.Completed);
        return new UserResponse(user.Id, user.Name, activeCount);
    }

    private TaskResponse MapTask(TaskItem task, IReadOnlyList<User> users)
    {
        var assignedUserName = task.AssignedUserId.HasValue
            ? users.FirstOrDefault(u => u.Id == task.AssignedUserId.Value)?.Name
            : null;

        return new TaskResponse(
            task.Id,
            task.Title,
            task.State,
            task.AssignedUserId,
            assignedUserName,
            task.AssignmentHistory.AsReadOnly(),
            task.CreatedAt
        );
    }
}
