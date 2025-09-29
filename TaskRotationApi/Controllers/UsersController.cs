using Microsoft.AspNetCore.Mvc;
using TaskRotationApi.Dtos;
using TaskRotationApi.Services;

namespace TaskRotationApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UsersController(TaskAssignmentService service) : ControllerBase
{
    /// <summary>
    /// Returns all registered users.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<UserResponse>))]
    public ActionResult<IReadOnlyCollection<UserResponse>> GetUsers()
    {
        return Ok(service.GetUsers());
    }

    /// <summary>
    /// Returns a single user by identifier.
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(UserResponse))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult<UserResponse> GetUser(Guid id)
    {
        var user = service.GetUser(id);

        if (user is null)
        {
            return NotFound(new { message = "User not found." });
        }

        return Ok(user);
    }

    /// <summary>
    /// Creates a new user.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(UserResponse))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public ActionResult<UserResponse> CreateUser([FromBody] CreateUserRequest request)
    {
        var result = service.CreateUser(request.Name);

        if (!result.Success)
        {
            return MapError(new ServiceResult(result.Success, result.Code, result.Error));
        }

        return CreatedAtAction(nameof(GetUser), new { id = result.Value!.Id }, result.Value);
    }

    /// <summary>
    /// Deletes a user and releases their tasks.
    /// </summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult DeleteUser(Guid id)
    {
        var result = service.DeleteUser(id);

        if (!result.Success)
        {
            return MapError(result);
        }

        return NoContent();
    }

    private IActionResult MapError(ServiceResult result) => result.Code switch
    {
        ErrorCode.Duplicate => Conflict(new { message = result.Error }),
        ErrorCode.NotFound => NotFound(new { message = result.Error }),
        ErrorCode.Invalid or ErrorCode.LimitReached => BadRequest(new { message = result.Error }),
        _ => BadRequest(new { message = result.Error ?? "An unexpected error occurred." })
    };
}
