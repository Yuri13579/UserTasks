using System.ComponentModel.DataAnnotations;
using TaskRotationApi.Models;

namespace TaskRotationApi.Dtos;

/// <summary>
///     Request payload used to create a task within the API.
/// </summary>
/// <param name="Title">The desired title of the task.</param>
public record CreateTaskRequest(
    [Required]
    [StringLength(100, MinimumLength = 1)]
    string Title
);

/// <summary>
///     Response payload that describes a task and its assignment metadata.
/// </summary>
/// <param name="Id">The unique identifier of the task.</param>
/// <param name="Title">The descriptive title of the task.</param>
/// <param name="State">The current processing state of the task.</param>
/// <param name="AssignedUserId">The identifier of the user currently assigned to the task.</param>
/// <param name="AssignedUserName">The display name of the user currently assigned to the task.</param>
/// <param name="VisitedUsersCount">Number of distinct users that have been assigned the task.</param>
/// <param name="AssignmentHistory">Chronological list of users that have received the task.</param>
/// <param name="CreatedAt">Timestamp of when the task was created.</param>
public record TaskResponse(
    Guid Id,
    string Title,
    TaskState State,
    Guid? AssignedUserId,
    string? AssignedUserName,
    int VisitedUsersCount,
    IReadOnlyCollection<Guid> AssignmentHistory,
    DateTimeOffset CreatedAt
);
