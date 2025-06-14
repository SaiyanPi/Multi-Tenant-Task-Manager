using Microsoft.EntityFrameworkCore;
using MultiTenantTaskManager.Data;
using MultiTenantTaskManager.DTOs;
using MultiTenantTaskManager.Mappers;
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

    public async Task<IEnumerable<TenantDto>> GetAllTenantsAsync(int page = 1, int pageSize = 10)
    {
        var tenants = await _context.Tenants
            .AsNoTracking()
            .Include(t => t.Projects) // This eagerly loads the related projects
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

         return tenants.Select(t => t.ToDto());
    }

    public async Task<TenantDto?> GetTenantByIdAsync(Guid tenantId)
    {
        var tenant = await _context.Tenants
        .AsNoTracking()
            .Include(t => t.Projects) // This eagerly loads the related projects
            .FirstOrDefaultAsync(t => t.Id == tenantId);

        return tenant?.ToDto();
    }

    public async Task<TenantDto> CreateTenantAsync(CreateTenantDto dto)
    {
        if(dto == null) throw new ArgumentNullException(nameof(dto));

        var tenant = dto.ToModel();
        tenant.Id = Guid.NewGuid(); // Ensure a new ID is set

        _context.Tenants.Add(tenant);
        await _context.SaveChangesAsync();

        return tenant.ToDto();
    }

    public async Task<TenantDto> UpdateTenantAsync(Guid tenantId, UpdateTenantDto dto)
    {
        if (dto == null) throw new ArgumentNullException(nameof(dto));

        var tenant = await _context.Tenants.FirstOrDefaultAsync(t => t.Id == tenantId);
        if (tenant == null)
        {
            throw new KeyNotFoundException($"Tenant with ID {tenantId} not found.");
        }

        tenant.UpdateFromDto(dto);
        await _context.SaveChangesAsync();

        return tenant.ToDto();
    }

    public async Task<bool> DeleteTenantAsync(Guid tenantId)
    {
        var tenant = await _context.Tenants.FirstOrDefaultAsync(t => t.Id == tenantId);
        if (tenant == null) return false;

        _context.Tenants.Remove(tenant);
        await _context.SaveChangesAsync();
        return true;
    }
}