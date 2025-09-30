using Microsoft.AspNetCore.Mvc;
using TaskRotationApi.Dtos;
using TaskRotationApi.Services;

namespace TaskRotationApi.Controllers;

[ApiController]
[Route("api/[controller]")]
/// <summary>
///     Provides endpoints for inspecting and creating tasks in the rotation system.
/// </summary>
public class TasksController(TaskAssignmentService service) : ControllerBase
{
    /// <summary>
    ///     Returns all tasks.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<TaskResponse>))]
    public ActionResult<IReadOnlyCollection<TaskResponse>> GetTasks()
    {
        return Ok(service.GetTasks());
    }

    /// <summary>
    ///     Returns a single task by identifier.
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(TaskResponse))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult<TaskResponse> GetTask(Guid id)
    {
        var task = service.GetTask(id);
        if (task is null) return NotFound(new { message = "Task not found." });

        return Ok(task);
    }

    /// <summary>
    ///     Creates a new task.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(TaskResponse))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public ActionResult<TaskResponse> CreateTask([FromBody] CreateTaskRequest request)
    {
        var result = service.CreateTask(request.Title);
        if (!result.Success)
            return MapError<TaskResponse>(new ServiceResult(result.Success, result.Code, result.Error));

        return CreatedAtAction(nameof(GetTask), new { id = result.Value!.Id }, result.Value);
    }

    /// <summary>
    ///     Converts a <see cref="ServiceResult"/> to a typed HTTP response.
    /// </summary>
    /// <typeparam name="T">The expected payload type.</typeparam>
    /// <param name="result">The operation outcome to translate.</param>
    /// <returns>The HTTP representation of the <paramref name="result"/>.</returns>
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
    ///     Converts a non-generic <see cref="ServiceResult"/> to an HTTP response.
    /// </summary>
    /// <param name="result">The operation outcome to translate.</param>
    /// <returns>The HTTP representation of the <paramref name="result"/>.</returns>
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