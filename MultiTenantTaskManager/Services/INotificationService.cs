namespace MultiTenantTaskManager.Services;
public interface INotificationService
{
    Task SendNotificationAsync(string userId, string title, string message);
}