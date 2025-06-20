using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using MultiTenantTaskManager.Data;
using MultiTenantTaskManager.DTOs.Tenant;
using MultiTenantTaskManager.Mappers;
using MultiTenantTaskManager.Models;

namespace MultiTenantTaskManager.Services;

public class TenantService : ITenantService
{
    private readonly ApplicationDbContext _context;
    private readonly IAuditService _auditService;
    public TenantService(ApplicationDbContext context, IAuditService auditService)
    {
        // ensures dependency injection is working properly, helps to debug early
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _auditService = auditService ?? throw new ArgumentNullException(nameof(auditService));
    }

    public async Task<IEnumerable<TenantDto>> GetAllTenantsAsync(int page = 1, int pageSize = 10)
    {
        var tenants = await _context.Tenants
            .AsNoTracking()
            .Include(t => t.Projects) // This eagerly loads the related projects
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return tenants.Select(t => t.ToTenantDto());
    }

    public async Task<TenantDto?> GetTenantByIdAsync(Guid tenantId)
    {
        var tenant = await _context.Tenants
            .AsNoTracking()
            .Include(t => t.Projects) // This eagerly loads the related projects
            .FirstOrDefaultAsync(t => t.Id == tenantId);

        return tenant?.ToTenantDto();
    }

    public async Task<TenantDto> CreateTenantAsync(CreateTenantDto dto)
    {
        if(dto == null) throw new ArgumentNullException(nameof(dto));

        var tenantExists = await _context.Tenants
            .AnyAsync(t => t.Name == dto.Name || t.Domain == dto.Domain);
        if (tenantExists)
        {
            throw new InvalidOperationException($"Tenant with name '{dto.Name}' or domain '{dto.Domain}' already exists.");
        }

        var tenant = dto.ToTenantModel();
        tenant.Id = Guid.NewGuid(); // Ensure a new ID is set

        _context.Tenants.Add(tenant);
        await _context.SaveChangesAsync();

        // AuditLog
        await _auditService.LogAsync(
            action: "Create",
            entityName: "Tenant",
            entityId: tenant.Id.ToString(),
            changes: JsonSerializer.Serialize(TenantMapper.ToTenantDto(tenant))
        );

        return tenant.ToTenantDto();
    }

    public async Task<TenantDto> UpdateTenantAsync(Guid tenantId, UpdateTenantDto dto)
    {
        if (dto == null) throw new ArgumentNullException(nameof(dto));

        var tenant = await _context.Tenants.FirstOrDefaultAsync(t => t.Id == tenantId);
        if (tenant == null)
        {
            throw new KeyNotFoundException($"Tenant with ID {tenantId} not found.");
        }

        // original tenant DTO snapshot before update
        var originalTenantDto = TenantMapper.ToTenantDto(tenant);


        // Now retrieve it again for tracking changes
        var trackedTenant = await _context.Tenants
            .FirstAsync(t => t.Id == tenantId);

        trackedTenant.UpdateFromDto(dto);
        await _context.SaveChangesAsync();

        var updatedTenantDto = TenantMapper.ToTenantDto(trackedTenant);

        // Log both old and new state
        var auditData = new
        {
            Original = originalTenantDto,
            Updated = updatedTenantDto
        };
        await _auditService.LogAsync(
            action: "Update",
            entityName: "Tenant",
            entityId: tenant.Id.ToString(),
            changes: JsonSerializer.Serialize(auditData)
        );
        return tenant.ToTenantDto();
    }

    public async Task<bool> DeleteTenantAsync(Guid tenantId)
    {
        var tenant = await _context.Tenants.FirstOrDefaultAsync(t => t.Id == tenantId);
        if (tenant == null) return false;

        // task DTO before deletion
        var deletedTenantDto = TenantMapper.ToTenantDto(tenant);

        _context.Tenants.Remove(tenant);
        await _context.SaveChangesAsync();

        // audit Log the after deletion
        await _auditService.LogAsync(
            action: "Delete",
            entityName: "Tenant",
            entityId: tenant.Id.ToString(),
            changes: JsonSerializer.Serialize(deletedTenantDto)
        );

        return true;
    }
}