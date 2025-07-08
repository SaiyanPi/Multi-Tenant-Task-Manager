using MultiTenantTaskManager.DTOs.Project;
using MultiTenantTaskManager.DTOs.Tenant;
using MultiTenantTaskManager.Helpers;
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
            Description = project.Description.Truncate(30),
            TenantId = project.TenantId,

            // assigned
            Tasks = project.Tasks?.Select(t => t.ToTaskItemDto()).ToList() ?? new(),
            AssignedUsers = project.AssignedUsers?
            .Select(u => new ProjectUserDto
            {
                UserId = u.Id,
                Email = u.Email ?? string.Empty,
                RoleInProject = u.RoleInProject ?? string.Empty
            }).ToList() ?? new(),
            Status = project.Status,
            DueDate = project.DueDate,
            CreatedAt = project.CreatedAt,
            StartedAt = project.StartedAt,
            CompletedAt = project.CompletedAt

            // soft delete properties
            // IsDeleted = project.IsDeleted,
            // DeletedAt = project.DeletedAt,
            // DeletedBy = project.DeletedBy,
        };
    }


    public static Project ToProjectModel(this CreateProjectDto dto)
    {
        if (dto == null) throw new ArgumentNullException(nameof(dto));

        return new Project
        {
            Name = dto.Name,
            Description = dto.Description,
            DueDate = dto.DueDate

        };
    }

    public static void UpdateFromDto(this Project project, UpdateProjectDto dto)
    {
        if (dto == null) throw new ArgumentNullException(nameof(dto));

        project.Name = dto.Name;
        project.Description = dto.Description;
    }

}
