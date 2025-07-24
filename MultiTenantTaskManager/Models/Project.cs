using MultiTenantTaskManager.Authentication;
using MultiTenantTaskManager.Enums;
using MultiTenantTaskManager.Services;

namespace MultiTenantTaskManager.Models;

public class Project : ISoftDeletable, IAuditable
{
    // Primary key
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;


    // Foreign key to Tenant
    public Guid TenantId { get; set; }
    // Reference navigation property to Tenant
    public Tenant? Tenant { get; set; } = null!;


    // Collection navigation property for related TaskItems
    public ICollection<TaskItem> Tasks { get; set; } = new List<TaskItem>(); 


    // One-to-many: A project has many users
    public ICollection<ApplicationUser> AssignedUsers { get; set; } = new List<ApplicationUser>();  // collection navigation property from ApplicationUser

    public ProjectStatus Status { get; set; } = ProjectStatus.Unassigned;
    public DateTime? DueDate { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }   
    

    // comments related to the project
    public ICollection<Comment> Comments { get; set; } = new List<Comment>();
    
    public bool IsDeleted { get; set; }     // for soft deletion
    public DateTime? DeletedAt { get; set; }    // for soft deletion
    public string DeletedBy { get; set; } = string.Empty; // for soft deletion

}