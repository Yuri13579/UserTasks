using Microsoft.AspNetCore.Mvc;
using TaskRotationApi.Dtos;
using TaskRotationApi.Services;

namespace TaskRotationApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UsersController(TaskAssignmentService service) : ControllerBase
{
    [HttpGet]
    public ActionResult<IReadOnlyCollection<UserResponse>> GetUsers()
    {
        return Ok(service.GetUsers());
    }

    [HttpGet("{id:guid}")]
    public ActionResult<UserResponse> GetUser(Guid id)
    {
        var user = service.GetUser(id);
        if (user is null)
        {
            return NotFound();
        }

        return Ok(user);
    }

    [HttpPost]
    public ActionResult<UserResponse> CreateUser([FromBody] CreateUserRequest request)
    {
        var (success, error, user) = service.CreateUser(request.Name);
        if (!success)
        {
            return string.Equals(error, "A user with the same name already exists.", StringComparison.Ordinal)
                ? Conflict(new { message = error })
                : BadRequest(new { message = error });
        }

        return CreatedAtAction(nameof(GetUser), new { id = user!.Id }, user);
    }

    [HttpDelete("{id:guid}")]
    public IActionResult DeleteUser(Guid id)
    {
        var (success, error) = service.DeleteUser(id);
        if (!success)
        {
            return NotFound(new { message = error });
        }

        return NoContent();
    }
}
