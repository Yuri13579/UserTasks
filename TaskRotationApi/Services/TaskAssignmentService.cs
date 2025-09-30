using System.Security.Cryptography;
using TaskRotationApi.Dtos;
using TaskRotationApi.Models;
using TaskRotationApi.Storage;

namespace TaskRotationApi.Services;

/// <summary>
///     Contains all domain rules for creating users, tasks and orchestrating the periodic reassignments.
/// </summary>
/// <remarks>
///     Provides operations for querying and mutating tasks and users while respecting rotation rules.
/// </remarks>
/// <param name="store">Centralized storage abstraction for users and tasks.</param>
/// <param name="logger">Logger used for diagnostic information.</param>
/// <param name="interval">Minimum time a task must exist before it can be finalized.</param>
public class TaskAssignmentService(InMemoryDataStore store, ILogger<TaskAssignmentService> logger,  TimeSpan interval)
{
    /// <summary>
    ///     Retrieves all users along with their assignment statistics.
    /// </summary>
    public IReadOnlyCollection<UserResponse> GetUsers()
    {
        return store.Read((users, tasks) =>
        {
            var response = new List<UserResponse>(users.Count);
            foreach (var user in users) response.Add(MapUser(user, tasks));

            return response;
        });
    }

    /// <summary>
    ///     Retrieves a single user by identifier with assignment statistics.
    /// </summary>
    /// <param name="id">The identifier of the user to retrieve.</param>
    /// <returns>The matching user response or <c>null</c> when not found.</returns>
    public UserResponse? GetUser(Guid id)
    {
        return store.Read((users, tasks) =>
        {
            var user = users.FirstOrDefault(u => u.Id == id);
            return user is null ? null : MapUser(user, tasks);
        });
    }

    /// <summary>
    ///     Creates a new user and triggers assignment of waiting tasks.
    /// </summary>
    /// <param name="name">The display name of the new user.</param>
    /// <returns>The outcome of the creation operation.</returns>
    public ServiceResult<UserResponse> CreateUser(string name)
    {
        var trimmed = name?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(trimmed))
            return ServiceResult<UserResponse>.Failure(ErrorCode.Invalid, "Name is required.");

        return store.Write((users, tasks) =>
        {
            if (users.Any(u => string.Equals(u.Name, trimmed, StringComparison.OrdinalIgnoreCase)))
                return ServiceResult<UserResponse>.Failure(ErrorCode.Duplicate,
                    "A user with the same name already exists.");

            var user = new User
            {
                Name = trimmed
            };

            users.Add(user);

            var newlyAssigned = 0;
            foreach (var task in tasks.Where(t => t.State != TaskState.Completed && t.AssignedUserId is null).ToList())
            {
                if (TryAssignTask(task, users, tasks)) newlyAssigned++;

                TryFinalizeTask(task, users);
            }

            if (newlyAssigned > 0)
                logger.LogInformation("Assigned {TaskCount} waiting tasks after adding user {UserId}", newlyAssigned,
                    user.Id);

            return ServiceResult<UserResponse>.SuccessResult(MapUser(user, tasks));
        });
    }

    /// <summary>
    ///     Deletes a user and releases any tasks assigned to them.
    /// </summary>
    /// <param name="id">The identifier of the user to remove.</param>
    /// <returns>The outcome of the deletion operation.</returns>
    public ServiceResult DeleteUser(Guid id)
    {
        return store.Write((users, tasks) =>
        {
            var user = users.FirstOrDefault(u => u.Id == id);
            if (user is null) return ServiceResult.Failure(ErrorCode.NotFound, "User not found.");

            users.Remove(user);

            var waitingCount = 0;
            foreach (var task in tasks.Where(t => t.State != TaskState.Completed))
            {
                if (task.AssignedUserId == id)
                {
                    task.AssignedUserId = null;
                    task.State = TaskState.Waiting;
                    waitingCount++;
                }

                if (task.PreviousUserId == id) task.PreviousUserId = null;
            }

            if (waitingCount > 0)
                logger.LogInformation("User {UserId} deleted. {TaskCount} tasks returned to waiting state.", id,
                    waitingCount);

            var reassigned = 0;
            foreach (var task in tasks.Where(t => t.State != TaskState.Completed && t.AssignedUserId is null).ToList())
            {
                if (TryAssignTask(task, users, tasks)) reassigned++;

                TryFinalizeTask(task, users);
            }

            if (reassigned > 0)
                logger.LogInformation("Reassigned {TaskCount} waiting tasks after deleting user {UserId}.", reassigned,
                    id);

            return ServiceResult.SuccessResult();
        });
    }

    /// <summary>
    ///     Retrieves all tasks including their assignment metadata.
    /// </summary>
    /// <returns>A collection of task responses.</returns>
    public IReadOnlyCollection<TaskResponse> GetTasks()
    {
        return store.Read((users, tasks) => tasks.Select(t => MapTask(t, users)).ToList());
    }

    /// <summary>
    ///     Retrieves a specific task by identifier.
    /// </summary>
    /// <param name="id">The identifier of the task to retrieve.</param>
    /// <returns>The task response or <c>null</c> when not found.</returns>
    public TaskResponse? GetTask(Guid id)
    {
        return store.Read((users, tasks) =>
        {
            var task = tasks.FirstOrDefault(t => t.Id == id);
            return task is null ? null : MapTask(task, users);
        });
    }

    /// <summary>
    ///     Creates a new task and attempts to assign it immediately.
    /// </summary>
    /// <param name="title">The descriptive title of the task.</param>
    /// <returns>The result of the creation operation.</returns>
    public ServiceResult<TaskResponse> CreateTask(string title)
        => store.Write((users, tasks) => CreateTaskCore(users, tasks, title));


    /// <summary>
    ///     Called by the hosted service every two minutes to rotate assignments.
    /// </summary>
    /// <returns>A collection describing changes to task assignments.</returns>
    public IReadOnlyCollection<(Guid TaskId, Guid? FromUserId, Guid? ToUserId)> RotateAssignments()
    {
        return store.Write((users, tasks) =>
        {
            var changes = new List<(Guid TaskId, Guid? FromUserId, Guid? ToUserId)>();

            if (users.Count == 0)
            {
                foreach (var task in tasks)
                {
                    if (task.State == TaskState.Completed) continue;

                    if (task.AssignedUserId is not null) changes.Add((task.Id, task.AssignedUserId, null));

                    task.AssignedUserId = null;
                    task.PreviousUserId = null;
                    task.State = TaskState.Waiting;
                }

                return changes;
            }

            foreach (var task in tasks)
            {
                if (task.State == TaskState.Completed) continue;

                var previousAssignee = task.AssignedUserId;
                var assigned = TryAssignTask(task, users, tasks, true);

                if (!assigned)
                {
                    if (previousAssignee is not null) changes.Add((task.Id, previousAssignee, null));

                    task.PreviousUserId = previousAssignee;
                    task.AssignedUserId = null;
                    task.State = TaskState.Waiting;
                }
                else if (previousAssignee != task.AssignedUserId)
                {
                    changes.Add((task.Id, previousAssignee, task.AssignedUserId));
                }

                TryFinalizeTask(task, users);
            }

            return changes;
        });
    }

    /// <summary>
    ///     Implements the logic for creating a task within a write-locked operation.
    /// </summary>
    /// <param name="users">The mutable collection of users.</param>
    /// <param name="tasks">The mutable collection of tasks.</param>
    /// <param name="title">The task title to persist.</param>
    /// <returns>The outcome of the creation operation.</returns>
    private ServiceResult<TaskResponse> CreateTaskCore(
        List<User> users, List<TaskItem> tasks, string title)
    {
        var trimmed = title?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(trimmed))
            return ServiceResult<TaskResponse>.Failure(ErrorCode.Invalid, "Title is required.");

        if (tasks.Any(t => string.Equals(t.Title, trimmed, StringComparison.OrdinalIgnoreCase)))
            return ServiceResult<TaskResponse>.Failure(ErrorCode.Duplicate, "A task with the same title already exists.");

        var task = new TaskItem { Title = trimmed };
        tasks.Add(task);

        TryAssignTask(task, users, tasks);
        TryFinalizeTask(task, users);

        return ServiceResult<TaskResponse>.SuccessResult(MapTask(task, users));
    }

    /// <summary>
    ///     Attempts to assign the provided task to a user that satisfies the rotation constraints.
    /// </summary>
    /// <param name="task">The task to assign.</param>
    /// <param name="users">The available users.</param>
    /// <param name="allTasks">All tasks for evaluating current load.</param>
    /// <param name="forceDifferent">Whether the new assignment must differ from the current user.</param>
    /// <returns><c>true</c> if the task was assigned; otherwise, <c>false</c>.</returns>
    private bool TryAssignTask(TaskItem task, IReadOnlyList<User> users, IReadOnlyList<TaskItem> allTasks,
        bool forceDifferent = false)
    {
        if (task.State == TaskState.Completed || users.Count == 0) return false;

        var activeCounts = new Dictionary<Guid, int>();
        foreach (var other in allTasks)
        {
            if (other.State == TaskState.Completed || other.AssignedUserId is null) continue;

            if (!activeCounts.TryGetValue(other.AssignedUserId.Value, out var count)) count = 0;

            activeCounts[other.AssignedUserId.Value] = count + 1;
        }

        var candidates = new List<User>();
        foreach (var user in users)
        {
            activeCounts.TryGetValue(user.Id, out var count);
            if (count >= 3) continue;

            if (forceDifferent && task.AssignedUserId == user.Id) continue;

            if (task.AssignedUserId == user.Id || task.PreviousUserId == user.Id) continue;

            candidates.Add(user);
        }

        if (candidates.Count == 0) return false;

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

    /// <summary>
    ///     Completes the provided task when it has visited all users or resets state if no users exist.
    /// </summary>
    /// <param name="task">The task to evaluate.</param>
    /// <param name="users">The available users used for completion criteria.</param>
    private void TryFinalizeTask(TaskItem task, IReadOnlyList<User> users)
    {
        if (task.State == TaskState.Completed || DateTimeOffset.UtcNow - task.CreatedAt < interval) return;

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

    /// <summary>
    ///     Creates a response DTO describing a user and their task statistics.
    /// </summary>
    /// <param name="user">The user to map.</param>
    /// <param name="tasks">The current task collection.</param>
    /// <returns>The mapped <see cref="UserResponse"/>.</returns>
    private UserResponse MapUser(User user, IReadOnlyList<TaskItem> tasks)
    {
        var activeCount = tasks.Count(t => t.AssignedUserId == user.Id && t.State != TaskState.Completed);
        var totalAssigned = tasks.Count(t => t.AssignmentHistory.Contains(user.Id));
        return new UserResponse(user.Id, user.Name, activeCount, totalAssigned);
    }

    /// <summary>
    ///     Creates a response DTO describing a task and its assignment history.
    /// </summary>
    /// <param name="task">The task to map.</param>
    /// <param name="users">The user collection used to resolve names.</param>
    /// <returns>The mapped <see cref="TaskResponse"/>.</returns>
    private TaskResponse MapTask(TaskItem task, IReadOnlyList<User> users)
    {
        var assignedUserName = task.AssignedUserId.HasValue
            ? users.FirstOrDefault(u => u.Id == task.AssignedUserId.Value)?.Name
            : null;

        var visitedUsersCount = task.AssignmentHistory.Distinct().Count();

        return new TaskResponse(
            task.Id,
            task.Title,
            task.State,
            task.AssignedUserId,
            assignedUserName,
            visitedUsersCount,
            task.AssignmentHistory.AsReadOnly(),
            task.CreatedAt
        );
    }
}