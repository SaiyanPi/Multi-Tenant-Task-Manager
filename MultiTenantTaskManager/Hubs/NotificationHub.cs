using System.Security.Claims;
using Microsoft.AspNetCore.SignalR;

namespace MultiTenantTaskManager.Hubs;

public class NotificationHub : Hub
{
    public const string HubUrl = "/hubs/notifications";

    // Optional: track connected users if needed
    public override Task OnConnectedAsync()
    {
        var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        Console.WriteLine($"Connected SignalR user: {userId}");
        
        return base.OnConnectedAsync();
    }
}