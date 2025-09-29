using Microsoft.AspNetCore.Mvc;
using TaskRotationApi.Dtos;
using TaskRotationApi.Services;

namespace TaskRotationApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TasksController(TaskAssignmentService service) : ControllerBase
{
    [HttpGet]
    public ActionResult<IReadOnlyCollection<TaskResponse>> GetTasks()
    {
        return Ok(service.GetTasks());
    }

    [HttpGet("{id:guid}")]
    public ActionResult<TaskResponse> GetTask(Guid id)
    {
        var task = service.GetTask(id);
        if (task is null)
        {
            return NotFound();
        }

        return Ok(task);
    }

    [HttpPost]
    public ActionResult<TaskResponse> CreateTask([FromBody] CreateTaskRequest request)
    {
        var (success, error, task) = service.CreateTask(request.Title);
        if (!success)
        {
            return string.Equals(error, "A task with the same title already exists.", StringComparison.Ordinal)
                ? Conflict(new { message = error })
                : BadRequest(new { message = error });
        }

        return CreatedAtAction(nameof(GetTask), new { id = task!.Id }, task);
    }
}
