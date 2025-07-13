using MultiTenantTaskManager.DTOs.Notification;
using MultiTenantTaskManager.Models;

namespace MultiTenantTaskManager.Mappers;

public static class NotificationMapper
{
    public static NotificationDto ToNotificationDto(this Notification notification)
    {
        return new NotificationDto
        {
            Id = notification.Id,
            Title = notification.Title,
            Message = notification.Message,
            CreatedAt = notification.CreatedAt,
            IsRead = notification.IsRead,
            TenantId = notification.TenantId,
            UserId = notification.UserId
        };
    }
}