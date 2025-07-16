using MultiTenantTaskManager.Authentication;
using MultiTenantTaskManager.Services;
using MultiTenantTaskManager.Enums;

namespace MultiTenantTaskManager.Models;

public class TaskItem : ISoftDeletable, IAuditable
{
    // Primary key
    public int Id { get; set; }
    public string Titles { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;



    // Foreign key to Project
    public int ProjectId { get; set; }
    // Reference navigation property to Project
    public Project? Project { get; set; } = null!;


    // Foreign key to Tenant
    public Guid TenantId { get; set; }
    // Reference navigation property to Tenant
    public Tenant? Tenant { get; set; } = null!;


    // task assign to user
    public string? AssignedUserId { get; set; }  // Foreign key to Identity user
    public ApplicationUser? AssignedUser { get; set; } // Navigation property
    public TaskItemStatus Status { get; set; } = TaskItemStatus.Unassigned;
    public DateTime? DueDate { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }


    // comments related to the task
    public ICollection<Comment> Comments { get; set; } = new List<Comment>();

    public bool IsDeleted { get; set; }     // for soft deletion
    public DateTime? DeletedAt { get; set; }    // for soft deletion
    public string DeletedBy { get; set; }  = string.Empty; // for soft deletion


}