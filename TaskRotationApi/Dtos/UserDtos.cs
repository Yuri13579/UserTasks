using System.ComponentModel.DataAnnotations;

namespace TaskRotationApi.Dtos;

public record CreateUserRequest(
    [Required] [MinLength(1)] string Name
);

public record UserResponse(Guid Id, string Name, int ActiveTaskCount);