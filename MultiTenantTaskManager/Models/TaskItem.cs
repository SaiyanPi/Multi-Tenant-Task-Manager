using MultiTenantTaskManager.Services;

namespace MultiTenantTaskManager.Models;

public class TaskItem : ISoftDeletable
{
    // Primary key
    public int Id { get; set; }
    public string Titles { get; set; } = string.Empty;

    public bool IsDeleted { get; set; }     // for soft deletion
    public DateTime? DeletedAt { get; set; }    // for soft deletion
    public string? DeletedBy { get; set; }  // for soft deletion

    // Foreign key to Project
    public int ProjectId { get; set; }
    // Reference navigation property to Project
    public Project? Project { get; set; } = null!;


    // Foreign key to Tenant
    public Guid TenantId { get; set; }
    // Reference navigation property to Tenant
    public Tenant? Tenant { get; set; } = null!;

}