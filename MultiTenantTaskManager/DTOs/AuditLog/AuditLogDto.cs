namespace MultiTenantTaskManager.DTOs.AuditLog;

public class AuditLogDto
{
    public int Id { get; set; }
    public string Action { get; set; } = string.Empty;
    public Guid? TenantId { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string EntityName { get; set; } = string.Empty;
    public string EntityId { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public string Changes { get; set; } = string.Empty;

}