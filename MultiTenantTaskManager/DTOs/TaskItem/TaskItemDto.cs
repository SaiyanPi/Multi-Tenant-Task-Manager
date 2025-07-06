using MultiTenantTaskManager.Authentication;
using MultiTenantTaskManager.DTOs.Project;
using MultiTenantTaskManager.DTOs.Tenant;
using MultiTenantTaskManager.Enums;

namespace MultiTenantTaskManager.DTOs.TaskItem;

public class TaskItemDto
{
    // Primary key
    public int Id { get; set; }
    public string Titles { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;


    // Foreign key to Project
    public int ProjectId { get; set; }
    // Reference navigation property to Project
    // public ProjectDto? Project { get; set; } = null!;


    // Foreign key to Tenant
    public Guid TenantId { get; set; }
    // Reference navigation property to Tenant
    // public TenantDto? Tenant { get; set; } = null!;
    
    // task assign to user
    public string? AssignedUserId { get; set; }
    public string? AssignedUserEmail { get; set; } //optional
    public TaskItemStatus Status { get; set; }
    public DateTime? DueDate { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }

    // soft delete- no need to expose
    // public bool IsDeleted { get; set; }
    // public DateTime? DeletedAt { get; set; }
    // public string? DeletedBy { get; set; }

}