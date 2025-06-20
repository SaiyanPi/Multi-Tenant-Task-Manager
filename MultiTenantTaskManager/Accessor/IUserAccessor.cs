namespace MultiTenantTaskManager.Accessor;

public interface IUserAccessor
{
    Guid? UserId { get; }
    string? UserName { get; }
    string? Email { get; }
    Guid? TenantId { get; }
    bool IsAuthenticated { get; }
    bool IsInRole(string role);
}