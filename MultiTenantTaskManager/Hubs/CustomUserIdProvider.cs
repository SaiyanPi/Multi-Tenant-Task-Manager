using System.Security.Claims;
using Microsoft.AspNetCore.SignalR;

namespace MultiTenantTaskManager.Hubs;
public class CustomUserIdProvider : IUserIdProvider
{
    public string? GetUserId(HubConnectionContext connection)
    {
        // Ensure this matches the claim JWT includes
        return connection.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    }
}