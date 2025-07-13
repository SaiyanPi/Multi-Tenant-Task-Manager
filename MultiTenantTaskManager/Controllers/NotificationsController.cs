using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MultiTenantTaskManager.DTOs.Notification;
using MultiTenantTaskManager.Services;

namespace MultiTenantTaskManager.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
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

    [HttpGet("{tenantId}/{userId}")]
    public async Task<ActionResult<IEnumerable<NotificationDto>>> GetUserNotifications(Guid tenantId, string userId)
    {
        var notifications = await _notificationService.GetUserNotificationsAsync(tenantId, userId);
        return Ok(notifications);
    }

    [HttpPut("{tenantId}/{userId}/{notificationId}")]
    public async Task<IActionResult> MarkAsRead(Guid tenantId, string userId, Guid notificationId)
    {
        await _notificationService.MarkAsReadAsync(tenantId, userId, notificationId);
        return Ok();
    }
}