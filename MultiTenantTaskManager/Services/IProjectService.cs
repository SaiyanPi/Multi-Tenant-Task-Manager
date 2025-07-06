using MultiTenantTaskManager.DTOs;
using MultiTenantTaskManager.DTOs.Project;
using MultiTenantTaskManager.DTOs.TaskItem;
using MultiTenantTaskManager.Models;

namespace MultiTenantTaskManager.Services;

public interface IProjectService
{
    Task<IEnumerable<ProjectDto>> GetAllProjectsAsync(int page = 1, int pageSize = 10);
    Task<ProjectDto?> GetProjectByIdAsync(int projectId);
    Task<ProjectDto> CreateProjectAsync(CreateProjectDto dto);
    Task<ProjectDto> UpdateProjectAsync(int projectId, UpdateProjectDto dto);
    Task<bool> DeleteProjectAsync(int projectId);

    Task<ProjectDto> AssignUsersToProjectAsync(AssignUsersToProjectDto dto);
    Task<bool> UpdateProjectStatusAsync(int projectId, UpdateProjectStatusDto dto);


}
