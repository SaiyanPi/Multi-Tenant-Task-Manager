using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MultiTenantTaskManager.Authentication;
using MultiTenantTaskManager.DTOs.Project;
using MultiTenantTaskManager.DTOs.TaskItem;
using MultiTenantTaskManager.Models;
using MultiTenantTaskManager.Services;

namespace MultiTenantTaskManager.Controllers;

[ApiController]
[Route("api/[controller]")]
// [SkipTenantResolution] 
public class ProjectsController : ControllerBase
{
    private readonly IProjectService _projectService;

    public ProjectsController(IProjectService projectService)
    {
        _projectService = projectService ?? throw new ArgumentNullException(nameof(projectService));
    }

    // GET:/api/projects
    [Authorize(Policy = "canManageProjects")]
    [HttpGet]
    public async Task<ActionResult<IEnumerable<ProjectDto>>> GetAllProjects()
    {
        var projects = await _projectService.GetAllProjectsAsync();

        return Ok(projects);
    }

    // GET:/api/projects/{id}
    [Authorize(Policy = "canManageProjects")]
    [HttpGet("{id}")]
    public async Task<ActionResult<ProjectDto>> GetProjectById(int id)
    {
        var project = await _projectService.GetProjectByIdAsync(id);
        if (project == null) return NotFound($"Project with ID {id} not found.");

        return Ok(project);
    }

    // POST:/api/projects
    [Authorize(Policy = "canManageProjects")]
    [HttpPost]
    public async Task<ActionResult<ProjectDto>> CreateProject([FromBody] CreateProjectDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        // var createdProject = await _projectService.CreateProjectAsync(project);
        // return CreatedAtAction(nameof(GetTenantById), new { id = createdProject.Id }, createdProject);

        try
        {
            var createdProject = await _projectService.CreateProjectAsync(dto);
            return CreatedAtAction(nameof(GetProjectById), new { id = createdProject.Id }, createdProject);
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

    // PUT:/api/projects/{id}
    [Authorize(Policy = "canManageProjects")]
    [HttpPut("{id}")]
    public async Task<ActionResult<ProjectDto>> UpdateProject(int id, [FromBody] UpdateProjectDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        if (id != dto.Id) return BadRequest("Project ID in the URL does not match the ID in the body.");

        try
        {
            var updatedProject = await _projectService.UpdateProjectAsync(id, dto);
            return Ok(updatedProject);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound($"Project with ID {id} not found. {ex.Message}");
        }
    }

    // DELETE:/api/Projects/{id}
    [Authorize(Policy = "canManageProjects")]
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteProject(int id)
    {
        var deleted = await _projectService.DeleteProjectAsync(id);
        if (!deleted) return NotFound($"Project with ID {id} not found.");
        return NoContent();
    }

    [Authorize(Policy = "canManageProjects")]
    [HttpPost("assign")]
    public async Task<ActionResult<ProjectDto>> AssignUsers([FromBody] AssignUsersToProjectDto dto)
    {
        var assignResult = await _projectService.AssignUsersToProjectAsync(dto);

        return Ok(assignResult);
    }
    
     // update the task status
    // PATCH: /api/projects/1/status
    //[Authorize(Policy = "canManageTasks")]
    [HttpPatch("{id}/status")]
    public async Task<IActionResult> UpdateProjectStatus(int id, [FromBody] UpdateProjectStatusDto dto)
    {
        try
        {
            var result = await _projectService.UpdateProjectStatusAsync(id, dto);
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
