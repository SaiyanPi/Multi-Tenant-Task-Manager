using MultiTenantTaskManager.DTOs;
using MultiTenantTaskManager.Models;

namespace MultiTenantTaskManager.Services;

public interface ITenantService
{
            
    Task<IEnumerable<TenantDto>> GetAllTenantsAsync(int page = 1, int pageSize = 10);
    Task<TenantDto?> GetTenantByIdAsync(Guid tenantId);
    Task<TenantDto> CreateTenantAsync(CreateTenantDto dto);
    Task<TenantDto> UpdateTenantAsync(Guid tenantId, UpdateTenantDto dto);
    Task<bool> DeleteTenantAsync(Guid tenantId);
}