using MultiTenantTaskManager.DTOs.Project;
using MultiTenantTaskManager.DTOs.Tenant;
using MultiTenantTaskManager.Models;

namespace  MultiTenantTaskManager.Mappers;

public static class ProjectMapper
{
    public static ProjectDto ToProjectDto(this Project project)
    {
        return new ProjectDto
        {
            Id = project.Id,
            Name = project.Name,
            TenantId = project.TenantId,
            // assigned
            // soft delete properties
            IsDeleted = project.IsDeleted,
            DeletedAt = project.DeletedAt,
            DeletedBy = project.DeletedBy,
            Tasks = project.Tasks?.Select(t => t.ToTaskItemDto()).ToList() ?? new(),

            AssignedUsers = project.AssignedUsers?
            .Select(u => new ProjectUserDto
            {
                UserId = u.Id,
                Email = u.Email ?? string.Empty,
                RoleInProject = u.RoleInProject ?? string.Empty
            }).ToList() ?? new()
        };
    }


    public static Project ToProjectModel(this CreateProjectDto dto)
    {
        if (dto == null) throw new ArgumentNullException(nameof(dto));

        return new Project
        {
            Name = dto.Name,
        };
    }
    
    public static void UpdateFromDto(this Project project, UpdateProjectDto dto)
    {
        if (dto == null) throw new ArgumentNullException(nameof(dto));

        project.Name = dto.Name;
    }
}
