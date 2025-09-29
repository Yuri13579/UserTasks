using System.ComponentModel.DataAnnotations;
using TaskRotationApi.Models;

namespace TaskRotationApi.Dtos;

public record CreateTaskRequest(
    [Required] [MinLength(1)] string Title
);

public record TaskResponse(
    Guid Id,
    string Title,
    TaskState State,
    Guid? AssignedUserId,
    string? AssignedUserName,
    IReadOnlyCollection<Guid> AssignmentHistory,
    DateTimeOffset CreatedAt
);