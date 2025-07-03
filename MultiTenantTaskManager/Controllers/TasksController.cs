using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MultiTenantTaskManager.Authentication;
using MultiTenantTaskManager.DTOs.TaskItem;
using MultiTenantTaskManager.Models;
using MultiTenantTaskManager.Services;

namespace MultiTenantTaskManager.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TasksController : ControllerBase
{
    private readonly ITaskItemService _taskService;
    public TasksController(ITaskItemService taskService)
    {
        _taskService = taskService ?? throw new ArgumentNullException(nameof(taskService));

    }

    // GET:/api/tasks
    [Authorize(Policy = "canViewTasks")]
    [HttpGet]
    public async Task<ActionResult<IEnumerable<TaskItemDto>>> GetAllTasks()
    {
        var tasks = await _taskService.GetAllTaskAsync();

        return Ok(tasks);
    }

    // GET:/api/tasks/{id}
    [Authorize(Policy = "canViewTasks")]
    [HttpGet("{id}")]
    public async Task<ActionResult<TaskItemDto>> GetTaskById(int id)
    {
        var task = await _taskService.GetTaskByIdAsync(id);
        if (task == null) return NotFound($"Task with ID {id} not found.");

        return Ok(task);
    }

    // POST:/api/tasks
    [Authorize(Policy = "canManageTasks")]
    [HttpPost]
    public async Task<ActionResult<TaskItemDto>> CreateTask([FromBody] CreateTaskItemDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        try
        {
            var createdTask = await _taskService.CreateTaskAsync(dto);
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
    [Authorize(Policy = "canManageTasks")]
    [HttpPut("{id}")]
    public async Task<ActionResult<TaskItemDto>> UpdateTask(int id, [FromBody] UpdateTaskItemDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        if (id != dto.Id) return BadRequest("Task ID in the URL does not match the ID in the body.");

        try
        {
            var updatedTask = await _taskService.UpdateTaskAsync(id, dto);
            return Ok(updatedTask);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound($"Task with ID {id} not found. {ex.Message}");
        }
    }

    // DELETE:/api/tasks/{id}
    [Authorize(Policy = "canManageTasks")]
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteTask(int id)
    {
        var deleted = await _taskService.DeleteTaskAsync(id);
        if (!deleted) return NotFound($"Task with ID {id} not found.");
        return NoContent();
    }

    // assign a user to a task
    // POST: /api/tasks/assign
    [Authorize(Policy = "canManageTasks")]
    [HttpPost("assign")]
    public async Task<ActionResult<TaskItemDto>> AssignUser([FromBody] AssignUserToTaskDto dto)
    {
        //Console.WriteLine("assign endpoint is hitting");
        var assignResult = await _taskService.AssignUserToTaskAsync(dto);
        
        return Ok(assignResult);
    }

    // update the task status
    // PATCH: /api/tasks/1/status
    [Authorize(Policy = "canManageTasks")]
    [HttpPatch("{id}/status")]
    public async Task<IActionResult> UpdateTaskStatus(int id, [FromBody] UpdateTaskItemStatusDto dto)
    {
        try
        {
            var result = await _taskService.UpdateTaskStatusAsync(id, dto);
            if (result)
                return NoContent();

            return BadRequest("Failed to update status");
        }
        catch (UnauthorizedAccessException ex)
        {
            return Forbid(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            return NotFound(ex.Message);
        }
    }

}
