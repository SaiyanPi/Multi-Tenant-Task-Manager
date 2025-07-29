namespace MultiTenantTaskManager.DTOs.Notification;

public class CommentNotificationDto : NotificationDto
{
    public Guid CommentId { get; set; }
    public string SenderName { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public int? TaskItemId { get; set; }
    public string? TaskName { get; set; }
    public int? ProjectId { get; set; }
     public string? ProjectName { get; set; }
    // public List<string> TargetUserIds { get; set; } = new List<string>();

    public CommentNotificationDto()
    {
        Type = "comment"; // Discriminator
    }
}
