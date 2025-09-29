using System.ComponentModel.DataAnnotations;
using TaskRotationApi.Models;

namespace TaskRotationApi.Dtos;

public record CreateTaskRequest(
    [Required]
    [StringLength(100, MinimumLength = 1)]
    string Title
);

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