using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using MultiTenantTaskManager.DTOs.Notification;
using MultiTenantTaskManager.Services;

namespace MultiTenantTaskManager.Controllers;

[ApiController]
[Route("api/[controller]")]
public class NotificationsController : ControllerBase
{
    private readonly INotificationService _notificationService;

    public NotificationsController(INotificationService notificationService)
    {
        _notificationService = notificationService;
    }


    // [HttpGet("user")]
    // public async Task<ActionResult<IEnumerable<NotificationDto>>> GetUserNotifications()
    // {
    //     // extract userId from JWT claims
    //     var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    //     var notifications = await _notificationService.GetUserNotificationsAsync(userId);
    //     return Ok(notifications);
    // }

    // [HttpPut("{notificationId}/read")]
    // public async Task<IActionResult> MarkAsRead(Guid notificationId)
    // {
    //     // extract userId from JWT claims
    //     var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    //     await _notificationService.MarkAsReadAsync(notificationId, userId);
    //     return Ok();
    // }

    [Authorize]
    [HttpGet("{tenantId}/{userId}")]
    public async Task<ActionResult<IEnumerable<NotificationDto>>> GetUserNotifications(Guid tenantId, string userId)
    {
        var notifications = await _notificationService.GetUserNotificationsAsync(tenantId, userId);
        return Ok(notifications);
    }

    [Authorize]
    [HttpPut("{tenantId}/{userId}/{notificationId}")]
    public async Task<IActionResult> MarkAsRead(Guid tenantId, string userId, Guid notificationId)
    {
        await _notificationService.MarkAsReadAsync(tenantId, userId, notificationId);
        return Ok();
    }

    // outside tenant scope
    [Authorize(Policy = "canManageTenants")] // super-admin
    [HttpGet]
    public async Task<ActionResult<IEnumerable<NotificationDto>>> GetAllNotificationsAsync()
    {
        var notifications = await _notificationService.GetAllNotificationsAsync();
        return Ok(notifications);
    }

    [Authorize(Policy = "canManageTenants")] // super-admin
    [HttpGet("unread")]
    public async Task<ActionResult<IEnumerable<NotificationDto>>> GetUnreadNotificationsAsync()
    {
        var notifications = await _notificationService.GetUnreadNotificationsAsync();
        return Ok(notifications);
    }

    [Authorize(Policy = "canManageProjects")] // admin
    [HttpGet("{tenantId}")]
    public async Task<ActionResult<IEnumerable<NotificationDto>>> GetAllNotificationsForTenantAsync(Guid tenantId)
    {
        var notifications = await _notificationService.GetAllNotificationsForTenantAsync(tenantId);
        return Ok(notifications);
    }

    
    [Authorize(Policy = "canManageProjects")] // admin
    [HttpGet("{tenantId}/unread")]
    public async Task<ActionResult<IEnumerable<NotificationDto>>> GetUnreadNotificationsForTenantAsync(Guid tenantId)
    {
        var notifications = await _notificationService.GetUnreadNotificationsForTenantAsync(tenantId);
        return Ok(notifications);
    }


}