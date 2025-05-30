using Microsoft.EntityFrameworkCore;
using MultiTenantTaskManager.Data;
using MultiTenantTaskManager.Models;

namespace MultiTenantTaskManager.Services;

public class TenantService : ITenantService
{
    private readonly ApplicationDbContext _context;
    public TenantService(ApplicationDbContext context)
    {
        // ensures dependency injection is working properly, helps to debug early
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<IEnumerable<Tenant>> GetAllTenantsAsync()
    {
        return await _context.Tenants
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<Tenant?> GetTenantByIdAsync(Guid tenantId)
    {
        return await _context.Tenants
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == tenantId);
    }

    public async Task<Tenant> CreateTenantAsync(Tenant tenant)
    {
        if(tenant == null) throw new ArgumentNullException(nameof(tenant));

        tenant.Id = Guid.NewGuid(); // Ensure a new ID is set

        _context.Tenants.Add(tenant);
        await _context.SaveChangesAsync();

        return tenant;
    }

    public async Task<Tenant> UpdateTenantAsync(Guid tenantId, Tenant tenant)
    {
        if (tenant == null) throw new ArgumentNullException(nameof(tenant));

        var existingTenant = await GetTenantByIdAsync(tenantId);
        if (existingTenant == null)
        {
            throw new KeyNotFoundException($"Tenant with ID {tenantId} not found.");
        }
        _context.Tenants.Update(tenant);
        await _context.SaveChangesAsync();

        return tenant;
    }

    public async Task<bool> DeleteTenantAsync(Guid tenantId)
    {
        var tenant = await GetTenantByIdAsync(tenantId);
        if (tenant == null) return false;

        _context.Tenants.Remove(tenant);
        await _context.SaveChangesAsync();
        
        return true;
    }
}