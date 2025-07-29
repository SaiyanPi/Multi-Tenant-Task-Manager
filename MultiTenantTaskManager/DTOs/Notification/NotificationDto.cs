namespace MultiTenantTaskManager.DTOs.Notification;

public class NotificationDto
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string UserId { get; set; } = default!;
    public string Title { get; set; } = default!;
    public string Message { get; set; } = default!;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsRead { get; set; } = false;

    // Optional: to filter by tenantId
    public Guid? TenantId { get; set; }

    // NEW: Type of notification (e.g. comment, assignment etc.)
    public string Type { get; set; } = "generic";

  
}