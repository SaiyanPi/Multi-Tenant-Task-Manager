using MultiTenantTaskManager.DTOs.TaskItem;
using MultiTenantTaskManager.DTOs.Tenant;
using MultiTenantTaskManager.Enums;

namespace MultiTenantTaskManager.DTOs.Project;

public class ProjectDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    // Foreign key to Tenant
    public Guid TenantId { get; set; }

    // Users assigned to this project
    public List<ProjectUserDto> AssignedUsers { get; set; } = new();
    public ProjectStatus Status { get; set; }
    public DateTime? DueDate { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }

    // Reference navigation property to Tenant
    // public TenantDto? Tenant { get; set; } = null!;

    // Collection navigation property for related TaskItems
    public ICollection<TaskItemDto> Tasks { get; set; } = new List<TaskItemDto>();


    // soft delete
    // public bool IsDeleted { get; set; }
    // public DateTime? DeletedAt { get; set; }
    // public string? DeletedBy { get; set; }
}