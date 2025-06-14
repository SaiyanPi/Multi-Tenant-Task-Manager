using MultiTenantTaskManager.Models;

namespace MultiTenantTaskManager.Services;

public interface ITenantService
{
            
    Task<IEnumerable<Tenant>> GetAllTenantsAsync(int page = 1, int pageSize = 10);
    Task<Tenant?> GetTenantByIdAsync(Guid tenantId);
    Task<Tenant> CreateTenantAsync(Tenant tenant);
    Task<Tenant> UpdateTenantAsync(Guid tenantId, Tenant tenant);
    Task<bool> DeleteTenantAsync(Guid tenantId);
}