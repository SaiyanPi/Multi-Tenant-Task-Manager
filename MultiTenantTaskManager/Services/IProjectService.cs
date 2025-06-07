using MultiTenantTaskManager.Models;

namespace MultiTenantTaskManager.Services;

public interface IProjectService
{
    Task<IEnumerable<Project>> GetAllProjectsAsync();
    Task<Project?> GetProjectByIdAsync(int projectId);
    Task<Project> CreateProjectAsync(Project project);
    Task<Project> UpdateProjectAsync(int projectId, Project project);
    Task<bool> DeleteProjectAsync(int projectId);
}
