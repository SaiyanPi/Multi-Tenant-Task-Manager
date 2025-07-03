using MultiTenantTaskManager.Authentication;
using MultiTenantTaskManager.Services;

namespace MultiTenantTaskManager.Models;

public class Project : ISoftDeletable
{
    // Primary key
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;


    // Foreign key to Tenant
    public Guid TenantId { get; set; }
    // Reference navigation property to Tenant
    public Tenant? Tenant { get; set; } = null!;


    // Collection navigation property for related TaskItems
    public ICollection<TaskItem> Tasks { get; set; } = new List<TaskItem>();


    // One-to-many: A project has many users
    public ICollection<ApplicationUser> AssignedUsers { get; set; } = new List<ApplicationUser>();    
    
    
    public bool IsDeleted { get; set; }     // for soft deletion
    public DateTime? DeletedAt { get; set; }    // for soft deletion
    public string? DeletedBy { get; set; }  // for soft deletion
    
}