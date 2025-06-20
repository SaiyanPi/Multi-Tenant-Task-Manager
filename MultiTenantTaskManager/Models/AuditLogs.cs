namespace MultiTenantTaskManager.Models;

public class AuditLog
{
    public int Id { get; set; }

    public string UserId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;

    // Action performed (e.g., "Create", "Update", "Delete")
    public string Action { get; set; } = string.Empty;

    // Entity name (e.g., "Project", "TaskItem")
    public string EntityName { get; set; } = string.Empty;

    // Entity ID (e.g., Project ID, TaskItem ID)
    public string EntityId { get; set; } = string.Empty;

    // Timestamp of the action
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public Guid? TenantId { get; set; } // nullable because super admin does not belong to any tenant
    public string Changes { get; set; } = string.Empty; // JSON or text representation of changes made
}