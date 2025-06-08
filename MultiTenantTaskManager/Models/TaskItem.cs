namespace MultiTenantTaskManager.Models;

public class TaskItem
{
    // Primary key
    public int Id { get; set; }
    public string Titles { get; set; } = string.Empty;


    // Foreign key to Project
    public int ProjectId { get; set; }
    // Reference navigation property to Project
    public Project? Project { get; set; } = null!;


    // Foreign key to Tenant
    public Guid TenantId { get; set; }
    // Reference navigation property to Tenant
    public Tenant? Tenant { get; set; } = null!;

}