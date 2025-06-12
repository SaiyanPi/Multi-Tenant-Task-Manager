namespace MultiTenantTaskManager.Accessor;

public interface ITenantAccessor
{
    Guid TenantId { get; set; }

    bool IsSuperAdmin { get; }
    
}