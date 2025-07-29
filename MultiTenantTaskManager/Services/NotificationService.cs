using System;
using System.Security.Claims;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using MultiTenantTaskManager.Accessor;
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
    private readonly ITenantAccessor _tenantAccessor;

    public NotificationService(IHubContext<NotificationHub> hubContext,
        ITenantAccessor tenantAccessor, ApplicationDbContext context)
    {
        _hubContext = hubContext;
        _tenantAccessor = tenantAccessor;
        _context = context;
    }

    // public async Task SendNotificationAsync(string userId, string title, string message)
    // {
    //     var userTenant = _context.Users
    //         .Where(u => u.Id == userId)
    //         .Select(u => u.TenantId)
    //         .FirstOrDefault();

    //     var notification = new Notification
    //     {
    //         UserId = userId,
    //         Title = title,
    //         Message = message,
    //         TenantId = userTenant
    //     };

    //     _context.Notifications.Add(notification);
    //     await _context.SaveChangesAsync();

    //     // Send to SignalR
    //     // await _hubContext.Clients.User(userId).SendAsync("ReceiveNotification", new
    //     // {
    //     //     notification.Id,
    //     //     notification.Title,
    //     //     notification.Message,
    //     //     notification.CreatedAt
    //     // });
    //     // await _hubContext.Clients.User(userId).SendAsync("ReceiveNotification", notification.ToNotificationDto());
    //     await _hubContext.Clients.User(userId).SendAsync("ReceiveNotification", NotificationMapper.ToNotificationDto(notification));

    // }

    public async Task SendNotificationAsync(string userId, object dto)
    {
        var userTenant = _context.Users
            .Where(u => u.Id == userId)
            .Select(u => u.TenantId)
            .FirstOrDefault();  

        var notification = new Notification
        {
            UserId = userId,
            Title = (dto as NotificationDto)?.Title ?? "Untitled",
            Message = (dto as NotificationDto)?.Message ?? "",
            TenantId = userTenant,
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
        // await _hubContext.Clients.User(userId).SendAsync("ReceiveNotification", NotificationMapper.ToNotificationDto(notification));
        await _hubContext.Clients.User(userId).SendAsync("ReceiveNotification", dto);
    }
    
    public async Task<IEnumerable<NotificationDto>> GetUserNotificationsAsync(Guid tenantId, string userId)
    {
        var notifications = await _context.Notifications
            .Where(n => n.TenantId == tenantId && n.UserId == userId)
            .OrderByDescending(n => n.CreatedAt)
            .ToListAsync();

        return notifications.Select(NotificationMapper.ToNotificationDto);
    }

    public async Task MarkAsReadAsync(Guid tenantId, string userId, Guid notificationId)
    {
        var notification = await _context.Notifications
            .FirstOrDefaultAsync(n => n.TenantId == tenantId && n.UserId == userId && n.Id == notificationId);

        if (notification == null)
            throw new KeyNotFoundException("Notification not found.");

        notification.IsRead = true;
        await _context.SaveChangesAsync();

    }

    // outside tenant scope
    public async Task<IEnumerable<NotificationDto>> GetAllNotificationsAsync()
    {
        var notifications = await _context.Notifications
            .OrderByDescending(n => n.CreatedAt)
            .ToListAsync();

        return notifications.Select(NotificationMapper.ToNotificationDto);
    }

     // outside tenant scope
    public async Task<IEnumerable<NotificationDto>> GetUnreadNotificationsAsync()
    {
        var notifications = await _context.Notifications
            .Where(n => !n.IsRead)
            .OrderByDescending(n => n.CreatedAt)
            .ToListAsync();

        return notifications.Select(NotificationMapper.ToNotificationDto);
    }

    // tenant scope 
    public async Task<IEnumerable<NotificationDto>> GetAllNotificationsForTenantAsync(Guid tenantId)
    {
        var notifications = await _context.Notifications
            .Where(n => n.TenantId == tenantId)
            .OrderByDescending(n => n.CreatedAt)
            .ToListAsync();

        return notifications.Select(NotificationMapper.ToNotificationDto);
    }

    // tenant scope
    public async Task<IEnumerable<NotificationDto>> GetUnreadNotificationsForTenantAsync(Guid tenantId)
    {
        var notifications = await _context.Notifications
            .Where(n => n.TenantId == tenantId && !n.IsRead)
            .OrderByDescending(n => n.CreatedAt)
            .ToListAsync();

        return notifications.Select(NotificationMapper.ToNotificationDto);
    }
}