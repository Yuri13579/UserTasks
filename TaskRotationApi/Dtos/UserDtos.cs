using System.ComponentModel.DataAnnotations;

namespace TaskRotationApi.Dtos;

public record CreateUserRequest(
    [Required]
    [StringLength(50, MinimumLength = 1)]
    string Name
);

public record UserResponse(Guid Id, string Name, int ActiveTasksCount, int TotalTasksAssigned);