using Microsoft.AspNetCore.SignalR;
using MultiTenantTaskManager.Data;
using MultiTenantTaskManager.DTOs.Notification;
using MultiTenantTaskManager.Hubs;
using MultiTenantTaskManager.Mappers;
using MultiTenantTaskManager.Models;

namespace MultiTenantTaskManager.Services;

public class NotificationService : INotificationService
{
    private readonly IHubContext<NotificationHub> _hubContext;
    private readonly ApplicationDbContext _context;

    public NotificationService(IHubContext<NotificationHub> hubContext, ApplicationDbContext context)
    {
        _hubContext = hubContext;
        _context = context;
    }

    public async Task SendNotificationAsync(string userId, string title, string message)
    {
        var notification = new Notification
        {
            UserId = userId,
            Title = title,
            Message = message
        };

        _context.Notifications.Add(notification);
        await _context.SaveChangesAsync();

        // Send to SignalR
        // await _hubContext.Clients.User(userId).SendAsync("ReceiveNotification", new
        // {
        //     notification.Id,
        //     notification.Title,
        //     notification.Message,
        //     notification.CreatedAt
        // });
        // await _hubContext.Clients.User(userId).SendAsync("ReceiveNotification", notification.ToNotificationDto());
        await _hubContext.Clients.User(userId).SendAsync("ReceiveNotification", NotificationMapper.ToNotificationDto(notification));

    }
}