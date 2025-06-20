using MultiTenantTaskManager.DTOs.Project;
using MultiTenantTaskManager.DTOs.Tenant;

namespace MultiTenantTaskManager.DTOs.TaskItem;

public class TaskItemDto
{
    // Primary key
    public int Id { get; set; }
    public string Titles { get; set; } = string.Empty;


    // Foreign key to Project
    public int ProjectId { get; set; }
    // Reference navigation property to Project
    // public ProjectDto? Project { get; set; } = null!;


    // Foreign key to Tenant
    public Guid TenantId { get; set; }
    // Reference navigation property to Tenant
    // public TenantDto? Tenant { get; set; } = null!;
    

    // soft delete
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public string? DeletedBy { get; set; }

}