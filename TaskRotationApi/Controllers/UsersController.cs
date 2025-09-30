using Microsoft.AspNetCore.Mvc;
using TaskRotationApi.Dtos;
using TaskRotationApi.Services;

namespace TaskRotationApi.Controllers;

[ApiController]
[Route("api/[controller]")]
/// <summary>
///     Exposes endpoints for managing users participating in task rotation.
/// </summary>
public class UsersController(TaskAssignmentService service) : ControllerBase
{
    /// <summary>
    ///     Returns all registered users.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<UserResponse>))]
    public ActionResult<IReadOnlyCollection<UserResponse>> GetUsers()
    {
        return Ok(service.GetUsers());
    }

    /// <summary>
    ///     Returns a single user by identifier.
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(UserResponse))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult<UserResponse> GetUser(Guid id)
    {
        var user = service.GetUser(id);
        return user is null
            ? NotFound(new { message = "User not found." })
            : Ok(user);
    }

    /// <summary>
    ///     Creates a new user.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(UserResponse))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public ActionResult<UserResponse> CreateUser([FromBody] CreateUserRequest request)
    {
        var result = service.CreateUser(request.Name);

        if (!result.Success)
            return MapError<UserResponse>(new ServiceResult(result.Success, result.Code, result.Error));

        return CreatedAtAction(nameof(GetUser), new { id = result.Value!.Id }, result.Value);
    }

    /// <summary>
    ///     Deletes a user and releases their tasks.
    /// </summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult DeleteUser(Guid id)
    {
        var result = service.DeleteUser(id);
        if (!result.Success) return MapError(result);

        return NoContent();
    }

    /// <summary>
    ///     Maps a <see cref="ServiceResult"/> to an HTTP response with a typed payload.
    /// </summary>
    /// <typeparam name="T">The expected response body type.</typeparam>
    /// <param name="result">The operation outcome to convert.</param>
    /// <returns>An HTTP result representing the provided <paramref name="result"/>.</returns>
    private static ActionResult<T> MapError<T>(ServiceResult result)
    {
        return result.Code switch
        {
            ErrorCode.Duplicate => new ConflictObjectResult(new { message = result.Error }),
            ErrorCode.NotFound => new NotFoundObjectResult(new { message = result.Error }),
            ErrorCode.Invalid => new BadRequestObjectResult(new { message = result.Error }),
            ErrorCode.LimitReached => new BadRequestObjectResult(new { message = result.Error }),
            _ => new BadRequestObjectResult(new { message = result.Error ?? "An unexpected error occurred." })
        };
    }

    /// <summary>
    ///     Maps a <see cref="ServiceResult"/> without payload to an HTTP response.
    /// </summary>
    /// <param name="result">The operation outcome to convert.</param>
    /// <returns>An HTTP result representing the provided <paramref name="result"/>.</returns>
    private static IActionResult MapError(ServiceResult result)
    {
        return result.Code switch
        {
            ErrorCode.Duplicate => new ConflictObjectResult(new { message = result.Error }),
            ErrorCode.NotFound => new NotFoundObjectResult(new { message = result.Error }),
            ErrorCode.Invalid => new BadRequestObjectResult(new { message = result.Error }),
            ErrorCode.LimitReached => new BadRequestObjectResult(new { message = result.Error }),
            _ => new BadRequestObjectResult(new { message = result.Error ?? "An unexpected error occurred." })
        };
    }
}