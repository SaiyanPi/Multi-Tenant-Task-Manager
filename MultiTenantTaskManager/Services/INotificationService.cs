using MultiTenantTaskManager.DTOs.Notification;

namespace MultiTenantTaskManager.Services;

public interface INotificationService
{
    Task SendNotificationAsync(string userId, string title, string message);


    // notification endpoints

    // Task<IEnumerable<NotificationDto>> GetUserNotificationsAsync();
    // Task MarkAsReadAsync(Guid notificationId);

    // Above lines of code are commented because if we do not pass tenantId or userId as a parameter and
    // instead retrive them from JWT claims, then marking the notification as read in real time from SignalR
    // client would throw exception because SignalR hub methods do not go through HTTP middleware therefore
    // userId and tenantId will have null values. Though retrieving userId and tenantId from JWT claims through
    // HttpContext class still works in NotificationService class, it does not in NotificationHub class.
    // we must use the hub Context for retrieving userId and tenantId.

    // if we do not pass the enantId and userId and instead retrieve them from JWT claim in service class
    // then the methods will have different number of parameters in hub class and service class and calling
    // the service method from hub class will not work.
    // for eg, we call MarkAsReadAsync method from hub class like:

    //      await _notificationService.MarkAsReadAsync(Guid.Parse(tenantId), userId, notificationId);

    // with 3 parameters but if we retrieve tenantId and userId from JWT claims in service class then
    // the MarkAsReadAsync method in the service class will have only one parameter as:

    //      Task MarkAsReadAsync(Guid notificationId);


    Task<IEnumerable<NotificationDto>> GetUserNotificationsAsync(Guid tenantId, string userId);
    Task MarkAsReadAsync(Guid tenantId, string userId, Guid notificationId);
    
    

}