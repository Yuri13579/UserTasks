using System.ComponentModel.DataAnnotations;

namespace TaskRotationApi.Dtos;

/// <summary>
///     Request payload used to register a new user.
/// </summary>
/// <param name="Name">Display name for the new user.</param>
public record CreateUserRequest(
    [Required]
    [StringLength(50, MinimumLength = 1)]
    string Name
);

/// <summary>
///     Response payload describing a user and assignment statistics.
/// </summary>
/// <param name="Id">The unique identifier of the user.</param>
/// <param name="Name">The display name of the user.</param>
/// <param name="ActiveTasksCount">Number of tasks currently assigned to the user.</param>
/// <param name="TotalTasksAssigned">Total number of tasks that have been assigned to the user.</param>
public record UserResponse(Guid Id, string Name, int ActiveTasksCount, int TotalTasksAssigned);
