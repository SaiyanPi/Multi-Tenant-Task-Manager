using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MultiTenantTaskManager.Authentication;
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
    public async Task<ActionResult<IEnumerable<Project>>> GetAllProjects()
    {
        var projects = await _projectService.GetAllProjectsAsync();

        return Ok(projects);
    }

    // GET:/api/projects/{id}
     [Authorize(Policy = "canManageProjects")]
    [HttpGet("{id}")]
    public async Task<ActionResult<Project>> GetProjectById(int id)
    {
        var project = await _projectService.GetProjectByIdAsync(id);
        if (project == null) return NotFound($"Project with ID {id} not found.");

        return Ok(project);
    }

    // POST:/api/projects
     [Authorize(Policy = "canManageProjects")]
    [HttpPost]
    public async Task<ActionResult<Project>> CreateProject([FromBody] Project project)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        // var createdProject = await _projectService.CreateProjectAsync(project);
        // return CreatedAtAction(nameof(GetTenantById), new { id = createdProject.Id }, createdProject);

        try
        {
            var createdProject = await _projectService.CreateProjectAsync(project);
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
    public async Task<ActionResult<Project>> UpdateProject(int id, [FromBody] Project project)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        if(id != project.Id) return BadRequest("Project ID in the URL does not match the ID in the body.");

        try
        {
            var updatedProject = await _projectService.UpdateProjectAsync(id, project);
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
}
