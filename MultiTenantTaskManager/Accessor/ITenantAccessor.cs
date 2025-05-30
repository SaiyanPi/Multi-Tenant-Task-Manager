namespace MultiTenantTaskManager.Accessor;

public interface ITenantAccessor
{
    Guid TenantId { get; set; }
    
}