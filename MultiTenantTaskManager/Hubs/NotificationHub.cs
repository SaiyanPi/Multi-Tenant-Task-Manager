using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR;
using MultiTenantTaskManager.Accessor;
using MultiTenantTaskManager.DTOs.Notification;
using MultiTenantTaskManager.Services;

namespace MultiTenantTaskManager.Hubs;

public class NotificationHub : Hub
{

    private readonly ITenantAccessor _tenantAccessor;
    private readonly INotificationService _notificationService;
    public const string HubUrl = "/hubs/notifications";

    public NotificationHub(ITenantAccessor tenantAccessor, INotificationService notificationService) : base()
    {
        _tenantAccessor = tenantAccessor ?? throw new ArgumentNullException(nameof(tenantAccessor));
        _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));

    }
    public override Task OnConnectedAsync()
    {
        var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        Console.WriteLine($"Connected SignalR user: {userId}");

        return base.OnConnectedAsync();
    }


    // takes a data from client when clicked 'Mark as read' button
    // Notification still works without this class because SignalR's default implementation already uses:
    // connection.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
    public async Task MarkAsRead(Guid notificationId)
    {

        // following col is commented because relying on ITenantAccessor or JWT claims or headers gives exception
        // because SignalR hub methods so not go through HTTP middleware so tenant resolution middleware does not
        // gets execute. so we must use hub context not http context to get tenantId.

        // var tenantId = _tenantAccessor.TenantId;

        var tenantId = Context.User?.FindFirst("tenant_id")?.Value; // this is not a HttpContext
        var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        //Console.WriteLine($"user tenant id: {tenantId}");

        if (userId == null || tenantId == null)
        {
            throw new HubException("Unauthorized: Missing user or tenant.");
        }


        // call the service method to update DB via service
        // await _notificationService.MarkAsReadAsync(notificationId, userId);

        await _notificationService.MarkAsReadAsync(Guid.Parse(tenantId), userId, notificationId);


        // Notify all of the user's connections to update their UI
        await Clients.User($"{tenantId}:{userId}")
            .SendAsync("NotificationReadUpdated", new
            {
                NotificationId = notificationId,
                IsRead = true
            });
    }




}

