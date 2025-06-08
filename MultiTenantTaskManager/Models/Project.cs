namespace MultiTenantTaskManager.Models;

public class Project
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
    
}