using Microsoft.AspNetCore.Mvc;
using MultiTenantTaskManager.Models;
using MultiTenantTaskManager.Services;

namespace MultiTenantTaskManager.Controllers;

[ApiController]
[Route("api/[controller]")]
// [SkipTenantResolution] 
public class TasksController : ControllerBase
{
    private readonly ITaskItemService _taskService;
    public TasksController(ITaskItemService taskService)
    {
        _taskService = taskService ?? throw new ArgumentNullException(nameof(taskService));
    }

     // GET:/api/tasks
    [HttpGet]
    public async Task<ActionResult<IEnumerable<TaskItem>>> GetAllTasks()
    {
        var tasks = await _taskService.GetAllTaskAsync();

        return Ok(tasks);
    }

    // GET:/api/tasks/{id}
    [HttpGet("{id}")]
    public async Task<ActionResult<TaskItem>> GetTaskById(int id)
    {
        var task = await _taskService.GetTaskByIdAsync(id);
        if (task == null) return NotFound($"Task with ID {id} not found.");

        return Ok(task);
    }

    // POST:/api/tasks
    [HttpPost]
    public async Task<ActionResult<TaskItem>> CreateTask([FromBody] TaskItem taskItem)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        try
        {
            var createdTask = await _taskService.CreateTaskAsync(taskItem);
            return CreatedAtAction(nameof(GetTaskById), new { id = createdTask.Id }, createdTask);
        }
        catch (InvalidOperationException ex)
        {
            // Return 409 Conflict with a user-friendly message
            return Conflict(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            // Catch other unexpected errors (optional but good practice)
            return StatusCode(500, new { message = "An unexpected error occurred.", detail = ex.Message });
        }
    }

    // PUT:/api/tasks/{id}
    [HttpPut("{id}")]
    public async Task<ActionResult<TaskItem>> UpdateTask(int id, [FromBody] TaskItem taskItem)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        if(id != taskItem.Id) return BadRequest("Task ID in the URL does not match the ID in the body.");

        try
        {
            var updatedTask = await _taskService.UpdateTaskAsync(id, taskItem);
            return Ok(updatedTask);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound($"Task with ID {id} not found. {ex.Message}");
        }
    }

    // DELETE:/api/tasks/{id}
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteTask(int id)
    {
        var deleted = await _taskService.DeleteTaskAsync(id);
        if (!deleted) return NotFound($"Task with ID {id} not found.");
        return NoContent();
    }
}
